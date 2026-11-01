using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using Microsoft.AspNetCore.Components;

namespace FitnessFrontend.Servicios.Infraestructura;

// Patrón 7 (docs/arquitectura/04-patrones-de-diseno.md): Interceptor transversal de autorización y manejo de 401
public class ManejadorDeAutenticacionHttp : DelegatingHandler
{
    private const string ClaveToken = "itm_token_acceso";
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
        // Añade Bearer token si no está presente en la petición
        if (request.Headers.Authorization == null)
        {
            var token = await _localStorage.ObtenerItemAsync(ClaveToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Si devuelve 401, limpia la sesión y redirige a login
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _localStorage.EliminarItemAsync(ClaveToken);
            await _localStorage.EliminarItemAsync("itm_refresh_token");
            _authStateProvider.NotificarDesautenticado();

            var uriActual = _navigationManager.Uri;
            if (!uriActual.Contains("/login", System.StringComparison.OrdinalIgnoreCase))
            {
                var rutaRelativa = _navigationManager.ToBaseRelativePath(uriActual);
                var returnUrl = string.IsNullOrWhiteSpace(rutaRelativa) ? "" : $"?returnUrl={Uri.EscapeDataString(rutaRelativa)}";
                _navigationManager.NavigateTo($"/login{returnUrl}");
            }
        }

        return response;
    }
}
