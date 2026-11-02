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

namespace FitnessFrontend.Servicios.Retroalimentacion;

// RF-038: Cliente para reporte de problemas y sugerencias conectado a la API
public class RetroalimentacionClienteService : IRetroalimentacionClienteService
{
    private readonly FitnessApiClient _apiClient;

    public RetroalimentacionClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<EstadoDeCarga<ReporteProblemaDto>> EnviarReporteAsync(CrearReporteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Mensaje))
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo("El mensaje es obligatorio.", "MensajeRequerido");
        }

        if (dto.Mensaje.Length > 1000)
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo("El mensaje no puede exceder 1000 caracteres.", "LongitudExcedida");
        }

        try
        {
            var tipoInt = string.Equals(dto.Tipo, "Sugerencia", StringComparison.OrdinalIgnoreCase) ? 1 :
                          string.Equals(dto.Tipo, "Pregunta", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

            var body = new CrearReporteRequest
            {
                Tipo = tipoInt,
                Mensaje = dto.Mensaje,
                Ruta = dto.RutaOrigen
            };

            var resp = await _apiClient.Api.Retroalimentacion.PostAsync(body, cancellationToken: ct);
            if (resp == null)
            {
                return EstadoDeCarga<ReporteProblemaDto>.Fallo("El servidor no devolvió información del reporte creado.");
            }

            var mapeado = MapearADto(resp);
            return EstadoDeCarga<ReporteProblemaDto>.Correcto(mapeado);
        }
        catch (ErrorDetalleDto errorEx)
        {
            var codigo = errorEx.CodigoError ?? "Error";
            var es429 = errorEx.ResponseStatusCode == 429 || string.Equals(codigo, "LimiteExcedido", StringComparison.OrdinalIgnoreCase);
            var mensaje = es429
                ? "Has alcanzado el límite de envíos por hora. Intenta más tarde."
                : (errorEx.Mensaje ?? "No fue posible enviar el reporte.");

            return EstadoDeCarga<ReporteProblemaDto>.Fallo(mensaje, codigo);
        }
        catch (ApiException apiEx)
        {
            var mensaje = apiEx.ResponseStatusCode == 429
                ? "Has alcanzado el límite de envíos por hora. Intenta más tarde."
                : $"Error del servidor al enviar el reporte ({apiEx.ResponseStatusCode}).";

            return EstadoDeCarga<ReporteProblemaDto>.Fallo(mensaje, apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo(
                $"Error de conexión al enviar reporte: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ObtenerMisReportesAsync(CancellationToken ct = default)
    {
        try
        {
            var lista = await _apiClient.Api.Retroalimentacion.Mios.GetAsync(cancellationToken: ct);
            if (lista == null)
            {
                return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(new List<ReporteProblemaDto>());
            }

            var dtos = lista.Select(MapearADto).OrderByDescending(r => r.FechaCreacionUtc).ToList();
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(dtos);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar tus reportes.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Fallo(
                $"Error del servidor al consultar reportes ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Fallo(
                $"Error de conexión al consultar tus reportes: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ListarBandejaAdminAsync(string? estado = null, CancellationToken ct = default)
    {
        try
        {
            int? estadoFiltro = null;
            if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                estadoFiltro = string.Equals(estado, "EnRevision", StringComparison.OrdinalIgnoreCase) ? 1 :
                               string.Equals(estado, "Resuelto", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
            }

            var pagina = await _apiClient.Api.Retroalimentacion.GetAsync(q =>
            {
                if (estadoFiltro.HasValue)
                {
                    q.QueryParameters.Estado = estadoFiltro.Value;
                }
                q.QueryParameters.Pagina = 1;
                q.QueryParameters.Tamano = 50;
            }, ct);

            if (pagina?.Items == null)
            {
                return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(new List<ReporteProblemaDto>());
            }

            var dtos = pagina.Items.Select(MapearADto).OrderByDescending(r => r.FechaCreacionUtc).ToList();
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(dtos);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar la bandeja de reportes.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Fallo(
                $"Error del servidor al consultar la bandeja ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Fallo(
                $"Error de conexión al consultar la bandeja: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<ReporteProblemaDto>> CambiarEstadoAsync(Guid id, string nuevoEstado, string? respuesta = null, CancellationToken ct = default)
    {
        try
        {
            var estadoInt = string.Equals(nuevoEstado, "EnRevision", StringComparison.OrdinalIgnoreCase) ? 1 :
                            string.Equals(nuevoEstado, "Resuelto", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

            var body = new ActualizarEstadoReporteRequest
            {
                Estado = estadoInt,
                Motivo = respuesta
            };

            var resp = await _apiClient.Api.Retroalimentacion[id].Estado.PutAsync(body, cancellationToken: ct);
            if (resp == null)
            {
                return EstadoDeCarga<ReporteProblemaDto>.Fallo("El servidor no devolvió el reporte actualizado.");
            }

            return EstadoDeCarga<ReporteProblemaDto>.Correcto(MapearADto(resp));
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible actualizar el estado del reporte.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo(
                $"Error del servidor al actualizar estado ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo(
                $"Error de conexión al actualizar estado: {ex.Message}",
                "ErrorConexion");
        }
    }

    private static ReporteProblemaDto MapearADto(ReporteDto r)
    {
        var tipoStr = r.Tipo switch
        {
            1 => "Sugerencia",
            2 => "Pregunta",
            _ => "Problema"
        };

        var estadoStr = r.Estado switch
        {
            1 => "EnRevision",
            2 => "Resuelto",
            _ => "Nuevo"
        };

        return new ReporteProblemaDto(
            r.Id ?? Guid.Empty,
            r.UsuarioId ?? Guid.Empty,
            r.UsuarioNombre ?? "Usuario ITM",
            tipoStr,
            r.Mensaje ?? string.Empty,
            r.Ruta,
            estadoStr,
            r.FechaHoraUtc ?? DateTimeOffset.UtcNow,
            null
        );
    }
}
