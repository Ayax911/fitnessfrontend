using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

public class AccesibilidadF9Tests
{
    [Fact]
    public void TodaPantallaEnPaginas_TieneComentarioConSuRfDeOrigen()
    {
        // Regla no negociable 3 del AGENTS.md:
        // Toda pantalla lleva el ID de su RF de origen en un comentario al inicio del componente.
        var rutaPaginas = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FitnessFrontend/Paginas"));
        var archivosRazor = Directory.GetFiles(rutaPaginas, "*.razor", SearchOption.AllDirectories);

        Assert.NotEmpty(archivosRazor);

        var regexRf = new Regex(@"@\*\s*RF-\d{3}", RegexOptions.IgnoreCase);

        foreach (var archivo in archivosRazor)
        {
            var nombre = Path.GetFileName(archivo);
            var contenido = File.ReadAllText(archivo);

            // Se busca en los primeros 1000 caracteres
            var encabezado = contenido.Length > 1000 ? contenido.Substring(0, 1000) : contenido;
            Assert.True(
                regexRf.IsMatch(encabezado),
                $"La pantalla '{nombre}' en '{archivo}' no tiene el comentario con su RF de origen en el encabezado.");
        }
    }

    [Fact]
    public void SkipLink_ExisteEnIndexHtmlParaNavegacionPorTeclado()
    {
        // WCAG 2.1 AA (2.4.1 Bypass Blocks): enlace para saltar al contenido principal
        var rutaIndex = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FitnessFrontend/wwwroot/index.html"));
        var html = File.ReadAllText(rutaIndex);

        Assert.Contains("class=\"skip-link\"", html);
        Assert.Contains("href=\"#main-content\"", html);
    }

    [Fact]
    public void AusenciaDeEstilosHardcodeados_EnPaginasRazor()
    {
        // Regla no negociable 6: Ningún color o fuente fuera de itm-tema.css
        var rutaPaginas = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FitnessFrontend/Paginas"));
        var archivosRazor = Directory.GetFiles(rutaPaginas, "*.razor", SearchOption.AllDirectories);

        var regexColorProhibido = new Regex(@"color:\s*#(?!003865|011C5B|107c10|d9534f|ffffff)[0-9a-fA-F]{3,6}", RegexOptions.IgnoreCase);

        foreach (var archivo in archivosRazor)
        {
            var contenido = File.ReadAllText(archivo);
            var coincidencia = regexColorProhibido.Match(contenido);
            Assert.False(
                coincidencia.Success,
                $"El archivo '{Path.GetFileName(archivo)}' tiene un color hardcodeado no institucional: '{coincidencia.Value}'.");
        }
    }
}
