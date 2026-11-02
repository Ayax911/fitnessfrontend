using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Bloqueos;

// RF-031: Tipos de bloqueo de calendario. Mismos valores que el backend (0..5)
public enum TipoBloqueo
{
    Festivo = 0,
    Mantenimiento = 1,
    Cierre = 2,
    EventoInstitucional = 3,
    Emergencia = 4,
    Otro = 5
}

/// <summary>Nombres para mostrar de cada tipo de cierre (el Administrador puede usar cualquiera).</summary>
public static class TiposBloqueoUi
{
    public static readonly IReadOnlyList<(string Valor, string Etiqueta)> Todos = new List<(string, string)>
    {
        (nameof(TipoBloqueo.Festivo), "Festivo institucional o nacional"),
        (nameof(TipoBloqueo.Mantenimiento), "Mantenimiento de instalaciones"),
        (nameof(TipoBloqueo.Cierre), "Cierre extraordinario"),
        (nameof(TipoBloqueo.EventoInstitucional), "Evento institucional"),
        (nameof(TipoBloqueo.Emergencia), "Emergencia o seguridad"),
        (nameof(TipoBloqueo.Otro), "Otro motivo")
    };

    public static string Etiqueta(string valor) =>
        Todos.FirstOrDefault(t => string.Equals(t.Valor, valor, StringComparison.OrdinalIgnoreCase)).Etiqueta ?? valor;

    public static string ClaseInsignia(string valor) => valor switch
    {
        nameof(TipoBloqueo.Festivo) => "bg-info text-dark",
        nameof(TipoBloqueo.Mantenimiento) => "bg-warning text-dark",
        nameof(TipoBloqueo.Emergencia) => "bg-danger",
        nameof(TipoBloqueo.EventoInstitucional) => "bg-primary",
        _ => "bg-secondary"
    };
}

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
