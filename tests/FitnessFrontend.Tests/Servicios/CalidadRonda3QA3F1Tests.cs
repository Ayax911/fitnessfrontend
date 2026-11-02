using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Bloqueos;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Sesiones;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class CalidadRonda3QA3F1Tests
{
    // =========================================================================
    // H-28: Tipos de bloqueo de calendario (Festivo=0, Mantenimiento=1, Cierre=2)
    // =========================================================================
    [Theory]
    [InlineData(0, "Festivo")]
    [InlineData(1, "Mantenimiento")]
    [InlineData(2, "Cierre")]
    public async Task Bloqueos_Lectura_DeserializaTipoCorrectoSegunBackend(int tipoBackend, string tipoEsperado)
    {
        var bloqueoId = Guid.NewGuid();
        var json = $@"[
            {{
                ""id"": ""{bloqueoId}"",
                ""desde"": ""2026-11-10T08:00:00Z"",
                ""hasta"": ""2026-11-10T12:00:00Z"",
                ""motivo"": ""Prueba de bloqueo"",
                ""tipo"": {tipoBackend}
            }}
        ]";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Contains("/api/calendario/bloqueos", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var servicio = new BloqueosClienteService(client);
        var res = await servicio.ObtenerBloqueosAsync();

        Assert.True(res.EsExito);
        Assert.NotNull(res.Valor);
        var bloqueo = Assert.Single(res.Valor);
        Assert.Equal(tipoEsperado, bloqueo.Tipo);
    }

    [Theory]
    [InlineData("Festivo", 0)]
    [InlineData("Mantenimiento", 1)]
    [InlineData("Cierre", 2)]
    public async Task Bloqueos_Creacion_SerializaTipoCorrectoHaciaBackend(string tipoFrontend, int tipoBackendEsperado)
    {
        int? tipoCapturado = null;

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/api/calendario/bloqueos", req.RequestUri!.AbsolutePath);

            var jsonBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(jsonBody);
            if (doc.RootElement.TryGetProperty("tipo", out var tipoProp))
            {
                tipoCapturado = tipoProp.GetInt32();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        var servicio = new BloqueosClienteService(client);
        var res = await servicio.CrearBloqueoAsync(new FitnessFrontend.Servicios.Bloqueos.CrearBloqueoRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            "Motivo de prueba",
            tipoFrontend));

        Assert.True(res.EsExitoso);
        Assert.Equal(tipoBackendEsperado, tipoCapturado);
    }

    // =========================================================================
    // H-30: "Mis reservas" pide anuncios únicamente para reservas Confirmada (Estado == 0)
    // =========================================================================
    [Fact]
    public async Task NotasYAnuncios_ObtenerAnunciosMisReservas_SoloPideParaReservasConfirmadas()
    {
        var sesionConfirmadaId = Guid.NewGuid();
        var sesionEsperaId = Guid.NewGuid();
        var sesionCanceladaId = Guid.NewGuid();

        var rutasInvocadas = new List<string>();

        var jsonReservas = $@"
        [
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""sesionEventoId"": ""{sesionConfirmadaId}"",
                ""eventoNombre"": ""Yoga Matutino"",
                ""estado"": 0,
                ""sesionCancelada"": false
            }},
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""sesionEventoId"": ""{sesionEsperaId}"",
                ""eventoNombre"": ""Crossfit"",
                ""estado"": 1,
                ""sesionCancelada"": false
            }},
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""sesionEventoId"": ""{sesionCanceladaId}"",
                ""eventoNombre"": ""Natación"",
                ""estado"": 2,
                ""sesionCancelada"": false
            }}
        ]";

        var jsonAnuncios = $@"[
            {{
                ""id"": ""{Guid.NewGuid()}"",
                ""sesionId"": ""{sesionConfirmadaId}"",
                ""mensaje"": ""Llevar toalla y termo"",
                ""fechaPublicacionUtc"": ""2026-11-01T10:00:00Z"",
                ""publicadoPorNombre"": ""Entrenador""
            }}
        ]";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            var ruta = req.RequestUri!.AbsolutePath;
            rutasInvocadas.Add(ruta);

            if (ruta.EndsWith("/api/reservas/mias", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonReservas, Encoding.UTF8, "application/json")
                };
            }

            if (ruta.Contains($"/api/eventos/sesiones/{sesionConfirmadaId}/anuncios", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonAnuncios, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var servicio = new NotasYAnunciosClienteService(client);
        var res = await servicio.ObtenerAnunciosMisReservasAsync();

        Assert.True(res.EsExito);
        Assert.NotNull(res.Valor);
        var anuncio = Assert.Single(res.Valor);
        Assert.Equal("Llevar toalla y termo", anuncio.Mensaje);

        // Verificamos que NUNCA se invocaron anuncios para la sesión en lista de espera ni cancelada
        Assert.Contains($"/api/eventos/sesiones/{sesionConfirmadaId}/anuncios", rutasInvocadas);
        Assert.DoesNotContain($"/api/eventos/sesiones/{sesionEsperaId}/anuncios", rutasInvocadas);
        Assert.DoesNotContain($"/api/eventos/sesiones/{sesionCanceladaId}/anuncios", rutasInvocadas);
    }

    // =========================================================================
    // H-27: Administrar sesión resiliente ante 403 en endpoint secundario de inscritos
    // =========================================================================
    [Fact]
    public async Task AdministrarSesion_Inscritos403_NoFallaYCargaMetadatosPrincipales()
    {
        var sesionId = Guid.NewGuid();
        var eventoId = Guid.NewGuid();

        var jsonEventos = $@"
        [
            {{
                ""id"": ""{eventoId}"",
                ""nombre"": ""Entrenamiento Funcional"",
                ""entrenadorNombre"": ""Pedro Gómez"",
                ""sesiones"": [
                    {{
                        ""id"": ""{sesionId}"",
                        ""fechaHoraInicio"": ""2026-11-05T14:00:00Z"",
                        ""fechaHoraFin"": ""2026-11-05T15:00:00Z"",
                        ""cupoMaximo"": 25,
                        ""cuposDisponibles"": 10
                    }}
                ]
            }}
        ]";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            var ruta = req.RequestUri!.AbsolutePath;

            // Simula un fallo (403) del endpoint administrativo de inscritos (D-E): la pantalla debe seguir operativa
            if (ruta.Contains($"/api/admin/sesiones/{sesionId}/inscritos", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent(@"{""codigo"":""AccesoDenegado"",""mensaje"":""Solo el entrenador puede consultar inscritos""}", Encoding.UTF8, "application/json")
                };
            }

            if (ruta.EndsWith("/api/eventos", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonEventos, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var servicio = new AdministracionSesionesClienteService(client);
        var res = await servicio.ObtenerDetalleSesionAdminAsync(sesionId);

        // La carga general debe ser exitosa, no fallar con 403
        Assert.True(res.EsExito);
        Assert.NotNull(res.Valor);
        Assert.Equal("Entrenamiento Funcional", res.Valor.EventoNombre);
        Assert.Equal("Pedro Gómez", res.Valor.EntrenadorNombre);
        Assert.Equal(25, res.Valor.CupoMaximo);
        Assert.Equal(10, res.Valor.CuposDisponibles);
        // La lista de reservas queda vacía en vez de arrojar excepción
        Assert.Empty(res.Valor.Reservas);
    }

    // H-27 (D-E), RN-020: el Administrador consulta SU endpoint y se muestran confirmados y lista de espera.
    [Fact]
    public async Task AdministrarSesion_UsaEndpointAdministrativo_YMapeaConfirmadosYListaEspera()
    {
        var sesionId = Guid.NewGuid();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        var rutas = new List<string>();
        var jsonInscritos = $@"{{
            ""sesionEventoId"": ""{sesionId}"",
            ""confirmados"": [{{ ""reservaId"": ""{Guid.NewGuid()}"", ""usuarioId"": ""{u1}"", ""nombreCompleto"": ""Ana Pérez"", ""estado"": 0, ""origen"": 0 }}],
            ""listaEspera"": [{{ ""reservaId"": ""{Guid.NewGuid()}"", ""usuarioId"": ""{u2}"", ""nombreCompleto"": ""Luis Gómez"", ""estado"": 1, ""origen"": 1 }}]
        }}";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            var ruta = req.RequestUri!.AbsolutePath;
            rutas.Add(ruta);
            if (ruta.Equals($"/api/admin/sesiones/{sesionId}/inscritos", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonInscritos, Encoding.UTF8, "application/json") };
            if (ruta.EndsWith("/api/eventos", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var res = await new AdministracionSesionesClienteService(client).ObtenerDetalleSesionAdminAsync(sesionId);

        Assert.True(res.EsExito);
        Assert.DoesNotContain(rutas, r => r.Contains("/api/reservas/sesiones/", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, res.Valor!.Reservas.Count);
        var confirmada = Assert.Single(res.Valor.Reservas, r => r.UsuarioId == u1);
        var espera = Assert.Single(res.Valor.Reservas, r => r.UsuarioId == u2);
        Assert.Equal(0, confirmada.Estado);
        Assert.Equal(1, espera.Estado);
        Assert.Equal("Ana Pérez", confirmada.NombreUsuario);
        Assert.Equal("Administrador", espera.Origen);
    }
}
