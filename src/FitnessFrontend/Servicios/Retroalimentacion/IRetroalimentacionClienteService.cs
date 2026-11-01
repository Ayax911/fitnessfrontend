using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Retroalimentacion;

// RF-038: Reportar problemas y sugerencias
public record ReporteProblemaDto(
    Guid Id,
    Guid UsuarioId,
    string UsuarioNombre,
    string Tipo,
    string Mensaje,
    string? RutaOrigen,
    string Estado,
    DateTimeOffset FechaCreacionUtc,
    string? RespuestaAdmin = null);

public record CrearReporteDto(
    string Tipo,
    string Mensaje,
    string? RutaOrigen);

public interface IRetroalimentacionClienteService
{
    Task<EstadoDeCarga<ReporteProblemaDto>> EnviarReporteAsync(CrearReporteDto dto, CancellationToken ct = default);
    Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ObtenerMisReportesAsync(CancellationToken ct = default);
    Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ListarBandejaAdminAsync(string? estado = null, CancellationToken ct = default);
    Task<EstadoDeCarga<ReporteProblemaDto>> CambiarEstadoAsync(Guid id, string nuevoEstado, string? respuesta = null, CancellationToken ct = default);
}
