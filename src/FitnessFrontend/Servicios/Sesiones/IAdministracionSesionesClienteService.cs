using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Sesiones;

// DTOs para RF-028, RF-030 (Administración de sesiones por Administrador)
// RN-020: NINGÚN campo de nivel, penalización ni indicador.
public record ReservaAdminDetalleDto(
    Guid ReservaId,
    Guid UsuarioId,
    string NombreUsuario,
    string Correo,
    int Estado, // 0: Confirmada, 1: EnListaEspera, 2: Cancelada, 3: Asistida, 4: NoAsistida
    DateTimeOffset FechaSolicitud,
    string? Origen = null
);

public record SesionAdminDetalleDto(
    Guid SesionId,
    Guid EventoId,
    string EventoNombre,
    string? EntrenadorNombre,
    DateTimeOffset FechaHoraInicio,
    DateTimeOffset FechaHoraFin,
    int CupoMaximo,
    int CuposDisponibles,
    bool EstaCancelada,
    string? MotivoCancelacion,
    IReadOnlyList<ReservaAdminDetalleDto> Reservas
);

public interface IAdministracionSesionesClienteService
{
    Task<EstadoDeCarga<SesionAdminDetalleDto>> ObtenerDetalleSesionAdminAsync(
        Guid sesionId,
        CancellationToken ct = default);

    Task<ResultadoOperacion> CancelarSesionAsync(
        Guid sesionId,
        string motivo,
        CancellationToken ct = default);

    Task<ResultadoOperacion> ReprogramarSesionAsync(
        Guid sesionId,
        DateTimeOffset nuevoInicio,
        DateTimeOffset nuevoFin,
        string motivo,
        CancellationToken ct = default);

    Task<ResultadoOperacion> InscribirTerceroAsync(
        Guid sesionId,
        Guid usuarioId,
        string motivo,
        CancellationToken ct = default);

    Task<ResultadoOperacion> RetirarTerceroAsync(
        Guid sesionId,
        Guid reservaId,
        string motivo,
        CancellationToken ct = default);
}
