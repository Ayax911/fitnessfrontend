using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// RF-025: el servicio de autenticación debe convertir CUALQUIER respuesta de error del backend en un mensaje para la UI,
// usando el cliente Kiota real (no un doble) contra un HttpMessageHandler que imita las respuestas de la API.
public class RegistroClienteHttpTests
{
    private sealed class ManejadorFijo : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string? _json;
        public ManejadorFijo(HttpStatusCode status, string? json) { _status = status; _json = json; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var r = new HttpResponseMessage(_status);
            if (_json != null) r.Content = new StringContent(_json, Encoding.UTF8, "application/json");
            return Task.FromResult(r);
        }
    }

    private sealed class AlmacenEnMemoria : ILocalStorageService
    {
        public ValueTask GuardarItemAsync(string clave, string valor) => ValueTask.CompletedTask;
        public ValueTask<string?> ObtenerItemAsync(string clave) => ValueTask.FromResult<string?>(null);
        public ValueTask EliminarItemAsync(string clave) => ValueTask.CompletedTask;
    }

    private static AutenticacionService Crear(HttpStatusCode status, string? json)
    {
        var http = new HttpClient(new ManejadorFijo(status, json)) { BaseAddress = new Uri("http://api.test") };
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http) { BaseUrl = "http://api.test" };
        var almacen = new AlmacenEnMemoria();
        return new AutenticacionService(new FitnessApiClient(adapter), almacen, new ProveedorEstadoAutenticacion(almacen));
    }

    private static Task<ResultadoOperacion> Registrar(AutenticacionService s) =>
        s.RegistrarAsync("ana@itm.edu.co", "Passw0rd!", "Ana Pérez", TipoUsuario.Profesor, true);

    [Fact]
    public async Task Registro_Exitoso204_RetornaExito()
    {
        var r = await Registrar(Crear(HttpStatusCode.NoContent, null));

        Assert.True(r.EsExitoso);
    }

    [Fact]
    public async Task Registro_CorreoDuplicado409_MuestraElMensajeDelBackend()
    {
        const string cuerpo = "{\"codigoError\":\"CorreoYaRegistrado\",\"mensaje\":\"Ya existe una cuenta con ese correo electrónico.\"}";

        var r = await Registrar(Crear(HttpStatusCode.Conflict, cuerpo));

        Assert.False(r.EsExitoso);
        Assert.Equal("Ya existe una cuenta con ese correo electrónico.", r.Mensaje);
        Assert.Equal("CorreoYaRegistrado", r.CodigoError);
    }

    [Fact]
    public async Task Registro_AutorizacionRequerida400_MuestraElMensajeDelBackend()
    {
        const string cuerpo = "{\"codigoError\":\"AutorizacionDatosRequerida\",\"mensaje\":\"Debe autorizar el tratamiento de sus datos personales para registrarse.\"}";

        var r = await Registrar(Crear(HttpStatusCode.BadRequest, cuerpo));

        Assert.Equal("AutorizacionDatosRequerida", r.CodigoError);
        Assert.Contains("autorizar", r.Mensaje);
    }

    [Fact]
    public async Task Registro_ContrasenaInvalida_SeMuestraEnEspanolNoLaReglaEnIngles()
    {
        const string cuerpo = "{\"codigoError\":\"ContrasenaInvalida\",\"mensaje\":\"Passwords must have at least one non alphanumeric character.\"}";

        var r = await Registrar(Crear(HttpStatusCode.BadRequest, cuerpo));

        Assert.Contains("símbolo", r.Mensaje);
        Assert.DoesNotContain("Passwords", r.Mensaje);
    }

    [Fact]
    public async Task Registro_Error500SinCuerpo_RetornaFalloControlado_NoExcepcion()
    {
        var r = await Registrar(Crear(HttpStatusCode.InternalServerError, null));

        Assert.False(r.EsExitoso);
        Assert.False(string.IsNullOrWhiteSpace(r.Mensaje));
    }
}
