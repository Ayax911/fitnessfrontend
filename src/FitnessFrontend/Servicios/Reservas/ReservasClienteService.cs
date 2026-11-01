using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Reservas;

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada de cliente HTTP para Reservas
public class ReservasClienteService : IReservasClienteService
{
    private readonly FitnessApiClient _apiClient;

    public ReservasClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // RF-002, RF-003: Reservar cupo o entrar en lista de espera
    public async Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default)
    {
        try
        {
            var body = new CrearReservaRequest
            {
                SesionEventoId = sesionEventoId
            };

            var respuesta = await _apiClient.Api.Reservas.PostAsync(body, cancellationToken: ct);
            if (respuesta == null)
            {
                return EstadoDeCarga<ReservaResumenDto>.Fallo("El servidor no devolvió información de la reserva.");
            }

            return EstadoDeCarga<ReservaResumenDto>.Correcto(respuesta);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<ReservaResumenDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible procesar la reserva.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<ReservaResumenDto>.Fallo(
                $"Error del servidor al procesar la reserva ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<ReservaResumenDto>.Fallo(
                $"Error de conexión al solicitar reserva: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-004: Cancelar reserva
    public async Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default)
    {
        try
        {
            await _apiClient.Api.Reservas[reservaId].Cancelar.PostAsync(cancellationToken: ct);
            return ResultadoOperacion.Exito("Reserva cancelada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible cancelar la reserva.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error al cancelar la reserva ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al cancelar la reserva: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-020: Consultar reservas propias del usuario autenticado
    public async Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Reservas.Mias.GetAsync(cancellationToken: ct);
            var lista = respuesta ?? new List<ReservaResumenDto>();
            return EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(lista);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible cargar sus reservas.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Fallo(
                $"Error al consultar sus reservas ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Fallo(
                $"Error de conexión al consultar sus reservas: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-011: Consultar inscritos de una sesión (Entrenador dueño de la sesión)
    public async Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Reservas.Sesiones[sesionEventoId].Inscritos.GetAsync(cancellationToken: ct);
            var lista = respuesta ?? new List<ReservaInscritoDto>();
            return EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Correcto(lista);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible obtener los inscritos.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Fallo(
                $"Error al consultar inscritos ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Fallo(
                $"Error de red al consultar inscritos: {ex.Message}",
                "ErrorConexion");
        }
    }
}
