using System;
using System.Globalization;

namespace FitnessFrontend.Servicios.Infraestructura;

// Patrón 3 (docs/arquitectura/04-patrones-de-diseno.md): Convierte fechas UTC exclusivamente a America/Bogota (UTC-5)
public class ZonaHorariaService : IZonaHorariaService
{
    private static readonly TimeZoneInfo ZonaBogota;
    private static readonly CultureInfo CulturaEsCo = new("es-CO");

    static ZonaHorariaService()
    {
        try
        {
            ZonaBogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch
        {
            try
            {
                ZonaBogota = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch
            {
                ZonaBogota = TimeZoneInfo.CreateCustomTimeZone(
                    "America/Bogota",
                    TimeSpan.FromHours(-5),
                    "America/Bogota",
                    "America/Bogota");
            }
        }
    }

    public DateTimeOffset AHoraLocal(DateTimeOffset utc)
    {
        return TimeZoneInfo.ConvertTime(utc, ZonaBogota);
    }

    public DateTimeOffset AUtc(DateTime local)
    {
        var localUnspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var offset = ZonaBogota.GetUtcOffset(localUnspecified);
        var dto = new DateTimeOffset(localUnspecified, offset);
        return dto.ToUniversalTime();
    }

    public DateTime ObtenerFechaHoyLocal()
    {
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ZonaBogota);
        return local.Date;
    }

    public string FormatearFechaHora(DateTimeOffset utc)
    {
        var local = AHoraLocal(utc);
        return local.ToString("dd/MM/yyyy hh:mm tt", CulturaEsCo);
    }

    public string FormatearHora(DateTimeOffset utc)
    {
        var local = AHoraLocal(utc);
        return local.ToString("hh:mm tt", CulturaEsCo);
    }

    public string FormatearFecha(DateTimeOffset utc)
    {
        var local = AHoraLocal(utc);
        return local.ToString("dd/MM/yyyy", CulturaEsCo);
    }

    public string FormatearRango(DateTimeOffset inicioUtc, DateTimeOffset finUtc)
    {
        var inicio = AHoraLocal(inicioUtc);
        var fin = AHoraLocal(finUtc);

        if (inicio.Date == fin.Date)
        {
            return $"{inicio.ToString("dd/MM/yyyy", CulturaEsCo)} de {inicio.ToString("hh:mm tt", CulturaEsCo)} a {fin.ToString("hh:mm tt", CulturaEsCo)}";
        }

        return $"{inicio.ToString("dd/MM/yyyy hh:mm tt", CulturaEsCo)} - {fin.ToString("dd/MM/yyyy hh:mm tt", CulturaEsCo)}";
    }
}
