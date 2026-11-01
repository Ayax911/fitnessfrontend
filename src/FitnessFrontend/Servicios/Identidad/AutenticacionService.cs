using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Identidad;

// RF-013: Servicio de autenticación con ASP.NET Core Identity y Bearer Token
public class AutenticacionService : IAutenticacionService
{
    private const string ClaveToken = "itm_token_acceso";
    private const string ClaveRefreshToken = "itm_refresh_token";

    private readonly FitnessApiClient _apiClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ProveedorEstadoAutenticacion _authStateProvider;

    public AutenticacionService(
        FitnessApiClient apiClient,
        ILocalStorageService localStorage,
        ProveedorEstadoAutenticacion authStateProvider)
    {
        _apiClient = apiClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<ResultadoOperacion> IniciarSesionAsync(string email, string password)
    {
        try
        {
            var body = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var respuesta = await _apiClient.Api.Auth.Login.PostAsync(body);
            if (respuesta == null || string.IsNullOrWhiteSpace(respuesta.AccessToken))
            {
                return ResultadoOperacion.Fallo("No se recibieron credenciales válidas del servidor.");
            }

            await _localStorage.GuardarItemAsync(ClaveToken, respuesta.AccessToken);
            if (!string.IsNullOrWhiteSpace(respuesta.RefreshToken))
            {
                await _localStorage.GuardarItemAsync(ClaveRefreshToken, respuesta.RefreshToken);
            }

            _authStateProvider.NotificarAutenticado(respuesta.AccessToken);
            return ResultadoOperacion.Exito();
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo($"Error de autenticación ({apiEx.ResponseStatusCode}): Credenciales incorrectas o cuenta inactiva.", "CredencialesInvalidas");
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo($"Error al comunicarse con el servicio de autenticación: {ex.Message}", "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> RegistrarAsync(string email, string password)
    {
        try
        {
            var body = new RegisterRequest
            {
                Email = email,
                Password = password
            };

            await _apiClient.Api.Auth.Register.PostAsync(body);
            return ResultadoOperacion.Exito("Registro completado exitosamente.");
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo($"No fue posible completar el registro ({apiEx.ResponseStatusCode}). Verifique que la contraseña cumpla los requisitos de seguridad.", "ErrorRegistro");
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo($"Error de conexión al registrar usuario: {ex.Message}", "ErrorConexion");
        }
    }

    public async Task CerrarSesionAsync()
    {
        await LimpiarSesionAsync();
    }

    public async Task LimpiarSesionAsync()
    {
        await _localStorage.EliminarItemAsync(ClaveToken);
        await _localStorage.EliminarItemAsync(ClaveRefreshToken);
        _authStateProvider.NotificarDesautenticado();
    }

    public async Task<string?> ObtenerTokenAsync()
    {
        return await _localStorage.ObtenerItemAsync(ClaveToken);
    }

    public async Task<UsuarioSesion?> ObtenerUsuarioActualAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return null;
        }

        var idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(idStr, out var id);

        var email = user.FindFirst(ClaimTypes.Email)?.Value 
                    ?? user.FindFirst(ClaimTypes.Name)?.Value 
                    ?? string.Empty;

        var roles = user.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return new UsuarioSesion(id, email, roles);
    }
}
