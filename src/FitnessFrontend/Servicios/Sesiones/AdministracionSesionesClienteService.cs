using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Sesiones;

// Fachada para RF-028 (Cancelar/Reprogramar) y RF-030 (Reservas de terceros)
public class AdministracionSesionesClienteService : IAdministracionSesionesClienteService
{
    private static readonly ConcurrentDictionary<Guid, SesionAdminDetalleDto> _sesiones = new();

    public Task<EstadoDeCarga<SesionAdminDetalleDto>> ObtenerDetalleSesionAdminAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        var sesion = _sesiones.GetOrAdd(sesionId, id => new SesionAdminDetalleDto(
            id,
            Guid.NewGuid(),
            "Acondicionamiento Físico General",
            "Entrenador Responsable",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            20,
            15,
            false,
            null,
            new List<ReservaAdminDetalleDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Laura Martínez", "laura@itm.edu.co", 0, DateTimeOffset.UtcNow.AddHours(-5)),
                new(Guid.NewGuid(), Guid.NewGuid(), "Andrés Restrepo", "andres@itm.edu.co", 0, DateTimeOffset.UtcNow.AddHours(-4)),
                new(Guid.NewGuid(), Guid.NewGuid(), "Camila Correa", "camila@itm.edu.co", 1, DateTimeOffset.UtcNow.AddHours(-2))
            }));

        return Task.FromResult(EstadoDeCarga<SesionAdminDetalleDto>.Correcto(sesion));
    }

    public Task<ResultadoOperacion> CancelarSesionAsync(
        Guid sesionId,
        string motivo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El motivo de cancelación es obligatorio."));
        }

        if (_sesiones.TryGetValue(sesionId, out var actual))
        {
            var reservasCanceladas = actual.Reservas.Select(r => r with { Estado = 2 }).ToList();
            _sesiones[sesionId] = actual with
            {
                EstaCancelada = true,
                MotivoCancelacion = motivo,
                Reservas = reservasCanceladas
            };
        }

        return Task.FromResult(ResultadoOperacion.Exito("Sesión cancelada con éxito. Las reservas asociadas han sido canceladas sin penalización (RN-031)."));
    }

    public Task<ResultadoOperacion> ReprogramarSesionAsync(
        Guid sesionId,
        DateTimeOffset nuevoInicio,
        DateTimeOffset nuevoFin,
        string motivo,
        CancellationToken ct = default)
    {
        if (nuevoFin <= nuevoInicio)
        {
            return Task.FromResult(ResultadoOperacion.Fallo("La hora de fin debe ser posterior a la de inicio."));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El motivo de reprogramación es obligatorio."));
        }

        if (_sesiones.TryGetValue(sesionId, out var actual))
        {
            _sesiones[sesionId] = actual with
            {
                FechaHoraInicio = nuevoInicio,
                FechaHoraFin = nuevoFin
            };
        }

        return Task.FromResult(ResultadoOperacion.Exito("Sesión reprogramada correctamente."));
    }

    public Task<ResultadoOperacion> InscribirTerceroAsync(
        Guid sesionId,
        Guid usuarioId,
        string motivo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El motivo de la inscripción administrativa es obligatorio."));
        }

        if (_sesiones.TryGetValue(sesionId, out var actual))
        {
            if (actual.EstaCancelada)
            {
                return Task.FromResult(ResultadoOperacion.Fallo("No se puede inscribir en una sesión cancelada."));
            }

            var nuevaLista = actual.Reservas.ToList();
            nuevaLista.Add(new ReservaAdminDetalleDto(
                Guid.NewGuid(),
                usuarioId,
                "Usuario ITM",
                "estudiante@itm.edu.co",
                0,
                DateTimeOffset.UtcNow,
                "Administrador"));

            _sesiones[sesionId] = actual with
            {
                CuposDisponibles = Math.Max(0, actual.CuposDisponibles - 1),
                Reservas = nuevaLista
            };
        }

        return Task.FromResult(ResultadoOperacion.Exito("Inscripción de tercero realizada correctamente."));
    }

    public Task<ResultadoOperacion> RetirarTerceroAsync(
        Guid sesionId,
        Guid reservaId,
        string motivo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El motivo de retiro es obligatorio."));
        }

        if (_sesiones.TryGetValue(sesionId, out var actual))
        {
            var nuevaLista = actual.Reservas.Where(r => r.ReservaId != reservaId).ToList();
            _sesiones[sesionId] = actual with
            {
                CuposDisponibles = Math.Min(actual.CupoMaximo, actual.CuposDisponibles + 1),
                Reservas = nuevaLista
            };
        }

        return Task.FromResult(ResultadoOperacion.Exito("Reserva retirada correctamente. Se ha notificado la promoción al siguiente usuario en lista de espera."));
    }
}
