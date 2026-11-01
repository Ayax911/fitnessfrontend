using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Usuarios;

// RF-023 — Fachada cliente (Patrón 1)
public class UsuariosClienteService : IUsuariosClienteService
{
    private readonly FitnessApiClient _apiClient;

    public UsuariosClienteService(FitnessApiClient apiClient) => _apiClient = apiClient;

    public async Task<EstadoDeCarga<PaginaUsuariosDto>> BuscarAsync(string? busqueda, string? rol, int pagina, int tamano, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Usuarios.GetAsync(c =>
            {
                c.QueryParameters.Busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();
                c.QueryParameters.Rol = string.IsNullOrWhiteSpace(rol) ? null : rol;
                c.QueryParameters.Pagina = pagina;
                c.QueryParameters.Tamano = tamano;
            }, ct);

            return respuesta == null
                ? EstadoDeCarga<PaginaUsuariosDto>.Fallo("No se recibió respuesta del servidor.")
                : EstadoDeCarga<PaginaUsuariosDto>.Correcto(respuesta);
        }
        catch (Exception ex)
        {
            var (mensaje, codigo) = ErroresApi.Traducir(ex, "buscar usuarios");
            return EstadoDeCarga<PaginaUsuariosDto>.Fallo(mensaje, codigo);
        }
    }

    public async Task<EstadoDeCarga<List<EntrenadorDto>>> ListarEntrenadoresAsync(CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Usuarios.Entrenadores.GetAsync(cancellationToken: ct);
            return EstadoDeCarga<List<EntrenadorDto>>.Correcto(respuesta ?? new List<EntrenadorDto>());
        }
        catch (Exception ex)
        {
            var (mensaje, codigo) = ErroresApi.Traducir(ex, "obtener los entrenadores");
            return EstadoDeCarga<List<EntrenadorDto>>.Fallo(mensaje, codigo);
        }
    }

    public async Task<ResultadoOperacion> ActualizarRolesAsync(Guid usuarioId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        try
        {
            await _apiClient.Api.Usuarios[usuarioId].Roles.PutAsync(new ActualizarRolesRequest { Roles = roles.ToList() }, cancellationToken: ct);
            return ResultadoOperacion.Exito("Roles actualizados correctamente.");
        }
        catch (Exception ex)
        {
            var (mensaje, codigo) = ErroresApi.Traducir(ex, "actualizar los roles");
            return ResultadoOperacion.Fallo(mensaje, codigo);
        }
    }
}
