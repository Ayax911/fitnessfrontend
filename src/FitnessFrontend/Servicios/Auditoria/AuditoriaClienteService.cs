using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Auditoria;

// RF-032: Cliente para consulta de auditoría administrativa conectado a la API
public class AuditoriaClienteService : IAuditoriaClienteService
{
    private readonly FitnessApiClient _apiClient;

    public AuditoriaClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<EstadoDeCarga<PaginaAuditoriaDto>> ConsultarAuditoriaAsync(
        DateTimeOffset? desde = null,
        DateTimeOffset? hasta = null,
        string? tipoAccion = null,
        string? actor = null,
        int pagina = 1,
        int tamano = 25,
        CancellationToken ct = default)
    {
        try
        {
            var tamanoAjustado = Math.Min(Math.Max(1, tamano), 50); // Máx 50 por página según RF-032 / H-19

            var respuesta = await _apiClient.Api.Auditoria.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
                if (!string.IsNullOrWhiteSpace(tipoAccion))
                {
                    q.QueryParameters.Accion = tipoAccion;
                }
                if (Guid.TryParse(actor, out var actorGuid))
                {
                    q.QueryParameters.ActorId = actorGuid;
                }
                q.QueryParameters.Pagina = pagina;
                q.QueryParameters.Tamano = tamanoAjustado;
            }, ct);

            if (respuesta == null)
            {
                return EstadoDeCarga<PaginaAuditoriaDto>.Correcto(
                    new PaginaAuditoriaDto(new List<RegistroAuditoriaDto>(), 0, pagina, 1));
            }

            var elementos = (respuesta.Items ?? new List<ApiCliente.Models.RegistroAuditoriaDto>())
                .Select(item =>
                {
                    var id = item.Id ?? Guid.Empty;
                    var fecha = item.FechaHoraUtc ?? DateTimeOffset.UtcNow;
                    var actorIdStr = item.ActorId?.ToString() ?? "Sistema";
                    var rol = item.ActorRol ?? "Administrador";
                    var accion = item.Accion ?? "Accion";
                    var entidad = string.IsNullOrWhiteSpace(item.EntidadId)
                        ? (item.EntidadTipo ?? "—")
                        : $"{item.EntidadTipo} ({item.EntidadId})";
                    var detalle = string.IsNullOrWhiteSpace(item.Motivo)
                        ? (item.Resumen ?? string.Empty)
                        : $"{item.Resumen} | Motivo: {item.Motivo}";

                    return new RegistroAuditoriaDto(id, fecha, actorIdStr, rol, accion, entidad, detalle);
                }).ToList();

            var total = respuesta.Total ?? 0;
            var paginaActual = respuesta.Pagina ?? pagina;
            var totalPaginas = (int)Math.Ceiling((double)total / tamanoAjustado);

            var resultado = new PaginaAuditoriaDto(elementos, total, paginaActual, Math.Max(1, totalPaginas));
            return EstadoDeCarga<PaginaAuditoriaDto>.Correcto(resultado);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<PaginaAuditoriaDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar el registro de auditoría.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            var mensaje = apiEx.ResponseStatusCode == 403
                ? "No tienes permisos para ver el registro de auditoría."
                : $"Error del servidor al consultar auditoría ({apiEx.ResponseStatusCode}).";

            return EstadoDeCarga<PaginaAuditoriaDto>.Fallo(
                mensaje,
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<PaginaAuditoriaDto>.Fallo(
                $"Error de conexión al consultar auditoría: {ex.Message}",
                "ErrorConexion");
        }
    }
}
