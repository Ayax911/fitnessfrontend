using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Reportes;

// Fachada para RF-033 (Reportes administrativos)
public class ReportesClienteService : IReportesClienteService
{
    private readonly IZonaHorariaService _zonaHoraria;

    public ReportesClienteService(IZonaHorariaService zonaHoraria)
    {
        _zonaHoraria = zonaHoraria;
    }

    public Task<EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>> ConsultarAsistenciaPorSesionAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        var lista = new List<ReporteAsistenciaSesionDto>
        {
            new(Guid.NewGuid(), "Spinning Matutino", DateTimeOffset.UtcNow.AddDays(-2), 20, 18, 16, 2, 88.8),
            new(Guid.NewGuid(), "Crossfit Intenso", DateTimeOffset.UtcNow.AddDays(-1), 15, 15, 14, 1, 93.3),
            new(Guid.NewGuid(), "Natación Libre", DateTimeOffset.UtcNow, 25, 20, 18, 2, 90.0)
        };
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Correcto(lista));
    }

    public Task<EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>> ConsultarOcupacionAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        var lista = new List<ReporteOcupacionDto>
        {
            new("Spinning Matutino", _zonaHoraria.ObtenerFechaHoyLocal().AddDays(-2), 20, 18, 90.0),
            new("Crossfit Intenso", _zonaHoraria.ObtenerFechaHoyLocal().AddDays(-1), 15, 15, 100.0),
            new("Natación Libre", _zonaHoraria.ObtenerFechaHoyLocal(), 25, 20, 80.0)
        };
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Correcto(lista));
    }

    public Task<EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>> ConsultarInasistenciasAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        var lista = new List<ReporteInasistenciasEventoDto>
        {
            new("Spinning Matutino", 50, 4, 8.0),
            new("Crossfit Intenso", 40, 2, 5.0),
            new("Natación Libre", 60, 5, 8.3)
        };
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Correcto(lista));
    }

    public Task<EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>> ConsultarHorasPicoAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        var lista = new List<ReporteHorasPicoDto>
        {
            new("Lunes", 7, 45),
            new("Lunes", 18, 62),
            new("Martes", 7, 40),
            new("Martes", 18, 58),
            new("Miércoles", 17, 70),
            new("Jueves", 18, 55),
            new("Viernes", 16, 50)
        };
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Correcto(lista));
    }

    public Task<EstadoDeCarga<ReporteUsoPlataformaDto>> ConsultarUsoPlataformaAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        var dto = new ReporteUsoPlataformaDto(
            RegistrosNuevos: 34,
            UsuariosActivos: 185,
            ReservasCreadas: 412,
            Cancelaciones: 28);

        return Task.FromResult(EstadoDeCarga<ReporteUsoPlataformaDto>.Correcto(dto));
    }
}
