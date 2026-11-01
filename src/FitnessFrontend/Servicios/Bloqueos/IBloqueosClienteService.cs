using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Bloqueos;

// RF-031: Fachada para consulta y administración de bloqueos de calendario
public record BloqueoCalendarioDto(
    Guid Id,
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    string Motivo,
    string Tipo);

public record CrearBloqueoRequest(
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    string Motivo,
    string Tipo);

public interface IBloqueosClienteService
{
    Task<EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>> ObtenerBloqueosAsync(DateTimeOffset? desde = null, DateTimeOffset? hasta = null, CancellationToken ct = default);
    Task<ResultadoOperacion> CrearBloqueoAsync(CrearBloqueoRequest request, CancellationToken ct = default);
    Task<ResultadoOperacion> EliminarBloqueoAsync(Guid bloqueoId, CancellationToken ct = default);
}
