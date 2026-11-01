using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// RF-013: el bearer token de Identity es OPACO (no JWT). Antes el frontend intentaba leer los roles del token, no obtenía
// ninguno y TODA persona quedaba como "Usuario": Entrenador y Administrador no podían usar sus pantallas.
// Estas pruebas fijan que los roles vienen del perfil que entrega el backend (GET /api/usuarios/yo).
public class SesionConRolesTests
{
    private const string TokenOpaco = "CfDJ8OpacoSinPuntosNiClaimsLegibles";   // formato real de Identity: sin '.'

    private sealed class AlmacenEnMemoria : ILocalStorageService
    {
        public Dictionary<string, string> Datos { get; } = new();
        public ValueTask GuardarItemAsync(string clave, string valor) { Datos[clave] = valor; return ValueTask.CompletedTask; }
        public ValueTask<string?> ObtenerItemAsync(string clave) => ValueTask.FromResult(Datos.TryGetValue(clave, out var v) ? v : null);
        public ValueTask EliminarItemAsync(string clave) { Datos.Remove(clave); return ValueTask.CompletedTask; }
    }

    private sealed class ManejadorPorRuta : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, (HttpStatusCode, string?)> _responder;
        public List<string> Rutas { get; } = new();
        public ManejadorPorRuta(Func<HttpRequestMessage, (HttpStatusCode, string?)> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Rutas.Add($"{request.Method} {request.RequestUri!.AbsolutePath}");
            var (status, json) = _responder(request);
            var r = new HttpResponseMessage(status);
            if (json != null) r.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return Task.FromResult(r);
        }
    }

    private static string Jwt(string payloadJson)
    {
        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{B64("{\"alg\":\"none\"}")}.{B64(payloadJson)}.firma";
    }

    // ---------- ProveedorEstadoAutenticacion ----------

    [Fact]
    public async Task TokenOpaco_ConPerfilGuardado_AplicaCorreoYRolesDelBackend()
    {
        var almacen = new AlmacenEnMemoria();
        var id = Guid.NewGuid();
        almacen.Datos[ProveedorEstadoAutenticacion.ClaveToken] = TokenOpaco;
        almacen.Datos[ProveedorEstadoAutenticacion.ClavePerfil] =
            ProveedorEstadoAutenticacion.SerializarPerfil(new PerfilSesion(id, "admin@itm.edu.co", new[] { "Administrador", "Usuario" }));

        var usuario = (await new ProveedorEstadoAutenticacion(almacen).GetAuthenticationStateAsync()).User;

        Assert.True(usuario.Identity!.IsAuthenticated);
        Assert.True(usuario.IsInRole("Administrador"));
        Assert.True(usuario.IsInRole("Usuario"));
        Assert.False(usuario.IsInRole("Entrenador"));
        Assert.Equal("admin@itm.edu.co", usuario.Identity.Name);
        Assert.Equal(id.ToString(), usuario.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    }

    [Fact]
    public async Task TokenOpacoSinPerfil_SeTrataComoAnonimo_NoComoAutenticadoSinRoles()
    {
        var almacen = new AlmacenEnMemoria();
        almacen.Datos[ProveedorEstadoAutenticacion.ClaveToken] = TokenOpaco;

        var usuario = (await new ProveedorEstadoAutenticacion(almacen).GetAuthenticationStateAsync()).User;

        Assert.False(usuario.Identity!.IsAuthenticated);   // fuerza a iniciar sesión de nuevo en vez de mostrar un menú sin sentido
    }

    [Fact]
    public async Task SinToken_EsAnonimo_AunqueQuedeUnPerfilViejo()
    {
        var almacen = new AlmacenEnMemoria();
        almacen.Datos[ProveedorEstadoAutenticacion.ClavePerfil] =
            ProveedorEstadoAutenticacion.SerializarPerfil(new PerfilSesion(Guid.NewGuid(), "x@itm.edu.co", new[] { "Administrador" }));

        var usuario = (await new ProveedorEstadoAutenticacion(almacen).GetAuthenticationStateAsync()).User;

        Assert.False(usuario.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task PerfilCorrupto_EsAnonimo_SinLanzarExcepcion()
    {
        var almacen = new AlmacenEnMemoria();
        almacen.Datos[ProveedorEstadoAutenticacion.ClaveToken] = TokenOpaco;
        almacen.Datos[ProveedorEstadoAutenticacion.ClavePerfil] = "{no es json";

        var usuario = (await new ProveedorEstadoAutenticacion(almacen).GetAuthenticationStateAsync()).User;

        Assert.False(usuario.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task TokenJwtSinPerfil_SigueLeyendoLosRolesDelToken()
    {
        var almacen = new AlmacenEnMemoria();
        almacen.Datos[ProveedorEstadoAutenticacion.ClaveToken] = Jwt("{\"sub\":\"u1\",\"email\":\"e@itm.edu.co\",\"role\":\"Entrenador\"}");

        var usuario = (await new ProveedorEstadoAutenticacion(almacen).GetAuthenticationStateAsync()).User;

        Assert.True(usuario.IsInRole("Entrenador"));
    }

    // ---------- AutenticacionService.IniciarSesionAsync (cliente Kiota real) ----------

    private static (AutenticacionService, AlmacenEnMemoria, ManejadorPorRuta, ProveedorEstadoAutenticacion) CrearServicio(
        Func<HttpRequestMessage, (HttpStatusCode, string?)> responder)
    {
        var manejador = new ManejadorPorRuta(responder);
        var http = new HttpClient(manejador) { BaseAddress = new Uri("http://api.test") };
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http) { BaseUrl = "http://api.test" };
        var almacen = new AlmacenEnMemoria();
        var proveedor = new ProveedorEstadoAutenticacion(almacen);
        return (new AutenticacionService(new FitnessApiClient(adapter), almacen, proveedor), almacen, manejador, proveedor);
    }

    [Fact]
    public async Task IniciarSesion_PideElPerfilAlBackend_LoGuarda_Y_ElUsuarioQuedaConSusRoles()
    {
        var id = Guid.NewGuid();
        var (servicio, almacen, manejador, proveedor) = CrearServicio(req => req.RequestUri!.AbsolutePath switch
        {
            "/api/auth/login" => (HttpStatusCode.OK, $"{{\"tokenType\":\"Bearer\",\"accessToken\":\"{TokenOpaco}\",\"expiresIn\":3600,\"refreshToken\":\"refresco\"}}"),
            "/api/usuarios/yo" => (HttpStatusCode.OK, $"{{\"id\":\"{id}\",\"email\":\"entrenador@itm.edu.co\",\"roles\":[\"Entrenador\",\"Usuario\"]}}"),
            _ => (HttpStatusCode.NotFound, null)
        });

        var r = await servicio.IniciarSesionAsync("entrenador@itm.edu.co", "Passw0rd!");

        Assert.True(r.EsExitoso, r.Mensaje);
        Assert.Equal(new[] { "POST /api/auth/login", "GET /api/usuarios/yo" }, manejador.Rutas);
        Assert.True(almacen.Datos.ContainsKey(ProveedorEstadoAutenticacion.ClavePerfil));

        var usuario = (await proveedor.GetAuthenticationStateAsync()).User;   // lo que verá la app al recargar la página
        Assert.True(usuario.IsInRole("Entrenador"));
        Assert.False(usuario.IsInRole("Administrador"));

        var sesion = await servicio.ObtenerUsuarioActualAsync();
        Assert.Equal(id, sesion!.Id);
        Assert.Contains("Entrenador", sesion.Roles);
    }

    [Fact]
    public async Task IniciarSesion_SiFallaElPerfil_FallaYNoDejaUnaSesionAMedias()
    {
        var (servicio, almacen, _, _) = CrearServicio(req => req.RequestUri!.AbsolutePath switch
        {
            "/api/auth/login" => (HttpStatusCode.OK, $"{{\"accessToken\":\"{TokenOpaco}\",\"refreshToken\":\"refresco\"}}"),
            _ => (HttpStatusCode.InternalServerError, null)
        });

        var r = await servicio.IniciarSesionAsync("a@itm.edu.co", "Passw0rd!");

        Assert.False(r.EsExitoso);
        Assert.Empty(almacen.Datos);   // ni token ni perfil: nada de "autenticado sin roles"
    }

    [Fact]
    public async Task CerrarSesion_BorraTokenRefrescoYPerfil()
    {
        var (servicio, almacen, _, _) = CrearServicio(req => (HttpStatusCode.NotFound, null));
        almacen.Datos[ProveedorEstadoAutenticacion.ClaveToken] = TokenOpaco;
        almacen.Datos[ProveedorEstadoAutenticacion.ClaveRefreshToken] = "r";
        almacen.Datos[ProveedorEstadoAutenticacion.ClavePerfil] = "{}";

        await servicio.CerrarSesionAsync();

        Assert.Empty(almacen.Datos);
    }
}
