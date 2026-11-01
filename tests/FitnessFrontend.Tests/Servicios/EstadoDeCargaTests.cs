using FitnessFrontend.Servicios.Infraestructura;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class EstadoDeCargaTests
{
    [Fact]
    public void EstadoCargando_DebeTenerPropiedadesCorrectas()
    {
        var estado = EstadoDeCarga<string>.Cargando();

        Assert.True(estado.EsCargando);
        Assert.False(estado.EsExito);
        Assert.False(estado.EsError);
        Assert.Null(estado.Valor);
        Assert.Null(estado.MensajeError);

        var matchResult = estado.Match(
            onCargando: () => "cargando",
            onExito: val => "exito",
            onError: (err, cod) => "error");

        Assert.Equal("cargando", matchResult);
    }

    [Fact]
    public void EstadoCorrecto_DebeContenerValor()
    {
        var estado = EstadoDeCarga<int>.Correcto(42);

        Assert.False(estado.EsCargando);
        Assert.True(estado.EsExito);
        Assert.False(estado.EsError);
        Assert.Equal(42, estado.Valor);
        Assert.Null(estado.MensajeError);

        var matchResult = estado.Match(
            onCargando: () => -1,
            onExito: val => val * 2,
            onError: (err, cod) => -2);

        Assert.Equal(84, matchResult);
    }

    [Fact]
    public void EstadoFallo_DebeContenerMensajeYCodigo()
    {
        var estado = EstadoDeCarga<string>.Fallo("Sin conexión", "ErrorRed");

        Assert.False(estado.EsCargando);
        Assert.False(estado.EsExito);
        Assert.True(estado.EsError);
        Assert.Null(estado.Valor);
        Assert.Equal("Sin conexión", estado.MensajeError);
        Assert.Equal("ErrorRed", estado.CodigoError);

        var matchResult = estado.Match(
            onCargando: () => "cargando",
            onExito: val => "exito",
            onError: (err, cod) => $"{err}:{cod}");

        Assert.Equal("Sin conexión:ErrorRed", matchResult);
    }
}
