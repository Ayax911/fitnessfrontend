using Bunit;
using FitnessFrontend.Componentes.Compartidos;
using InfraEstadoDeCargaNullable = FitnessFrontend.Servicios.Infraestructura.EstadoDeCarga<string?>;
using InfraEstadoDeCarga = FitnessFrontend.Servicios.Infraestructura.EstadoDeCarga<string>;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// I-11: Verificación de EstadoDeCarga cuando EsExito == true y Valor == null
public class EstadoDeCargaComponenteTests : BunitContext
{
    [Fact]
    public void EstadoDeCarga_I11_CuandoValorEsNull_NoQuedaEnBlanco_MuestraMensajeDeDatosNoEncontrados()
    {
        var estado = InfraEstadoDeCargaNullable.Correcto(null);
        var cut = Render<EstadoDeCarga<string?>>(parameters => parameters
            .Add(p => p.Estado, estado)
            .Add(p => p.ExitoTemplate, val => (builder) => builder.AddContent(0, $"Valor: {val}")));

        var markup = cut.Markup;
        Assert.NotEmpty(markup);
        Assert.Contains("No se encontraron datos", markup);
    }

    [Fact]
    public void EstadoDeCarga_CuandoValorExiste_RenderizaExitoTemplate()
    {
        var estado = InfraEstadoDeCarga.Correcto("Contenido Cargado");
        var cut = Render<EstadoDeCarga<string>>(parameters => parameters
            .Add(p => p.Estado, estado)
            .Add(p => p.ExitoTemplate, val => (builder) => builder.AddContent(0, val)));

        var markup = cut.Markup;
        Assert.Contains("Contenido Cargado", markup);
    }
}
