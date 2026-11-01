using System;
using System.Text;

namespace FitnessFrontend.Servicios.Calendario;

// RF-036: Generador de archivos .ics (RFC 5545) puro para exportar reservas confirmadas
public static class GeneradorIcs
{
    private const int MaxOctetosLinea = 75;

    public record DatosEventoIcs(
        Guid ReservaId,
        string Titulo,
        DateTimeOffset InicioUtc,
        DateTimeOffset FinUtc,
        string? Descripcion = null,
        string? Ubicacion = null);

    public static string Generar(DatosEventoIcs evento, DateTimeOffset? selloTiempoUtc = null)
    {
        var stamp = (selloTiempoUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("yyyyMMddTHHmmssZ");
        var dtStart = evento.InicioUtc.UtcDateTime.ToString("yyyyMMddTHHmmssZ");
        var dtEnd = evento.FinUtc.UtcDateTime.ToString("yyyyMMddTHHmmssZ");

        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//ITM Bienestar//Gimnasio Universitario//ES\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        sb.Append("METHOD:PUBLISH\r\n");
        sb.Append("BEGIN:VEVENT\r\n");

        AgregarPropiedadPlegada(sb, $"UID:{evento.ReservaId}@gimnasio.itm");
        AgregarPropiedadPlegada(sb, $"DTSTAMP:{stamp}");
        AgregarPropiedadPlegada(sb, $"DTSTART:{dtStart}");
        AgregarPropiedadPlegada(sb, $"DTEND:{dtEnd}");
        AgregarPropiedadPlegada(sb, $"SUMMARY:{EscaparTexto(evento.Titulo)}");

        if (!string.IsNullOrWhiteSpace(evento.Descripcion))
        {
            AgregarPropiedadPlegada(sb, $"DESCRIPTION:{EscaparTexto(evento.Descripcion)}");
        }

        if (!string.IsNullOrWhiteSpace(evento.Ubicacion))
        {
            AgregarPropiedadPlegada(sb, $"LOCATION:{EscaparTexto(evento.Ubicacion)}");
        }

        sb.Append("STATUS:CONFIRMED\r\n");
        sb.Append("END:VEVENT\r\n");
        sb.Append("END:VCALENDAR\r\n");

        return sb.ToString();
    }

    public static string EscaparTexto(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;

        return texto
            .Replace(@"\", @"\\")
            .Replace(";", @"\;")
            .Replace(",", @"\,")
            .Replace("\r\n", @"\n")
            .Replace("\n", @"\n")
            .Replace("\r", @"\n");
    }

    public static void AgregarPropiedadPlegada(StringBuilder sb, string linea)
    {
        var bytes = Encoding.UTF8.GetBytes(linea);
        if (bytes.Length <= MaxOctetosLinea)
        {
            sb.Append(linea);
            sb.Append("\r\n");
            return;
        }

        int offset = 0;
        bool primeraLinea = true;

        while (offset < bytes.Length)
        {
            int limite = primeraLinea ? MaxOctetosLinea : (MaxOctetosLinea - 1); // 1 octeto para el espacio inicial al plegar
            int tamanoChunk = Math.Min(limite, bytes.Length - offset);

            // Evitar cortar a la mitad de un carácter multibyte UTF-8
            while (tamanoChunk > 0 && offset + tamanoChunk < bytes.Length && (bytes[offset + tamanoChunk] & 0xC0) == 0x80)
            {
                tamanoChunk--;
            }

            var chunkTexto = Encoding.UTF8.GetString(bytes, offset, tamanoChunk);

            if (!primeraLinea)
            {
                sb.Append(' ');
            }

            sb.Append(chunkTexto);
            sb.Append("\r\n");

            offset += tamanoChunk;
            primeraLinea = false;
        }
    }
}
