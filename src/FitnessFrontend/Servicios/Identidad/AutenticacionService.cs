using System;
using System.Collections.Generic;
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
    private const string ClaveToken = ProveedorEstadoAutenticacion.ClaveToken;
    private const string ClaveRefreshToken = ProveedorEstadoAutenticacion.ClaveRefreshToken;
    private const string ClavePerfil = ProveedorEstadoAutenticacion.ClavePerfil;

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

            // El token es opaco: identidad y roles se piden al backend (GET /api/usuarios/yo) con el token recién guardado.
            // Si eso falla NO se deja una sesión a medias (token sin perfil) ni se culpa a las credenciales.
            var perfil = await ObtenerPerfilAsync();
            if (perfil == null)
            {
                await LimpiarSesionAsync();
                return ResultadoOperacion.Fallo("No fue posible obtener su perfil. Intente iniciar sesión de nuevo.", "PerfilNoDisponible");
            }

            await _localStorage.GuardarItemAsync(ClavePerfil, ProveedorEstadoAutenticacion.SerializarPerfil(perfil));

            _authStateProvider.NotificarAutenticado(respuesta.AccessToken, perfil);
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

    private async Task<PerfilSesion?> ObtenerPerfilAsync()
    {
        try
        {
            var dto = await _apiClient.Api.Usuarios.Yo.GetAsync();
            return dto?.Id == null
                ? null
                : new PerfilSesion(dto.Id.Value, dto.Email ?? string.Empty, dto.Roles ?? new List<string>());
        }
        catch
        {
            return null;
        }
    }

    // RF-013, RF-025: /api/auth/register está bloqueado en el backend; el registro real es /api/usuarios/registro
    public async Task<ResultadoOperacion> RegistrarAsync(string email, string password, string nombreCompleto, TipoUsuario tipoUsuario, bool aceptaTratamientoDatos)
    {
        try
        {
            var body = new RegistroUsuarioRequest
            {
                Email = email,
                Password = password,
                NombreCompleto = nombreCompleto,
                TipoUsuario = tipoUsuario,
                AceptaTratamientoDatos = aceptaTratamientoDatos
            };

            await _apiClient.Api.Usuarios.Registro.PostAsync(body);
            return ResultadoOperacion.Exito("Registro completado exitosamente.");
        }
        catch (Exception ex)
        {
            var (mensaje, codigo) = ErroresApi.Traducir(ex, "completar el registro");

            // El backend devuelve las reglas de Identity en inglés; se muestra el requisito en español
            if (codigo == "ContrasenaInvalida")
            {
                mensaje = "La contraseña no cumple los requisitos de seguridad: mínimo 6 caracteres, con mayúscula, minúscula, número y un símbolo.";
            }

            return ResultadoOperacion.Fallo(mensaje, codigo ?? "ErrorRegistro");
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
        await _localStorage.EliminarItemAsync(ClavePerfil);
        _authStateProvider.NotificarDesautenticado();
    }

    public async Task<string?> ObtenerTokenAsync()
    {
        return await _localStorage.ObtenerItemAsync(ClaveToken);
    }

    public async Task<ResultadoOperacion> RenovarSesionAsync()
    {
        try
        {
            var refreshToken = await _localStorage.ObtenerItemAsync(ClaveRefreshToken);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await LimpiarSesionAsync();
                return ResultadoOperacion.Fallo("No hay token de renovación.", "SinRefreshToken");
            }

            var respuesta = await _apiClient.Api.Auth.Refresh.PostAsync(new RefreshRequest
            {
                RefreshToken = refreshToken
            });

            if (respuesta == null || string.IsNullOrWhiteSpace(respuesta.AccessToken))
            {
                await LimpiarSesionAsync();
                return ResultadoOperacion.Fallo("No se recibieron credenciales renovadas.", "RenovacionInvalida");
            }

            await _localStorage.GuardarItemAsync(ClaveToken, respuesta.AccessToken);
            if (!string.IsNullOrWhiteSpace(respuesta.RefreshToken))
            {
                await _localStorage.GuardarItemAsync(ClaveRefreshToken, respuesta.RefreshToken);
            }

            await _authStateProvider.NotificarTokenRenovadoAsync(respuesta.AccessToken);
            return ResultadoOperacion.Exito();
        }
        catch (Exception ex)
        {
            await LimpiarSesionAsync();
            return ResultadoOperacion.Fallo($"Error al renovar sesión: {ex.Message}", "FalloRenovacion");
        }
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
