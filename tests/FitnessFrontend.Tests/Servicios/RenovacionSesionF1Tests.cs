using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Asistencia;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// RF-026 / F1: Pruebas obligatorias de renovación de sesión de un solo vuelo (Patrón 8)
public class RenovacionSesionF1Tests
{
    private sealed class AlmacenEnMemoria : ILocalStorageService
    {
        public Dictionary<string, string> Datos { get; } = new();
        public ValueTask GuardarItemAsync(string clave, string valor) { Datos[clave] = valor; return ValueTask.CompletedTask; }
        public ValueTask<string?> ObtenerItemAsync(string clave) => ValueTask.FromResult(Datos.TryGetValue(clave, out var v) ? v : null);
        public ValueTask EliminarItemAsync(string clave) { Datos.Remove(clave); return ValueTask.CompletedTask; }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public string? UltimaNavegacion { get; private set; }

        public TestNavigationManager(string baseUri = "http://localhost:5081/", string uriActual = "http://localhost:5081/")
        {
            Initialize(baseUri, uriActual);
        }

        public void CambiarRutaActual(string uri)
        {
            Uri = uri;
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            UltimaNavegacion = uri;
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }

    private sealed class MockHttpTransportHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> HandlerFunc { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = HandlerFunc(request);
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task Prueba1_CincoPeticionesSimultaneasConTokenVencido_ProvocanExactamenteUnRefresh()
    {
        // 1. Cinco peticiones simultáneas con token vencido provocan exactamente 1 llamada a refresh y las 5 terminan bien
        var almacen = new AlmacenEnMemoria();
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveToken, "token-vencido-inicial");
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveRefreshToken, "refresh-valido");

        var nav = new TestNavigationManager();
        var authProvider = new ProveedorEstadoAutenticacion(almacen);

        int llamadasRefresh = 0;
        int peticionesAtendidasConTokenRenovado = 0;

        var transport = new MockHttpTransportHandler
        {
            HandlerFunc = req =>
            {
                var path = req.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref llamadasRefresh);
                    // Simular demora ligera para verificar que concurrentemente se sincronizan
                    Thread.Sleep(30);

                    var respBody = JsonSerializer.Serialize(new
                    {
                        tokenType = "Bearer",
                        accessToken = "token-renovado-nuevo",
                        refreshToken = "refresh-renovado-nuevo",
                        expiresIn = 3600
                    });

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(respBody, Encoding.UTF8, "application/json")
                    };
                }

                // Endpoint protegido
                var authHeader = req.Headers.Authorization?.Parameter;
                if (authHeader == "token-vencido-inicial")
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                if (authHeader == "token-renovado-nuevo")
                {
                    Interlocked.Increment(ref peticionesAtendidasConTokenRenovado);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"exito\":true}", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        };

        var manejadorAuth = new ManejadorDeAutenticacionHttp(almacen, nav, authProvider)
        {
            InnerHandler = transport
        };

        var client = new HttpClient(manejadorAuth)
        {
            BaseAddress = new Uri("http://localhost:5224")
        };

        // Disparamos 5 peticiones simultáneas
        var tareas = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tareas.Add(Task.Run(() => client.GetAsync("/api/eventos")));
        }

        var respuestas = await Task.WhenAll(tareas);

        // Aserciones obligatorias:
        // Exactamente 1 llamada a refresh
        Assert.Equal(1, llamadasRefresh);

        // Las 5 terminan bien con 200 OK
        Assert.Equal(5, respuestas.Length);
        foreach (var r in respuestas)
        {
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        // Las 5 peticiones usaron el token renovado
        Assert.Equal(5, peticionesAtendidasConTokenRenovado);

        // El nuevo token quedó almacenado
        var tokenGuardado = await almacen.ObtenerItemAsync(ProveedorEstadoAutenticacion.ClaveToken);
        Assert.Equal("token-renovado-nuevo", tokenGuardado);
    }

    [Fact]
    public async Task Prueba2_Un401TrasRefresh_NoEntraEnBucle_MaximoUnReintentoYCierraSesion()
    {
        // 2. Un 401 tras el refresh no entra en bucle (máximo un reintento)
        var almacen = new AlmacenEnMemoria();
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveToken, "token-vencido");
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveRefreshToken, "refresh-valido");

        var nav = new TestNavigationManager("http://localhost:5081/", "http://localhost:5081/mis-reservas");
        var authProvider = new ProveedorEstadoAutenticacion(almacen);

        int llamadasRefresh = 0;
        int llamadasEndpoint = 0;

        var transport = new MockHttpTransportHandler
        {
            HandlerFunc = req =>
            {
                var path = req.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref llamadasRefresh);
                    var respBody = JsonSerializer.Serialize(new
                    {
                        tokenType = "Bearer",
                        accessToken = "token-renovado-pero-invalido",
                        refreshToken = "refresh-renovado",
                        expiresIn = 3600
                    });

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(respBody, Encoding.UTF8, "application/json")
                    };
                }

                // El endpoint protegido siempre devuelve 401 aun con el token renovado
                Interlocked.Increment(ref llamadasEndpoint);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        };

        var manejadorAuth = new ManejadorDeAutenticacionHttp(almacen, nav, authProvider)
        {
            InnerHandler = transport
        };

        var client = new HttpClient(manejadorAuth)
        {
            BaseAddress = new Uri("http://localhost:5224")
        };

        var respuesta = await client.GetAsync("/api/reservas/mis-reservas");

        // Debe devolver 401
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        // Se llamó a refresh EXACTAMENTE 1 vez (no en bucle)
        Assert.Equal(1, llamadasRefresh);

        // El endpoint se llamó la primera vez y reintentó una sola vez: total 2
        Assert.Equal(2, llamadasEndpoint);

        // La sesión se cerró y se redirigió a login
        Assert.Null(await almacen.ObtenerItemAsync(ProveedorEstadoAutenticacion.ClaveToken));
        Assert.Contains("/login?returnUrl=mis-reservas", nav.UltimaNavegacion);
    }

    [Fact]
    public async Task Prueba3_RefreshInvalido_CierraSesionYRedirigeConReturnUrlCorrecto()
    {
        // 3. Refresh inválido -> sesión cerrada y returnUrl correcto
        var almacen = new AlmacenEnMemoria();
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveToken, "token-vencido");
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveRefreshToken, "refresh-ya-expirado");
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClavePerfil, "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"email\":\"u@itm.edu.co\",\"roles\":[]}");

        var nav = new TestNavigationManager("http://localhost:5081/", "http://localhost:5081/eventos");
        var authProvider = new ProveedorEstadoAutenticacion(almacen);

        var transport = new MockHttpTransportHandler
        {
            HandlerFunc = req =>
            {
                var path = req.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
                {
                    // Refresh token inválido en el servidor
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        Content = new StringContent("{\"codigoError\":\"TokenInvalido\"}", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        };

        var manejadorAuth = new ManejadorDeAutenticacionHttp(almacen, nav, authProvider)
        {
            InnerHandler = transport
        };

        var client = new HttpClient(manejadorAuth)
        {
            BaseAddress = new Uri("http://localhost:5224")
        };

        var respuesta = await client.GetAsync("/api/eventos");

        // Resultado 401
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        // Sesión completamente limpia en storage
        Assert.Null(await almacen.ObtenerItemAsync(ProveedorEstadoAutenticacion.ClaveToken));
        Assert.Null(await almacen.ObtenerItemAsync(ProveedorEstadoAutenticacion.ClaveRefreshToken));
        Assert.Null(await almacen.ObtenerItemAsync(ProveedorEstadoAutenticacion.ClavePerfil));

        // Redirigido a login con returnUrl de la página que estaba navegando
        Assert.NotNull(nav.UltimaNavegacion);
        Assert.Equal("/login?returnUrl=eventos", nav.UltimaNavegacion);
    }

    [Fact]
    public async Task Prueba4_PeticionesAuth_NoDisparanRefreshRecursivo()
    {
        // 4. Las llamadas a /api/auth/refresh y /api/auth/login no disparan refresh recursivo
        var almacen = new AlmacenEnMemoria();
        var nav = new TestNavigationManager();
        var authProvider = new ProveedorEstadoAutenticacion(almacen);

        int llamadasLogin = 0;
        int llamadasRefresh = 0;

        var transport = new MockHttpTransportHandler
        {
            HandlerFunc = req =>
            {
                var path = req.RequestUri?.AbsolutePath ?? string.Empty;

                if (path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref llamadasLogin);
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                if (path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref llamadasRefresh);
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        };

        var manejadorAuth = new ManejadorDeAutenticacionHttp(almacen, nav, authProvider)
        {
            InnerHandler = transport
        };

        var client = new HttpClient(manejadorAuth)
        {
            BaseAddress = new Uri("http://localhost:5224")
        };

        // Petición a login que da 401 (ej. credenciales inválidas)
        var respLogin = await client.PostAsync("/api/auth/login", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, respLogin.StatusCode);
        Assert.Equal(1, llamadasLogin);
        Assert.Equal(0, llamadasRefresh); // NO llamó a refresh

        // Petición directa a refresh que da 401
        var respRefresh = await client.PostAsync("/api/auth/refresh", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, respRefresh.StatusCode);
        Assert.Equal(1, llamadasRefresh); // NO llamó recursivamente a refresh
    }

    [Fact]
    public async Task Prueba5_CheckInConTokenVencido_SeCompletaTrasRenovarSinPasarPorLogin()
    {
        // 5. El check-in (/checkin/{token}) con token vencido se completa tras renovar, sin pasar por la pantalla de login
        var almacen = new AlmacenEnMemoria();
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveToken, "token-vencido-estudiante");
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClaveRefreshToken, "refresh-estudiante-valido");
        var perfil = new PerfilSesion(Guid.NewGuid(), "estudiante@itm.edu.co", new[] { "Usuario" });
        await almacen.GuardarItemAsync(ProveedorEstadoAutenticacion.ClavePerfil, ProveedorEstadoAutenticacion.SerializarPerfil(perfil));

        var nav = new TestNavigationManager("http://localhost:5081/", "http://localhost:5081/checkin/qr-sesion-999");
        var authProvider = new ProveedorEstadoAutenticacion(almacen);

        int refreshesEjecutados = 0;
        int checkInIntentos = 0;

        var peticionesRecibidas = new List<string>();

        var transport = new MockHttpTransportHandler
        {
            HandlerFunc = req =>
            {
                var path = req.RequestUri?.AbsolutePath ?? string.Empty;
                var auth = req.Headers.Authorization?.Parameter ?? "sin-auth";
                peticionesRecibidas.Add($"{req.Method} {path} auth={auth}");

                if (path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref refreshesEjecutados);
                    var respBody = JsonSerializer.Serialize(new
                    {
                        tokenType = "Bearer",
                        accessToken = "token-estudiante-renovado",
                        refreshToken = "refresh-estudiante-nuevo",
                        expiresIn = 3600
                    });

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(respBody, Encoding.UTF8, "application/json")
                    };
                }

                if (path.Contains("/api/asistencia/checkin/", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref checkInIntentos);
                    var bearer = req.Headers.Authorization?.Parameter;

                    if (bearer == "token-vencido-estudiante")
                    {
                        return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    }

                    if (bearer == "token-estudiante-renovado")
                    {
                        var checkInJson = JsonSerializer.Serialize(new
                        {
                            exitoso = true,
                            mensaje = "¡Asistencia confirmada para Voleibol!",
                            detalle = "Cancha 1"
                        });

                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(checkInJson, Encoding.UTF8, "application/json")
                        };
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        };

        var manejadorAuth = new ManejadorDeAutenticacionHttp(almacen, nav, authProvider)
        {
            InnerHandler = transport
        };

        var httpClient = new HttpClient(manejadorAuth)
        {
            BaseAddress = new Uri("http://localhost:5224")
        };

        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)
        {
            BaseUrl = "http://localhost:5224"
        };

        var apiClient = new FitnessApiClient(adapter);
        var servicioAsistencia = new AsistenciaClienteService(apiClient);

        // El servicio de asistencia ejecuta el check-in
        var resultado = await servicioAsistencia.RegistrarCheckInAsync("qr-sesion-999");

        // Verificaciones DoD:
        // 1. El check-in tuvo éxito
        Assert.True(resultado.EsExito, resultado.MensajeError);
        Assert.NotNull(resultado.Valor);
        Assert.True(resultado.Valor.Exitoso);
        Assert.Equal("¡Asistencia confirmada para Voleibol!", resultado.Valor.Mensaje);

        // 2. Hubo renovación en silencio
        Assert.Equal(1, refreshesEjecutados);
        Assert.Equal(2, checkInIntentos); // 1er intento con 401 + reintento con token renovado exitoso

        // 3. No se navegó a login
        Assert.Null(nav.UltimaNavegacion);
    }
}
