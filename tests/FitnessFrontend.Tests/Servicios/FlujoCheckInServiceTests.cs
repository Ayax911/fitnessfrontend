using FitnessFrontend.Servicios.Asistencia;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// Patrón 6: Pruebas unitarias puras del servicio de decisión de check-in
public class FlujoCheckInServiceTests
{
    private readonly FlujoCheckInService _servicio = new();

    [Fact]
    public void EvaluarInicio_ConTokenVacio_DevuelveCasoCTokenInvalido()
    {
        var resultadoNull = _servicio.EvaluarInicio(null, estaAutenticado: true);
        var resultadoVacio = _servicio.EvaluarInicio("", estaAutenticado: true);
        var resultadoEspacios = _servicio.EvaluarInicio("   ", estaAutenticado: false);

        Assert.Equal(TipoDecisionCheckIn.CasoC_TokenInvalido, resultadoNull.Tipo);
        Assert.Equal(TipoDecisionCheckIn.CasoC_TokenInvalido, resultadoVacio.Tipo);
        Assert.Equal(TipoDecisionCheckIn.CasoC_TokenInvalido, resultadoEspacios.Tipo);
    }

    [Fact]
    public void EvaluarInicio_SinSesionIniciada_DevuelveCasoBConReturnUrlHaciaCheckIn()
    {
        var token = "token-qr-123";
        var resultado = _servicio.EvaluarInicio(token, estaAutenticado: false);

        Assert.Equal(TipoDecisionCheckIn.CasoB_RequiereAutenticacion, resultado.Tipo);
        Assert.Equal(token, resultado.TokenSesion);
        Assert.Equal("/checkin/token-qr-123", resultado.ReturnUrl);
    }

    [Fact]
    public void EvaluarInicio_ConSesionIniciada_DevuelveCasoAProcederCheckIn()
    {
        var token = "token-qr-valido";
        var resultado = _servicio.EvaluarInicio(token, estaAutenticado: true);

        Assert.Equal(TipoDecisionCheckIn.CasoA_ProcederCheckIn, resultado.Tipo);
        Assert.Equal(token, resultado.TokenSesion);
    }
}
