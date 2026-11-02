using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Reportes;

// DTOs para RF-033 (Reportes y exportación)
// RN-020: NINGÚN reporte incluye nivel de priorización ni indicador
public record ReporteAsistenciaSesionDto(
    Guid SesionId,
    string EventoNombre,
    DateTimeOffset FechaHora,
    int CupoMaximo,
    int Inscritos,
    int Asistentes,
    int NoAsistentes,
    double PorcentajeAsistencia
);

public record ReporteOcupacionDto(
    string EventoNombre,
    DateTime Fecha,
    int CupoTotal,
    int CupoOcupado,
    double PorcentajeOcupacion
);

public record ReporteInasistenciasEventoDto(
    string EventoNombre,
    int TotalReservas,
    int Inasistencias,
    double TasaInasistencia
);

public record ReporteHorasPicoDto(
    string DiaSemana,
    int FranjaHora, // 0..23 hora local
    int TotalIngresosYCheckins
);

/// <summary>Una fila por día del rango consultado.</summary>
public record UsoPlataformaDiarioDto(DateTime Fecha, int RegistrosNuevos, int UsuariosActivos, int ReservasCreadas, int Cancelaciones);

/// <summary>
/// Totales del rango (registros, reservas y cancelaciones suman los días; UsuariosActivos es el MÁXIMO de un día,
/// porque el backend informa personas activas por día y sumarlas contaría varias veces a la misma persona) y el detalle diario.
/// </summary>
public record ReporteUsoPlataformaDto(
    int RegistrosNuevos,
    int UsuariosActivos,
    int ReservasCreadas,
    int Cancelaciones,
    IReadOnlyList<UsoPlataformaDiarioDto>? Diario = null
);

public interface IReportesClienteService
{
    Task<EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>> ConsultarAsistenciaPorSesionAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default);

    Task<EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>> ConsultarOcupacionAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default);

    Task<EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>> ConsultarInasistenciasAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default);

    Task<EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>> ConsultarHorasPicoAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default);

    Task<EstadoDeCarga<ReporteUsoPlataformaDto>> ConsultarUsoPlataformaAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default);

    Task<EstadoDeCarga<string>> DescargarCsvAsync(
        string reporte,
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default);
}
