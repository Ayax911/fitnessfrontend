using System;

namespace FitnessFrontend.Servicios.Infraestructura;

// Patrón 3 (docs/arquitectura/04-patrones-de-diseno.md): Adapter de zona horaria institucional
public interface IZonaHorariaService
{
    DateTimeOffset AHoraLocal(DateTimeOffset utc);
    DateTimeOffset AUtc(DateTime local);
    DateTime ObtenerFechaHoyLocal();
    string FormatearFechaHora(DateTimeOffset utc);
    string FormatearHora(DateTimeOffset utc);
    string FormatearFecha(DateTimeOffset utc);
    string FormatearRango(DateTimeOffset inicioUtc, DateTimeOffset finUtc);
}
