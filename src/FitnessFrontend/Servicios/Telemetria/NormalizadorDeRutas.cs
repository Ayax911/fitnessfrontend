using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FitnessFrontend.Servicios.Telemetria;

// RF-033 (analítica de uso): convierte la URL visitada en una ruta SIN datos de personas ni de secretos.
// "/admin/sesiones/0b94…" → "/admin/sesiones/{id}";  "/checkin/AbC.d" → "/checkin/{id}";  se descartan parámetros y fragmento.
public static class NormalizadorDeRutas
{
    // El backend rechaza rutas de más de 80 caracteres; aquí se evita siquiera enviarlas
    public const int LongitudMaxima = 80;

    // Una palabra de ruta estática: minúsculas y guiones ("puntos-acceso", "mi-historial"). Cualquier otra cosa es un valor.
    private static readonly Regex PalabraDeRuta = new(@"^[a-z]+(-[a-z]+)*$", RegexOptions.Compiled);

    // Después de estas palabras viaja un token de QR (aunque sean solo letras minúsculas): nunca se conserva
    private static readonly string[] PalabrasConToken = { "checkin", "ingreso" };

    /// <summary>Ruta normalizada, o null si no se debe registrar.</summary>
    public static string? Normalizar(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var ruta = url.Trim();
        // Solo una URL con esquema ("https://…") se interpreta como absoluta; en Linux, .NET toma "/eventos?x=1" por una ruta de archivo
        if (ruta.Contains("://") && Uri.TryCreate(ruta, UriKind.Absolute, out var absoluta)) ruta = absoluta.AbsolutePath;

        var corte = ruta.IndexOfAny(new[] { '?', '#' });
        if (corte >= 0) ruta = ruta[..corte];

        ruta = ruta.ToLowerInvariant();
        var segmentos = ruta.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmentos.Length == 0) return "/";

        var resultado = new string[segmentos.Length];
        for (var i = 0; i < segmentos.Length; i++)
        {
            var anterior = i > 0 ? segmentos[i - 1] : null;
            var esValor = anterior != null && PalabrasConToken.Contains(anterior);
            resultado[i] = !esValor && segmentos[i].Length <= 30 && PalabraDeRuta.IsMatch(segmentos[i])
                ? segmentos[i]
                : "{id}";
        }

        var normalizada = "/" + string.Join('/', resultado);
        return normalizada.Length > LongitudMaxima ? null : normalizada;
    }
}
