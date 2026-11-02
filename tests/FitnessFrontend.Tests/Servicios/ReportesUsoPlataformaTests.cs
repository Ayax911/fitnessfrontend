using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reportes;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// Observación de uso (2026-11-02): "Uso de plataforma" fallaba con DeserializeUnableToConvertValue porque el backend
// devuelve una fila por día (lista) y el cliente esperaba un solo objeto.
public class ReportesUsoPlataformaTests
{
    [Fact]
    public async Task UsoPlataforma_LeeLaListaDiaria_YSumaLosTotales()
    {
        const string json = @"[
            {""fecha"":""2026-11-01"",""registrosNuevos"":2,""usuariosActivos"":5,""reservasCreadas"":7,""cancelaciones"":1},
            {""fecha"":""2026-11-02"",""registrosNuevos"":3,""usuariosActivos"":9,""reservasCreadas"":4,""cancelaciones"":2}
        ]";
        var cliente = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Contains("/api/reportes/uso-plataforma", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });

        var res = await new ReportesClienteService(cliente, new ZonaHorariaService())
            .ConsultarUsoPlataformaAsync(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow);

        Assert.True(res.EsExito, res.MensajeError);
        Assert.Equal(5, res.Valor!.RegistrosNuevos);
        Assert.Equal(11, res.Valor.ReservasCreadas);
        Assert.Equal(3, res.Valor.Cancelaciones);
        Assert.Equal(9, res.Valor.UsuariosActivos); // máximo de un día, no la suma
        Assert.Equal(2, res.Valor.Diario!.Count);
        Assert.Equal(new DateTime(2026, 11, 1), res.Valor.Diario[0].Fecha);
    }

    [Fact]
    public async Task UsoPlataforma_SinDatos_DevuelveCeros()
    {
        var cliente = KiotaTestHelper.CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") });

        var res = await new ReportesClienteService(cliente, new ZonaHorariaService())
            .ConsultarUsoPlataformaAsync(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow);

        Assert.True(res.EsExito);
        Assert.Equal(0, res.Valor!.ReservasCreadas);
        Assert.Equal(0, res.Valor.UsuariosActivos);
    }

    [Fact]
    public async Task HorasPico_LeeLaRespuestaRealDelBackend()
    {
        const string json = @"[{""diaSemana"":1,""diaSemanaNombre"":""Lunes"",""franjaHoraria"":19,""totalCheckInsGeneral"":10,""totalCheckInsSesion"":5,""totalIngresos"":15}]";
        var cliente = KiotaTestHelper.CrearCliente(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

        var res = await new ReportesClienteService(cliente, new ZonaHorariaService())
            .ConsultarHorasPicoAsync(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow);

        Assert.True(res.EsExito, res.MensajeError);
        var fila = Assert.Single(res.Valor!);
        Assert.Equal("Lunes", fila.DiaSemana);
        Assert.Equal(19, fila.FranjaHora);
        Assert.Equal(15, fila.TotalIngresosYCheckins);
    }
}
