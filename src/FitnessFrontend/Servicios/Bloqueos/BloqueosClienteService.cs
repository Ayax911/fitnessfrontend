using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Bloqueos;

// RF-031: Implementación de cliente para bloqueos de calendario conectado a la API
public class BloqueosClienteService : IBloqueosClienteService
{
    private readonly FitnessApiClient _apiClient;

    public BloqueosClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>> ObtenerBloqueosAsync(
        DateTimeOffset? desde = null,
        DateTimeOffset? hasta = null,
        CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Calendario.Bloqueos.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct);

            if (respuesta == null)
            {
                return EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>.Correcto(new List<BloqueoCalendarioDto>());
            }

            var dtos = respuesta.Select(b => new BloqueoCalendarioDto(
                b.Id ?? Guid.Empty,
                b.Desde ?? DateTimeOffset.UtcNow,
                b.Hasta ?? DateTimeOffset.UtcNow,
                b.Motivo ?? string.Empty,
                b.Tipo switch { 1 => "Festivo", 2 => "Institucional", _ => "Mantenimiento" }
            )).OrderBy(b => b.Desde).ToList();

            return EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>.Correcto(dtos);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar los bloqueos de calendario.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>.Fallo(
                $"Error del servidor al consultar bloqueos ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>.Fallo(
                $"Error de conexión al consultar bloqueos: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> CrearBloqueoAsync(CrearBloqueoRequest request, CancellationToken ct = default)
    {
        try
        {
            var tipoInt = string.Equals(request.Tipo, "Festivo", StringComparison.OrdinalIgnoreCase) ? 1 :
                          string.Equals(request.Tipo, "Institucional", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

            var body = new ApiCliente.Models.CrearBloqueoRequest
            {
                Desde = request.Desde,
                Hasta = request.Hasta,
                Motivo = request.Motivo,
                Tipo = tipoInt
            };

            var respuesta = await _apiClient.Api.Calendario.Bloqueos.PostAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito();
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible crear el bloqueo.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al crear bloqueo ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al crear bloqueo: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> EliminarBloqueoAsync(Guid bloqueoId, CancellationToken ct = default)
    {
        try
        {
            await _apiClient.Api.Calendario.Bloqueos[bloqueoId].DeleteAsync(cancellationToken: ct);
            return ResultadoOperacion.Exito();
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible eliminar el bloqueo.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al eliminar bloqueo ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al eliminar bloqueo: {ex.Message}",
                "ErrorConexion");
        }
    }
}
