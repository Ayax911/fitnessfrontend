using Bunit;
using FitnessFrontend.Componentes.Compartidos;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Rediseño visual (docs/arquitectura/09-rediseno-visual.md): la insignia nunca depende solo del color y la carga se anuncia con texto.
public class InsigniaYEsqueletosTests : BunitContext
{
    [Theory]
    [InlineData("ok")]
    [InlineData("espera")]
    [InlineData("asistida")]
    [InlineData("no")]
    [InlineData("cancelada")]
    [InlineData("ingreso")]
    public void InsigniaEstado_LlevaIconoYPalabra_EnCadaEstado(string tipo)
    {
        var cut = Render<InsigniaEstado>(p => p.Add(x => x.Tipo, tipo).AddChildContent("Texto del estado"));

        Assert.Contains($"itm-estado--{tipo}", cut.Markup);
        Assert.Contains("Texto del estado", cut.Markup);
        Assert.NotNull(cut.Find("svg[aria-hidden='true']"));
    }

    [Fact]
    public void EsqueletoTarjetas_SeAnunciaConTexto_YEsDecorativo()
    {
        var cut = Render<EsqueletoTarjetas>(p => p.Add(x => x.Cantidad, 2).Add(x => x.Mensaje, "Cargando actividades..."));

        var contenedor = cut.Find("[role='status']");
        Assert.Equal("true", contenedor.GetAttribute("aria-busy"));
        Assert.Contains("Cargando actividades...", cut.Find(".itm-solo-lectores").TextContent);
        Assert.Equal(2, cut.FindAll("article[aria-hidden='true']").Count);
    }
}
