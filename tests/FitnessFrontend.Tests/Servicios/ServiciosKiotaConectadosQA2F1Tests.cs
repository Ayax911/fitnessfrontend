using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Asistencia;
using FitnessFrontend.Servicios.Auditoria;
using FitnessFrontend.Servicios.Bloqueos;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reportes;
using FitnessFrontend.Servicios.Retroalimentacion;
using FitnessFrontend.Servicios.Sesiones;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class ServiciosKiotaConectadosQA2F1Tests
{


    // =========================================================================
    // 1. ReportesClienteService
    // =========================================================================
    [Fact]
    public async Task Reportes_ConsultarAsistencia_EnviaQueryCorrectayDeserializa()
    {
        var desde = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var hasta = new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);
        var json = @"[
            {
                ""sesionId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
                ""eventoNombre"": ""Spinning"",
                ""fechaHoraInicio"": ""2026-11-01T08:00:00Z"",
                ""fechaHoraFin"": ""2026-11-01T09:00:00Z"",
                ""cupo"": 20,
                ""inscritos"": 18,
                ""asistentes"": 16,
                ""inasistentes"": 2,
                ""porcentajeAsistencia"": 88.8
            }
        ]";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Contains("/api/reportes/asistencia-por-sesion", req.RequestUri!.AbsolutePath);
            Assert.Contains("desde=", req.RequestUri.Query);
            Assert.Contains("hasta=", req.RequestUri.Query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var servicio = new ReportesClienteService(client, new ZonaHorariaService());
        var res = await servicio.ConsultarAsistenciaPorSesionAsync(desde, hasta);

        Assert.True(res.EsExito);
        Assert.Single(res.Valor!);
        Assert.Equal("Spinning", res.Valor![0].EventoNombre);
        Assert.Equal(20, res.Valor![0].CupoMaximo);
    }

    [Fact]
    public async Task Reportes_Error400RangoExcesivo_ProduceEstadoDeCargaError()
    {
        var desde = DateTimeOffset.UtcNow.AddDays(-100);
        var hasta = DateTimeOffset.UtcNow;
        var errorJson = @"{
            ""codigoError"": ""RangoExcesivo"",
            ""mensaje"": ""El rango de fechas no puede superar 90 días.""
        }";

        var client = KiotaTestHelper.CrearCliente(HttpStatusCode.BadRequest, errorJson);
        var servicio = new ReportesClienteService(client, new ZonaHorariaService());
        var res = await servicio.ConsultarAsistenciaPorSesionAsync(desde, hasta);

        Assert.True(res.EsError);
        Assert.Contains("El rango de fechas no puede superar 90 días", res.MensajeError);
    }

    [Fact]
    public async Task Reportes_DescargarCsv_EnviaParametroFormatoCsv()
    {
        var desde = DateTimeOffset.UtcNow.AddDays(-7);
        var hasta = DateTimeOffset.UtcNow;
        var csvEsperado = "Actividad;Cupo\r\nSpinning;20";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Contains("formato=csv", req.RequestUri!.Query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(csvEsperado, Encoding.UTF8, "text/csv")
            };
        });

        var servicio = new ReportesClienteService(client, new ZonaHorariaService());
        var res = await servicio.DescargarCsvAsync("asistencia", desde, hasta);

        Assert.True(res.EsExito);
        Assert.Contains("Actividad;Cupo", res.Valor);
        Assert.Contains("Spinning;20", res.Valor);
    }

    // =========================================================================
    // 2. AuditoriaClienteService
    // =========================================================================
    [Fact]
    public async Task Auditoria_Consultar_EnviaPaginacionMaxima50_YDeserializa()
    {
        var json = @"{
            ""items"": [
                {
                    ""id"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
                    ""actorId"": ""11111111-1111-1111-1111-111111111111"",
                    ""actorEmail"": ""admin@itm.edu.co"",
                    ""actorRol"": ""Administrador"",
                    ""accion"": ""CrearBloqueo"",
                    ""entidadTipo"": ""BloqueoCalendario"",
                    ""entidadId"": ""22222222-2222-2222-2222-222222222222"",
                    ""detalle"": ""Bloqueo por mantenimiento"",
                    ""fechaHoraUtc"": ""2026-11-01T12:00:00Z""
                }
            ],
            ""totalRegistros"": 1,
            ""paginaActual"": 1,
            ""totalPaginas"": 1
        }";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Contains("/api/auditoria", req.RequestUri!.AbsolutePath);
            Assert.Contains("tamano=50", req.RequestUri.Query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var servicio = new AuditoriaClienteService(client);
        var res = await servicio.ConsultarAuditoriaAsync(tamano: 100); // debe topar a 50

        Assert.True(res.EsExito);
        Assert.Single(res.Valor!.Elementos);
        Assert.Equal("CrearBloqueo", res.Valor.Elementos[0].Accion);
    }

    [Fact]
    public async Task Auditoria_Error403_ProduceEstadoDeCargaError()
    {
        var errorJson = @"{
            ""codigo"": ""AccesoDenegado"",
            ""mensaje"": ""No tienes permisos para ver la auditoría.""
        }";

        var client = KiotaTestHelper.CrearCliente(HttpStatusCode.Forbidden, errorJson);
        var servicio = new AuditoriaClienteService(client);
        var res = await servicio.ConsultarAuditoriaAsync();

        Assert.True(res.EsError);
        Assert.Contains("No tienes permisos", res.MensajeError);
    }

    // =========================================================================
    // 3. BloqueosClienteService
    // =========================================================================
    [Fact]
    public async Task Bloqueos_CrearBloqueo_EnviaPostCorrecto_YDeserializaRespuesta()
    {
        var sesionAfectadaId = Guid.NewGuid();
        var jsonRespuesta = $@"{{
            ""bloqueo"": {{
                ""id"": ""{Guid.NewGuid()}"",
                ""motivo"": ""Mantenimiento Preventivo"",
                ""fechaHoraInicio"": ""2026-11-05T08:00:00Z"",
                ""fechaHoraFin"": ""2026-11-05T12:00:00Z"",
                ""creadoPorEmail"": ""admin@itm.edu.co""
            }},
            ""sesionesAfectadas"": [""{sesionAfectadaId}""]
        }}";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/api/calendario/bloqueos", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonRespuesta, Encoding.UTF8, "application/json")
            };
        });

        var servicio = new BloqueosClienteService(client);
        var res = await servicio.CrearBloqueoAsync(new FitnessFrontend.Servicios.Bloqueos.CrearBloqueoRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(4),
            "Mantenimiento Preventivo",
            "Mantenimiento"));

        Assert.True(res.EsExitoso);
    }

    [Fact]
    public async Task Bloqueos_EliminarBloqueo_EnviaDeleteCorrecto()
    {
        var bloqueoId = Guid.NewGuid();

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Contains(bloqueoId.ToString(), req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var servicio = new BloqueosClienteService(client);
        var res = await servicio.EliminarBloqueoAsync(bloqueoId);

        Assert.True(res.EsExitoso);
    }

    // =========================================================================
    // 4. AdministracionSesionesClienteService
    // =========================================================================
    [Fact]
    public async Task Sesiones_CancelarSesion_EnviaMotivoObligatorio()
    {
        var sesionId = Guid.NewGuid();

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"/api/eventos/sesiones/{sesionId}/cancelar", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var servicio = new AdministracionSesionesClienteService(client);
        var res = await servicio.CancelarSesionAsync(sesionId, "Falla en equipo");

        Assert.True(res.EsExitoso);
    }

    [Fact]
    public async Task Sesiones_CancelarSesion_SinMotivo_FallaLocalmente()
    {
        var servicio = new AdministracionSesionesClienteService(KiotaTestHelper.CrearCliente(HttpStatusCode.OK));
        var res = await servicio.CancelarSesionAsync(Guid.NewGuid(), "");

        Assert.False(res.EsExitoso);
        Assert.Contains("motivo", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sesiones_InscribirTercero_Conflict409_MuestraError()
    {
        var sesionId = Guid.NewGuid();
        var errorJson = @"{
            ""codigo"": ""SinCupoDisponible"",
            ""mensaje"": ""No hay cupos disponibles en esta sesión.""
        }";

        var client = KiotaTestHelper.CrearCliente(HttpStatusCode.Conflict, errorJson);
        var servicio = new AdministracionSesionesClienteService(client);
        var res = await servicio.InscribirTerceroAsync(sesionId, Guid.NewGuid(), "Inscripción especial");

        Assert.False(res.EsExitoso);
        Assert.Contains("No hay cupos disponibles", res.Mensaje);
    }

    // =========================================================================
    // 5. NotasYAnunciosClienteService
    // =========================================================================
    [Fact]
    public async Task NotasYAnuncios_ObtenerNotas_RN007_403ProduceMensajeClaro()
    {
        var sesionId = Guid.NewGuid();
        var errorJson = @"{
            ""codigo"": ""AccesoDenegado"",
            ""mensaje"": ""Solo el entrenador asignado o el administrador pueden ver las notas.""
        }";

        var client = KiotaTestHelper.CrearCliente(HttpStatusCode.Forbidden, errorJson);
        var servicio = new NotasYAnunciosClienteService(client);
        var res = await servicio.ObtenerNotasAsync(sesionId);

        Assert.True(res.EsError);
        Assert.Contains("Solo el entrenador asignado", res.MensajeError);
    }

    [Fact]
    public async Task NotasYAnuncios_PublicarAnuncio_EnviaPostYDeserializa()
    {
        var sesionId = Guid.NewGuid();
        var jsonRespuesta = $@"{{
            ""id"": ""{Guid.NewGuid()}"",
            ""sesionId"": ""{sesionId}"",
            ""mensaje"": ""Traer toalla personal"",
            ""fechaPublicacionUtc"": ""2026-11-01T10:00:00Z"",
            ""publicadoPorNombre"": ""Entrenador Juan""
        }}";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal($"/api/eventos/sesiones/{sesionId}/anuncios", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonRespuesta, Encoding.UTF8, "application/json")
            };
        });

        var servicio = new NotasYAnunciosClienteService(client);
        var res = await servicio.PublicarAnuncioAsync(sesionId, "Traer toalla personal");

        Assert.True(res.EsExitoso);
    }

    // =========================================================================
    // 6. AsistenciaManualClienteService
    // =========================================================================
    [Fact]
    public async Task AsistenciaManual_Registrar_EnviaPutCorrecto()
    {
        var sesionId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var jsonInscritos = $@"[
            {{
                ""reservaId"": ""{reservaId}"",
                ""usuarioId"": ""{usuarioId}"",
                ""nombreCompleto"": ""Carlos Pérez"",
                ""email"": ""carlos@itm.edu.co"",
                ""estado"": 0
            }}
        ]";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonInscritos, Encoding.UTF8, "application/json")
                };
            }

            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal($"/api/asistencia/sesiones/{sesionId}/manual", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var servicio = new AsistenciaManualClienteService(client);
        var res = await servicio.MarcarAsistenciaManualAsync(sesionId, reservaId, "Validación física");

        Assert.True(res.EsExitoso);
    }

    // =========================================================================
    // 7. RetroalimentacionClienteService
    // =========================================================================
    [Fact]
    public async Task Retroalimentacion_429LimiteExcedido_MuestraMensajeAmigable()
    {
        var errorJson = @"{
            ""codigoError"": ""LimiteExcedido"",
            ""mensaje"": ""Has alcanzado el límite de reportes.""
        }";

        var client = KiotaTestHelper.CrearCliente((HttpStatusCode)429, errorJson);
        var servicio = new RetroalimentacionClienteService(client);
        var res = await servicio.EnviarReporteAsync(new CrearReporteDto("Sugerencia", "Mejora", "/inicio"));

        Assert.True(res.EsError);
        Assert.Contains("Has alcanzado el límite de envíos por hora. Intenta más tarde", res.MensajeError);
    }

    [Fact]
    public async Task Retroalimentacion_EnviarReporte_EnviaPostYDeserializa()
    {
        var id = Guid.NewGuid();
        var jsonRespuesta = $@"{{
            ""id"": ""{id}"",
            ""usuarioId"": ""{Guid.NewGuid()}"",
            ""tipo"": 1,
            ""mensaje"": ""Excelente servicio"",
            ""ruta"": ""/gimnasio"",
            ""estado"": 0,
            ""fechaCreacionUtc"": ""2026-11-01T15:00:00Z""
        }}";

        var client = KiotaTestHelper.CrearCliente(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/api/retroalimentacion", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonRespuesta, Encoding.UTF8, "application/json")
            };
        });

        var servicio = new RetroalimentacionClienteService(client);
        var res = await servicio.EnviarReporteAsync(new CrearReporteDto("Sugerencia", "Excelente servicio", "/gimnasio"));

        Assert.True(res.EsExito);
        Assert.Equal("Excelente servicio", res.Valor!.Mensaje);
    }
}
