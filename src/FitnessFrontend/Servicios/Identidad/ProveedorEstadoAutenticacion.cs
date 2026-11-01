using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitnessFrontend.Servicios.Identidad;

// RF-013: Estado de autenticación para Blazor WASM persistido en localStorage.
// Identidad y roles vienen del perfil guardado al iniciar sesión (PerfilSesion); si no hay perfil se intenta leer un JWT
// (compatibilidad por si el backend emitiera JWT). Con un token sin claims legibles la sesión se trata como anónima
// en lugar de "autenticada sin roles", que dejaba al usuario sin ninguna pantalla utilizable.
public class ProveedorEstadoAutenticacion : AuthenticationStateProvider
{
    public const string ClaveToken = "itm_token_acceso";
    public const string ClaveRefreshToken = "itm_refresh_token";
    public const string ClavePerfil = "itm_perfil";

    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationState _estadoAnonimo;

    public ProveedorEstadoAutenticacion(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
        _estadoAnonimo = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.ObtenerItemAsync(ClaveToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                return _estadoAnonimo;
            }

            var perfilJson = await _localStorage.ObtenerItemAsync(ClavePerfil);
            return Construir(token, DeserializarPerfil(perfilJson));
        }
        catch
        {
            return _estadoAnonimo;
        }
    }

    public void NotificarAutenticado(string token, PerfilSesion? perfil = null)
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Construir(token, perfil)));
    }

    public void NotificarDesautenticado()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoAnonimo));
    }

    public async Task NotificarTokenRenovadoAsync(string nuevoToken)
    {
        var perfilJson = await _localStorage.ObtenerItemAsync(ClavePerfil);
        var perfil = DeserializarPerfil(perfilJson);
        NotificarAutenticado(nuevoToken, perfil);
    }

    public static string SerializarPerfil(PerfilSesion perfil) => JsonSerializer.Serialize(perfil);

    private static readonly JsonSerializerOptions OpcionesJson = new() { PropertyNameCaseInsensitive = true };

    private static PerfilSesion? DeserializarPerfil(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<PerfilSesion>(json, OpcionesJson); }
        catch (JsonException) { return null; }
    }

    private AuthenticationState Construir(string token, PerfilSesion? perfil)
    {
        var claims = perfil != null
            ? perfil.ComoClaims().ToList()
            : JwtTokenParser.ParseClaimsFromJwt(token).ToList();

        if (claims.Count == 0)
        {
            return _estadoAnonimo;
        }

        var identity = new ClaimsIdentity(claims, perfil != null ? PerfilSesion.TipoAutenticacion : "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
