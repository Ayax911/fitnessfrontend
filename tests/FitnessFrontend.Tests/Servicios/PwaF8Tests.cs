using System.IO;
using System.Text.Json;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class PwaF8Tests
{
    [Fact]
    public void ManifiestoPwa_ExisteYContienePropiedadesRequeridas()
    {
        // F8: Manifiesto webmanifest con standalone y color institucional
        var rutaManifiesto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FitnessFrontend/wwwroot/manifest.webmanifest"));
        Assert.True(File.Exists(rutaManifiesto), $"No se encontró el manifiesto en {rutaManifiesto}");

        var json = File.ReadAllText(rutaManifiesto);
        using var doc = JsonDocument.Parse(json);
        var raiz = doc.RootElement;

        Assert.Equal("Gimnasio ITM — Bienestar Universitario", raiz.GetProperty("name").GetString());
        Assert.Equal("standalone", raiz.GetProperty("display").GetString());
        Assert.Equal("#011C5B", raiz.GetProperty("theme_color").GetString());
        Assert.Equal("#011C5B", raiz.GetProperty("background_color").GetString());

        var iconos = raiz.GetProperty("icons");
        Assert.True(iconos.GetArrayLength() >= 2);
    }

    [Fact]
    public void IndexHtml_VinculaElManifiestoWeb()
    {
        var rutaIndex = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FitnessFrontend/wwwroot/index.html"));
        Assert.True(File.Exists(rutaIndex));

        var contenido = File.ReadAllText(rutaIndex);
        Assert.Contains("rel=\"manifest\"", contenido);
        Assert.Contains("href=\"manifest.webmanifest\"", contenido);
    }
}
