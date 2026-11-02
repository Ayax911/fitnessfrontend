using System;

namespace FitnessFrontend.Servicios.Infraestructura;

// Formatos de fecha y hora de las pantallas del estudiante (diseño "Pantallas del estudiante"): "6:00 p. m.", "Vie 30 oct",
// "Sábado 31 de octubre". Solo presentación: recibe fechas YA convertidas a America/Bogotá por IZonaHorariaService
// (la conversión sigue siendo el único trabajo de ese servicio). No depende de la cultura del navegador.
public static class PresentacionFechas
{
    private static readonly string[] DiasCortos = { "dom", "lun", "mar", "mié", "jue", "vie", "sáb" };
    private static readonly string[] DiasLargos = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };
    private static readonly string[] MesesCortos = { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic" };
    private static readonly string[] MesesLargos = { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

    private static string Mayuscula(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>"6:00" (sin cero a la izquierda, formato de 12 horas).</summary>
    public static string Hora(DateTime local)
    {
        var h = local.Hour % 12;
        return $"{(h == 0 ? 12 : h)}:{local.Minute:00}";
    }

    /// <summary>"a. m." o "p. m."</summary>
    public static string Meridiano(DateTime local) => local.Hour < 12 ? "a. m." : "p. m.";

    /// <summary>"6:00 p. m."</summary>
    public static string HoraCompleta(DateTime local) => $"{Hora(local)} {Meridiano(local)}";

    /// <summary>"Hoy", "Mañana" o "Vie 30 oct".</summary>
    public static string DiaCorto(DateTime local, DateTime hoyLocal)
    {
        if (local.Date == hoyLocal.Date) return "Hoy";
        if (local.Date == hoyLocal.Date.AddDays(1)) return "Mañana";
        return FechaCorta(local);
    }

    /// <summary>"Vie 30 oct".</summary>
    public static string FechaCorta(DateTime local) =>
        $"{Mayuscula(DiasCortos[(int)local.DayOfWeek])} {local.Day} {MesesCortos[local.Month - 1]}";

    /// <summary>"Sábado 31 de octubre".</summary>
    public static string FechaLarga(DateTime local) =>
        $"{Mayuscula(DiasLargos[(int)local.DayOfWeek])} {local.Day} de {MesesLargos[local.Month - 1]}";

    /// <summary>Texto de los chips de día: ("Hoy", "sáb 31"), ("Mañana", "dom 1"), ("Lunes", "lun 2").</summary>
    public static (string Etiqueta, string Sub) Chip(DateTime dia, DateTime hoyLocal)
    {
        var sub = $"{DiasCortos[(int)dia.DayOfWeek]} {dia.Day}";
        if (dia.Date == hoyLocal.Date) return ("Hoy", sub);
        if (dia.Date == hoyLocal.Date.AddDays(1)) return ("Mañana", sub);
        return (Mayuscula(DiasLargos[(int)dia.DayOfWeek]), sub);
    }
}
