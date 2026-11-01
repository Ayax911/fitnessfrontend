using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using Microsoft.AspNetCore.Components;

namespace FitnessFrontend.Servicios.Infraestructura;

// RF-026, Patrón 8 (docs/arquitectura/04-patrones-de-diseno.md):
// Renovación de sesión de un solo vuelo con SemaphoreSlim y reintento único sin bucle
public class ManejadorDeAutenticacionHttp : DelegatingHandler
{
    private const string ClaveToken = ProveedorEstadoAutenticacion.ClaveToken;
    private const string ClaveRefreshToken = ProveedorEstadoAutenticacion.ClaveRefreshToken;
    private const string ClavePerfil = ProveedorEstadoAutenticacion.ClavePerfil;

    private static readonly SemaphoreSlim SemaforoRefresh = new(1, 1);

    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigationManager;
    private readonly ProveedorEstadoAutenticacion _authStateProvider;

    public ManejadorDeAutenticacionHttp(
        ILocalStorageService localStorage,
        NavigationManager navigationManager,
        ProveedorEstadoAutenticacion authStateProvider)
    {
        _localStorage = localStorage;
        _navigationManager = navigationManager;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var esPeticionAuth = path.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase);

        // Aseguramos que el contenido de la petición se amortigüe para permitir reintentos clonados si fuera necesario
        if (request.Content != null)
        {
            await request.Content.LoadIntoBufferAsync();
        }

        // Añade Bearer token si no está presente en la petición y no es una ruta de autenticación
        if (!esPeticionAuth && request.Headers.Authorization == null)
        {
            var token = await _localStorage.ObtenerItemAsync(ClaveToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Si la petición era de auth, no disparamos renovación recursiva
        if (esPeticionAuth)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
            {
                await LimpiarSesionYRedirigirAsync();
            }
            return response;
        }

        // Si devuelve 401 en un endpoint protegido, intentamos renovar con Patrón 8
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var tokenEnviado = request.Headers.Authorization?.Parameter;

            await SemaforoRefresh.WaitAsync(cancellationToken);
            try
            {
                // Verificar si otra petición en vuelo ya renovó el token
                var tokenActual = await _localStorage.ObtenerItemAsync(ClaveToken);
                if (!string.IsNullOrWhiteSpace(tokenActual) && tokenActual != tokenEnviado)
                {
                    // Reintentar directamente con el token ya renovado
                    var reintento = await ClonarHttpRequestAsync(request);
                    reintento.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenActual);
                    return await base.SendAsync(reintento, cancellationToken);
                }

                // Ejecutamos exactamente una llamada a refresh
                var exito = await EjecutarRefreshAsync(request, cancellationToken);
                if (!exito)
                {
                    await LimpiarSesionYRedirigirAsync();
                    return response;
                }

                var nuevoToken = await _localStorage.ObtenerItemAsync(ClaveToken);
                var reintentoTrasRefresh = await ClonarHttpRequestAsync(request);
                reintentoTrasRefresh.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nuevoToken);

                var respuestaReintento = await base.SendAsync(reintentoTrasRefresh, cancellationToken);

                // Si tras el reintento vuelve a fallar con 401, cerramos sesión y NO reintentamos más
                if (respuestaReintento.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await LimpiarSesionYRedirigirAsync();
                }

                return respuestaReintento;
            }
            finally
            {
                SemaforoRefresh.Release();
            }
        }

        return response;
    }

    private async Task<bool> EjecutarRefreshAsync(HttpRequestMessage originalRequest, CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = await _localStorage.ObtenerItemAsync(ClaveRefreshToken);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            var refreshUri = ObtenerRefreshUri(originalRequest);
            var payload = JsonSerializer.Serialize(new { refreshToken });
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, refreshUri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);
            if (!refreshResponse.IsSuccessStatusCode)
            {
                return false;
            }

            using var stream = await refreshResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            string? nuevoAccessToken = null;
            if (root.TryGetProperty("accessToken", out var at) || root.TryGetProperty("AccessToken", out at))
            {
                nuevoAccessToken = at.GetString();
            }

            string? nuevoRefreshToken = null;
            if (root.TryGetProperty("refreshToken", out var rt) || root.TryGetProperty("RefreshToken", out rt))
            {
                nuevoRefreshToken = rt.GetString();
            }

            if (string.IsNullOrWhiteSpace(nuevoAccessToken))
            {
                return false;
            }

            await _localStorage.GuardarItemAsync(ClaveToken, nuevoAccessToken);
            if (!string.IsNullOrWhiteSpace(nuevoRefreshToken))
            {
                await _localStorage.GuardarItemAsync(ClaveRefreshToken, nuevoRefreshToken);
            }

            await _authStateProvider.NotificarTokenRenovadoAsync(nuevoAccessToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private Uri ObtenerRefreshUri(HttpRequestMessage request)
    {
        if (request.RequestUri != null && request.RequestUri.IsAbsoluteUri)
        {
            var apiBase = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority));
            return new Uri(apiBase, "/api/auth/refresh");
        }

        return new Uri(new Uri(_navigationManager.BaseUri), "api/auth/refresh");
    }

    private async Task LimpiarSesionYRedirigirAsync()
    {
        await _localStorage.EliminarItemAsync(ClaveToken);
        await _localStorage.EliminarItemAsync(ClaveRefreshToken);
        await _localStorage.EliminarItemAsync(ClavePerfil);
        _authStateProvider.NotificarDesautenticado();

        var uriActual = _navigationManager.Uri;
        if (!uriActual.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            var rutaRelativa = _navigationManager.ToBaseRelativePath(uriActual);
            var returnUrl = string.IsNullOrWhiteSpace(rutaRelativa) ? "" : $"?returnUrl={Uri.EscapeDataString(rutaRelativa)}";
            _navigationManager.NavigateTo($"/login{returnUrl}");
        }
    }

    private static async Task<HttpRequestMessage> ClonarHttpRequestAsync(HttpRequestMessage original)
    {
        var clon = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        foreach (var header in original.Headers)
        {
            clon.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content != null)
        {
            var ms = new MemoryStream();
            await original.Content.CopyToAsync(ms);
            ms.Position = 0;
            clon.Content = new StreamContent(ms);

            if (original.Content.Headers != null)
            {
                foreach (var h in original.Content.Headers)
                {
                    clon.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }
        }

        foreach (var prop in original.Options)
        {
            clon.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);
        }

        return clon;
    }
}

