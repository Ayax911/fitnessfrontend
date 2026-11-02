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

namespace FitnessFrontend.Servicios.Sesiones;

// RF-028, RF-030: Cliente para administración de sesiones (Cancelar, Reprogramar, Inscripciones de terceros)
public class AdministracionSesionesClienteService : IAdministracionSesionesClienteService
{
    private readonly FitnessApiClient _apiClient;

    public AdministracionSesionesClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<EstadoDeCarga<SesionAdminDetalleDto>> ObtenerDetalleSesionAdminAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        try
        {
            // Consultamos inscritos de la sesión
            var inscritos = await _apiClient.Api.Reservas.Sesiones[sesionId].Inscritos.GetAsync(cancellationToken: ct)
                            ?? new List<ReservaInscritoDto>();

            // Consultamos eventos para ubicar metadata de la sesión
            var eventosResp = await _apiClient.Api.Eventos.GetAsync(q =>
            {
                q.QueryParameters.Tamano = 100;
            }, ct);

            EventoResumenDto? eventoEncontrado = null;
            SesionResumenDto? sesionEncontrada = null;

            if (eventosResp != null)
            {
                foreach (var ev in eventosResp)
                {
                    var s = ev.Sesiones?.FirstOrDefault(ses => ses.Id == sesionId);
                    if (s != null)
                    {
                        eventoEncontrado = ev;
                        sesionEncontrada = s;
                        break;
                    }
                }
            }

            var eventoId = eventoEncontrado?.Id ?? Guid.Empty;
            var eventoNombre = eventoEncontrado?.Nombre ?? "Actividad deportiva";
            var entrenadorNombre = eventoEncontrado?.EntrenadorNombre;
            var inicio = sesionEncontrada?.FechaHoraInicio ?? DateTimeOffset.UtcNow;
            var fin = sesionEncontrada?.FechaHoraFin ?? inicio.AddHours(1);
            var cupoMax = sesionEncontrada?.CupoMaximo ?? 20;
            var cuposDisp = sesionEncontrada?.CuposDisponibles ?? 0;
            var estaCancelada = false;
            string? motivoCanc = null;

            var reservasMapeadas = inscritos.Select(i => new ReservaAdminDetalleDto(
                ReservaId: Guid.Empty, // El DTO de inscritos expone UsuarioId, Estado y NombreCompleto (RN-020)
                UsuarioId: i.UsuarioId ?? Guid.Empty,
                NombreUsuario: i.NombreCompleto ?? "Usuario ITM",
                Correo: string.Empty,
                Estado: i.Estado ?? 0,
                FechaSolicitud: DateTimeOffset.UtcNow,
                Origen: null
            )).ToList();

            var sesionDetalle = new SesionAdminDetalleDto(
                sesionId,
                eventoId,
                eventoNombre,
                entrenadorNombre,
                inicio,
                fin,
                cupoMax,
                cuposDisp,
                estaCancelada,
                motivoCanc,
                reservasMapeadas
            );

            return EstadoDeCarga<SesionAdminDetalleDto>.Correcto(sesionDetalle);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<SesionAdminDetalleDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible obtener el detalle de la sesión.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<SesionAdminDetalleDto>.Fallo(
                $"Error del servidor al consultar la sesión ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<SesionAdminDetalleDto>.Fallo(
                $"Error de conexión al consultar la sesión: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> CancelarSesionAsync(
        Guid sesionId,
        string motivo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return ResultadoOperacion.Fallo("El motivo de cancelación es obligatorio.", "MotivoRequerido");
        }

        try
        {
            var body = new CancelarSesionRequest
            {
                Motivo = motivo
            };

            await _apiClient.Api.Eventos.Sesiones[sesionId].Cancelar.PostAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Sesión cancelada con éxito. Las reservas asociadas han sido canceladas sin penalización (RN-031).");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible cancelar la sesión.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al cancelar la sesión ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al cancelar la sesión: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> ReprogramarSesionAsync(
        Guid sesionId,
        DateTimeOffset nuevoInicio,
        DateTimeOffset nuevoFin,
        string motivo,
        CancellationToken ct = default)
    {
        if (nuevoFin <= nuevoInicio)
        {
            return ResultadoOperacion.Fallo("La hora de fin debe ser posterior a la de inicio.", "RangoInvalido");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return ResultadoOperacion.Fallo("El motivo de reprogramación es obligatorio.", "MotivoRequerido");
        }

        try
        {
            var body = new ReprogramarSesionRequest
            {
                FechaHoraInicio = nuevoInicio,
                FechaHoraFin = nuevoFin,
                Motivo = motivo
            };

            await _apiClient.Api.Eventos.Sesiones[sesionId].Reprogramar.PutAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Sesión reprogramada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible reprogramar la sesión.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al reprogramar la sesión ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al reprogramar la sesión: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> InscribirTerceroAsync(
        Guid sesionId,
        Guid usuarioId,
        string motivo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return ResultadoOperacion.Fallo("El motivo de la inscripción administrativa es obligatorio.", "MotivoRequerido");
        }

        try
        {
            var body = new InscribirTerceroRequest
            {
                UsuarioId = usuarioId,
                Motivo = motivo
            };

            await _apiClient.Api.Reservas.Sesiones[sesionId].Inscribir.PostAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Inscripción de tercero realizada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible inscribir al usuario.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al inscribir al usuario ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al inscribir al usuario: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> RetirarTerceroAsync(
        Guid sesionId,
        Guid reservaId,
        string motivo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return ResultadoOperacion.Fallo("El motivo de retiro es obligatorio.", "MotivoRequerido");
        }

        try
        {
            var body = new CancelarReservaAdministrativaRequest
            {
                Motivo = motivo
            };

            await _apiClient.Api.Reservas[reservaId].Administrativa.DeleteAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Reserva retirada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible retirar la reserva.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al retirar la reserva ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al retirar la reserva: {ex.Message}",
                "ErrorConexion");
        }
    }
}
