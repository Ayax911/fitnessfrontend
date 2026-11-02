using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.Componentes.Compartidos;
using FitnessFrontend.Servicios.Telemetria;
using FitnessFrontend.Tests.Servicios;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Telemetria;

// RF-033 (analítica de uso): la telemetría de pantallas es anónima y nunca debe molestar al usuario.
public class NormalizadorDeRutasTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("/eventos", "/eventos")]
    [InlineData("https://gimnasio-itm.duckdns.org/eventos", "/eventos")]
    [InlineData("/eventos?pagina=2#arriba", "/eventos")]                                   // sin parámetros ni fragmento
    [InlineData("/Admin/Reportes", "/admin/reportes")]                                     // minúsculas
    [InlineData("/admin/puntos-acceso", "/admin/puntos-acceso")]                           // palabras con guion se conservan
    [InlineData("/admin/sesiones/0b9429d3-88f4-4c1a-9890-00c675b02306", "/admin/sesiones/{id}")]
    [InlineData("/entrenador/sesiones/0b9429d3-88f4-4c1a-9890-00c675b02306/inscritos", "/entrenador/sesiones/{id}/inscritos")]
    [InlineData("/admin/priorizacion/usuarios/0b9429d3-88f4-4c1a-9890-00c675b02306", "/admin/priorizacion/usuarios/{id}")]
    [InlineData("/admin/usuarios/persona@itm.edu.co", "/admin/usuarios/{id}")]            // un correo nunca se conserva
    public void Normaliza_RutasSinDatosDePersonasNiIdentificadores(string? entrada, string? esperado)
    {
        Assert.Equal(esperado, NormalizadorDeRutas.Normalizar(entrada));
    }

    // El token del QR viaja en la ruta: no puede quedar guardado, ni siquiera si solo tiene letras minúsculas
    [Theory]
    [InlineData("/checkin/AbC123.dEf=", "/checkin/{id}")]
    [InlineData("/checkin/sololetras", "/checkin/{id}")]
    [InlineData("/ingreso/sololetras", "/ingreso/{id}")]
    [InlineData("https://gimnasio-itm.duckdns.org/ingreso/TOKEN-QR-secreto?x=1", "/ingreso/{id}")]
    public void Normaliza_NuncaConservaElTokenDeQr(string entrada, string esperado)
    {
        var resultado = NormalizadorDeRutas.Normalizar(entrada);

        Assert.Equal(esperado, resultado);
        Assert.DoesNotContain("sololetras", resultado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secreto", resultado, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Normaliza_UnaPalabraLarguisimaNoSeConservaYSeRegistraComoValor()
    {
        Assert.Equal("/{id}", NormalizadorDeRutas.Normalizar("/" + new string('a', 200)));
    }

    [Fact]
    public void Normaliza_UnaRutaMasLargaQueElMaximoNoSeRegistra()
    {
        var larga = "/" + string.Join('/', Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz", 4));

        Assert.True(larga.Length > NormalizadorDeRutas.LongitudMaxima);
        Assert.Null(NormalizadorDeRutas.Normalizar(larga));
    }

    // Todo lo que produce el normalizador debe cumplir el patrón que el backend acepta
    [Theory]
    [InlineData("/ADMIN/Sesiones/ÁÉÍ")]
    [InlineData("/a b/c%20d")]
    [InlineData("/<script>alert(1)</script>")]
    [InlineData("/eventos/12345")]
    public void Normaliza_LoQueEnviaCumpleElPatronDelBackend(string entrada)
    {
        var resultado = NormalizadorDeRutas.Normalizar(entrada);

        Assert.NotNull(resultado);
        Assert.Matches(@"^/[a-z0-9\-_/{}]*$", resultado);
    }
}

public class TelemetriaServiceTests : BunitContext
{
    private sealed record Peticion(string Metodo, string Ruta, string Cuerpo);

    private (TelemetriaService Servicio, List<Peticion> Peticiones) Crear(HttpStatusCode respuesta = HttpStatusCode.NoContent, string? dispositivo = "movil")
    {
        JSInterop.Setup<string?>("itmTelemetria.dispositivo").SetResult(dispositivo);
        var peticiones = new List<Peticion>();
        var cliente = KiotaTestHelper.CrearCliente(req =>
        {
            peticiones.Add(new Peticion(req.Method.Method, req.RequestUri!.AbsolutePath, req.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? ""));
            return new HttpResponseMessage(respuesta);
        });
        return (new TelemetriaService(cliente, JSInterop.JSRuntime), peticiones);
    }

    [Fact]
    public async Task EnviaSoloLaRutaNormalizadaYElDispositivo()
    {
        var (servicio, peticiones) = Crear();

        await servicio.RegistrarVistaAsync("https://gimnasio-itm.duckdns.org/admin/sesiones/0b9429d3-88f4-4c1a-9890-00c675b02306?token=secreto");

        var p = Assert.Single(peticiones);
        Assert.Equal("POST", p.Metodo);
        Assert.Equal("/api/telemetria/vistas", p.Ruta);
        var json = JsonDocument.Parse(p.Cuerpo).RootElement;
        Assert.Equal("/admin/sesiones/{id}", json.GetProperty("pantalla").GetString());
        Assert.Equal("movil", json.GetProperty("dispositivo").GetString());
        // Ni el GUID, ni el parámetro, ni nada más viaja en la petición
        Assert.DoesNotContain("0b9429d3", p.Cuerpo);
        Assert.DoesNotContain("secreto", p.Cuerpo);
        Assert.Equal(2, json.EnumerateObject().Count());
    }

    [Fact]
    public async Task NoRepiteLaMismaPantallaSeguidaPeroSiRegistraUnaDistintaYLaVueltaA()
    {
        var (servicio, peticiones) = Crear();

        await servicio.RegistrarVistaAsync("/eventos");
        await servicio.RegistrarVistaAsync("/eventos?pagina=2");   // misma pantalla
        await servicio.RegistrarVistaAsync("/reservas");
        await servicio.RegistrarVistaAsync("/eventos");            // vuelve: cuenta de nuevo

        Assert.Equal(3, peticiones.Count);
    }

    [Fact]
    public async Task SiElNavegadorPidioNoRastrear_NoEnviaNada()
    {
        var (servicio, peticiones) = Crear(dispositivo: null);

        await servicio.RegistrarVistaAsync("/eventos");

        Assert.Empty(peticiones);
    }

    [Fact]
    public async Task UnaRutaQueNoSePuedeRegistrarNoSeEnvia()
    {
        var (servicio, peticiones) = Crear();

        await servicio.RegistrarVistaAsync("");
        await servicio.RegistrarVistaAsync(null);
        await servicio.RegistrarVistaAsync("/" + string.Join('/', Enumerable.Repeat("abcdefghij", 10)));   // más de 80 caracteres

        Assert.Empty(peticiones);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task UnFalloDelServidorNuncaLlegaAlUsuario(HttpStatusCode respuesta)
    {
        var (servicio, peticiones) = Crear(respuesta);

        var excepcion = await Record.ExceptionAsync(() => servicio.RegistrarVistaAsync("/eventos"));

        Assert.Null(excepcion);
        Assert.Single(peticiones);
    }

    [Fact]
    public async Task UnFalloDeRedOSinJavaScriptNuncaLlegaAlUsuario()
    {
        // Sin registrar la función JS: la llamada falla
        JSInterop.Mode = JSRuntimeMode.Strict;
        var cliente = KiotaTestHelper.CrearCliente(_ => throw new HttpRequestException("sin red"));
        var servicio = new TelemetriaService(cliente, JSInterop.JSRuntime);

        var excepcion = await Record.ExceptionAsync(() => servicio.RegistrarVistaAsync("/eventos"));

        Assert.Null(excepcion);
    }
}

public class RegistroDeNavegacionTests : BunitContext
{
    private sealed class TelemetriaFalsa : ITelemetriaService
    {
        public List<string?> Urls { get; } = new();

        public Task RegistrarVistaAsync(string? url, CancellationToken ct = default)
        {
            Urls.Add(url);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void RegistraLaPrimeraPantallaYCadaNavegacion_YDejaDeEscucharAlDestruirse()
    {
        var telemetria = new TelemetriaFalsa();
        Services.AddSingleton<ITelemetriaService>(telemetria);
        var navegacion = Services.GetRequiredService<NavigationManager>();

        var cut = Render<RegistroDeNavegacion>();
        Assert.Single(telemetria.Urls);                                 // la pantalla con la que se entró

        navegacion.NavigateTo("/reservas");
        navegacion.NavigateTo("/historial");
        Assert.Equal(3, telemetria.Urls.Count);
        Assert.EndsWith("/historial", telemetria.Urls[^1]);

        cut.Instance.Dispose();                                         // Blazor lo hace al salir de la página: debe soltar el evento
        navegacion.NavigateTo("/eventos");
        Assert.Equal(3, telemetria.Urls.Count);                         // ya no escucha
    }

    [Fact]
    public void NoPintaNada()
    {
        Services.AddSingleton<ITelemetriaService>(new TelemetriaFalsa());

        var cut = Render<RegistroDeNavegacion>();

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }
}
