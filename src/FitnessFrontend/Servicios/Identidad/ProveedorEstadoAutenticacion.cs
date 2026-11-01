using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitnessFrontend.Servicios.Identidad;

// RF-013: Estado de autenticación para Blazor WASM persistido en localStorage
public class ProveedorEstadoAutenticacion : AuthenticationStateProvider
{
    private const string ClaveToken = "itm_token_acceso";
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

            var claims = JwtTokenParser.ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return _estadoAnonimo;
        }
    }

    public void NotificarAutenticado(string token)
    {
        var claims = JwtTokenParser.ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var usuario = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(usuario)));
    }

    public void NotificarDesautenticado()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoAnonimo));
    }
}
