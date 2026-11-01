using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Asistencia;

public record InscritoAsistenciaManualDto(
    Guid ReservaId,
    Guid UsuarioId,
    string NombreCompleto,
    string Correo,
    bool Asistio,
    DateTimeOffset? FechaHoraMarcado,
    string? Motivo
);

public interface IAsistenciaManualClienteService
{
    Task<EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>> ObtenerInscritosAsistenciaAsync(
        Guid sesionId,
        CancellationToken ct = default);

    Task<ResultadoOperacion> MarcarAsistenciaManualAsync(
        Guid sesionId,
        Guid reservaId,
        string? motivo = null,
        CancellationToken ct = default);

    Task<ResultadoOperacion> DesmarcarAsistenciaManualAsync(
        Guid sesionId,
        Guid reservaId,
        string? motivo = null,
        CancellationToken ct = default);
}
