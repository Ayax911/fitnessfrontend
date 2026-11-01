using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Eventos;
using FitnessFrontend.Servicios.Bloqueos;
using FitnessFrontend.Servicios.Calendario;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reservas;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// RF-035, RF-036, RF-037, RF-038 / F2: Pruebas obligatorias de programación, .ics y bloqueos
public class ProgramacionYCalendarioF2Tests : BunitContext
{
    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;

        public FakeAuthStateProvider(string email = "estudiante@itm.edu.co", string rol = "Usuario")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, rol)
            }, "TestAuth");
            _user = new ClaimsPrincipal(identity);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_user));
    }

    private class EventosServiceFalso : IEventosClienteService
    {
        public List<EventoResumenDto> Eventos { get; set; } = new();

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(Eventos));

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(
            DateTimeOffset? desde, DateTimeOffset? hasta, int? pagina = null, int? tamano = null, CancellationToken ct = default)
        {
            var res = Eventos.AsEnumerable();
            if (desde.HasValue) res = res.Where(e => (e.Sesiones ?? new List<SesionResumenDto>()).Any(s => (s.FechaHoraFin ?? s.FechaHoraInicio ?? DateTimeOffset.MinValue) >= desde.Value));
            if (hasta.HasValue) res = res.Where(e => (e.Sesiones ?? new List<SesionResumenDto>()).Any(s => (s.FechaHoraInicio ?? DateTimeOffset.MaxValue) <= hasta.Value));
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(res.ToList()));
        }

        public Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Correcto(new List<ActividadAsignadaDto>()));

        public Task<EstadoDeCarga<Guid>> CrearEventoAsync(CrearEventoRequest request, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<Guid>.Correcto(Guid.NewGuid()));

        public Task<ResultadoOperacion> ActualizarEventoAsync(Guid id, ActualizarEventoRequest request, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<ResultadoOperacion> DesactivarOEliminarEventoAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<IReadOnlyList<OpcionPatronRecurrencia>> ObtenerPatronesRecurrenciaSoportadosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OpcionPatronRecurrencia>>(new List<OpcionPatronRecurrencia>());
    }

    private class BloqueosServiceFalso : IBloqueosClienteService
    {
        public List<BloqueoCalendarioDto> Bloqueos { get; set; } = new();

        public Task<EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>> ObtenerBloqueosAsync(
            DateTimeOffset? desde = null, DateTimeOffset? hasta = null, CancellationToken ct = default)
        {
            var res = Bloqueos.AsEnumerable();
            if (desde.HasValue) res = res.Where(b => b.Hasta >= desde.Value);
            if (hasta.HasValue) res = res.Where(b => b.Desde <= hasta.Value);
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>.Correcto(res.ToList()));
        }

        public Task<ResultadoOperacion> CrearBloqueoAsync(CrearBloqueoRequest request, CancellationToken ct = default)
        {
            Bloqueos.Add(new BloqueoCalendarioDto(Guid.NewGuid(), request.Desde, request.Hasta, request.Motivo, request.Tipo));
            return Task.FromResult(ResultadoOperacion.Exito());
        }

        public Task<ResultadoOperacion> EliminarBloqueoAsync(Guid bloqueoId, CancellationToken ct = default)
        {
            Bloqueos.RemoveAll(b => b.Id == bloqueoId);
            return Task.FromResult(ResultadoOperacion.Exito());
        }
    }

    private class ReservasServiceFalso : IReservasClienteService
    {
        public Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(new List<ReservaResumenDto>()));

        public Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<ReservaResumenDto>.Correcto(new ReservaResumenDto { Id = Guid.NewGuid(), Estado = 0 }));

        public Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Correcto(new List<ReservaInscritoDto>()));
    }

    [Fact]
    public void Prueba1_GeneradorIcs_ValidoConEscapePlegadoYDespliegueCorrecto()
    {
        // 1. El .ics generado es válido (casos de escape, plegado <= 75 octetos y des-plegado de vuelta)
        var id = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var inicio = new DateTimeOffset(2026, 11, 10, 14, 0, 0, TimeSpan.Zero);
        var fin = new DateTimeOffset(2026, 11, 10, 15, 30, 0, TimeSpan.Zero);
        var stamp = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

        var tituloConCaracteresEspeciales = "Spinning & Cardio: Nivel 1; con pesas, colchonetas y barras \\ pesadas";
        var descripcionMuyLarga = "Esta es una descripción deliberadamente extensa diseñada para superar los setenta y cinco octetos por línea especificados por el RFC 5545, de modo que se obligue al GeneradorIcs a plegar la línea insertando un salto de línea seguido de un espacio en blanco.";
        var ubicacion = "Campus Robledo, Gimnasio Principal; Sala de Acondicionamiento 2";

        var datos = new GeneradorIcs.DatosEventoIcs(
            id,
            tituloConCaracteresEspeciales,
            inicio,
            fin,
            descripcionMuyLarga,
            ubicacion);

        var ics = GeneradorIcs.Generar(datos, stamp);

        // Aserciones estructurales del RFC 5545
        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.Contains("VERSION:2.0\r\n", ics);
        Assert.Contains("BEGIN:VEVENT\r\n", ics);
        Assert.Contains("END:VEVENT\r\n", ics);
        Assert.Contains($"UID:{id}@gimnasio.itm\r\n", ics);
        Assert.Contains("DTSTART:20261110T140000Z\r\n", ics);
        Assert.Contains("DTEND:20261110T153000Z\r\n", ics);
        Assert.Contains("STATUS:CONFIRMED\r\n", ics);

        // Ninguna línea física debe superar los 75 octetos
        var lineasFisicas = ics.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var linea in lineasFisicas)
        {
            var octetos = Encoding.UTF8.GetByteCount(linea);
            Assert.True(octetos <= 75, $"Línea supera 75 octetos ({octetos}): '{linea}'");
        }

        // Parseo de vuelta: desplegamos líneas que comienzan con espacio (RFC 5545 §3.1)
        var sbDesplegado = new StringBuilder();
        foreach (var linea in lineasFisicas)
        {
            if (linea.StartsWith(" "))
            {
                sbDesplegado.Append(linea.Substring(1));
            }
            else
            {
                if (sbDesplegado.Length > 0) sbDesplegado.Append("\n");
                sbDesplegado.Append(linea);
            }
        }

        var lineasLogicas = sbDesplegado.ToString().Split('\n');
        var propiedades = new Dictionary<string, string>();
        foreach (var l in lineasLogicas)
        {
            var idx = l.IndexOf(':');
            if (idx > 0)
            {
                propiedades[l.Substring(0, idx)] = l.Substring(idx + 1);
            }
        }

        // Verificamos que los campos escapados y plegados se conservan
        Assert.Equal($"{id}@gimnasio.itm", propiedades["UID"]);
        Assert.Equal("20261110T140000Z", propiedades["DTSTART"]);
        Assert.Equal("20261110T153000Z", propiedades["DTEND"]);

        // Escape verificado
        Assert.Contains(@"\,", propiedades["SUMMARY"]);
        Assert.Contains(@"\;", propiedades["SUMMARY"]);
        Assert.Contains(@"\\", propiedades["SUMMARY"]);
    }

    [Fact]
    public void Prueba2_VistaSemanal_ConvierteAAmericaBogota_SesionNocturnaCaeEnDiaCorrecto()
    {
        // 2. La vista semanal convierte a America/Bogota (sesión a las 23:30 locales cae en el día correcto aunque su UTC sea el siguiente)
        // Ejemplo: Miércoles 4 de Noviembre de 2026 a las 23:30 en Bogotá (UTC-5)
        // En UTC corresponde a: Jueves 5 de Noviembre de 2026 a las 04:30 UTC
        var offsetBogota = TimeSpan.FromHours(-5);
        var inicioLocal = new DateTimeOffset(2026, 11, 4, 23, 30, 0, offsetBogota);
        var finLocal = inicioLocal.AddHours(1); // Jueves 00:30 local

        var inicioUtc = inicioLocal.ToUniversalTime(); // 2026-11-05 04:30 UTC
        var finUtc = finLocal.ToUniversalTime();

        var eventosService = new EventosServiceFalso();
        eventosService.Eventos.Add(new EventoResumenDto
        {
            Id = Guid.NewGuid(),
            Nombre = "Acondicionamiento Nocturno",
            EntrenadorNombre = "Carlos Entrenador",
            Sesiones = new List<SesionResumenDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FechaHoraInicio = inicioUtc,
                    FechaHoraFin = finUtc,
                    CupoMaximo = 20,
                    CuposDisponibles = 15
                }
            }
        });

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IEventosClienteService>(eventosService);
        Services.AddSingleton<IBloqueosClienteService>(new BloqueosServiceFalso());
        Services.AddSingleton<IReservasClienteService>(new ReservasServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<Programacion>(p => p.Add(x => x.FechaReferencia, new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc)));

        // Verificamos que la sesión aparece bajo el día Miércoles 04/11 y muestra la hora 11:30 p.m.
        var contenido = cut.Markup;
        Assert.Contains("Acondicionamiento Nocturno", contenido);
        Assert.Contains("11:30", contenido);
        Assert.Contains("04/11", contenido);
    }

    [Fact]
    public void Prueba3_ConBloqueosDeCalendario_SeVenFranjasYCerrado_SinBotonesDeReserva()
    {
        // 3. Con bloqueos se ven franjas "Cerrado · motivo" y no botones de reserva
        var offsetBogota = TimeSpan.FromHours(-5);
        var desdeBloqueo = new DateTimeOffset(2026, 11, 2, 8, 0, 0, offsetBogota);
        var hastaBloqueo = new DateTimeOffset(2026, 11, 2, 18, 0, 0, offsetBogota);

        var bloqueosService = new BloqueosServiceFalso();
        bloqueosService.Bloqueos.Add(new BloqueoCalendarioDto(
            Guid.NewGuid(),
            desdeBloqueo.ToUniversalTime(),
            hastaBloqueo.ToUniversalTime(),
            "Mantenimiento General de Calderas",
            "Mantenimiento"));

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IEventosClienteService>(new EventosServiceFalso());
        Services.AddSingleton<IBloqueosClienteService>(bloqueosService);
        Services.AddSingleton<IReservasClienteService>(new ReservasServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<Programacion>(p => p.Add(x => x.FechaReferencia, new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc)));

        // Verificamos presencia de la franja con motivo
        Assert.Contains("Cerrado · Mantenimiento General de Calderas", cut.Markup);

        // Verificamos que no hay botones de reserva dentro de ese día bloqueado sin sesiones
        var botonesReserva = cut.FindAll("button").Where(b => b.TextContent.Contains("Reservar")).ToList();
        Assert.Empty(botonesReserva);
    }

    [Fact]
    public void Prueba4_NingunaPantallaMuestraPosicionEnListaDeEspera_RN020_AMBD04()
    {
        // 4. Ninguna pantalla muestra posición en lista de espera (solo número agregado o estado general)
        var offsetBogota = TimeSpan.FromHours(-5);
        var inicio = new DateTimeOffset(2026, 11, 3, 10, 0, 0, offsetBogota).ToUniversalTime();

        var eventosService = new EventosServiceFalso();
        eventosService.Eventos.Add(new EventoResumenDto
        {
            Id = Guid.NewGuid(),
            Nombre = "Crossfit Intenso",
            EntrenadorNombre = "Pedro Entrenador",
            Sesiones = new List<SesionResumenDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FechaHoraInicio = inicio,
                    FechaHoraFin = inicio.AddHours(1),
                    CupoMaximo = 10,
                    CuposDisponibles = 0 // Sesión llena -> lista de espera
                }
            }
        });

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IEventosClienteService>(eventosService);
        Services.AddSingleton<IBloqueosClienteService>(new BloqueosServiceFalso());
        Services.AddSingleton<IReservasClienteService>(new ReservasServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cutProgramacion = Render<Programacion>(p => p.Add(x => x.FechaReferencia, new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc)));
        var markupProg = cutProgramacion.Markup;

        // Verifica estado agregado
        Assert.Contains("Agotado (Espera)", markupProg);

        // Aserciones negativas estrictas (RN-020, AMB-D04): ninguna posición personal
        Assert.DoesNotMatch(new Regex(@"puesto\s+\d+", RegexOptions.IgnoreCase), markupProg);
        Assert.DoesNotMatch(new Regex(@"posición\s+\d+", RegexOptions.IgnoreCase), markupProg);
        Assert.DoesNotContain("tu posición", markupProg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lugar en fila", markupProg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("indicador", markupProg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prioridad", markupProg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Prueba5_EsqueletoVistaSemanal_SeAnunciaAccesiblemente()
    {
        // 5. El esqueleto de la vista semanal se anuncia con role="status" y texto accesible
        var tcs = new TaskCompletionSource<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>>();
        var eventosServiceLento = new EventosServiceFalsoLento(tcs.Task);

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IEventosClienteService>(eventosServiceLento);
        Services.AddSingleton<IBloqueosClienteService>(new BloqueosServiceFalso());
        Services.AddSingleton<IReservasClienteService>(new ReservasServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<Programacion>();

        // Mientras está cargando, el contenedor anuncia el esqueleto
        var statusNode = cut.Find("[role='status']");
        Assert.NotNull(statusNode);
        Assert.Equal("true", statusNode.GetAttribute("aria-busy"));
        Assert.Contains("Cargando programación de la semana...", statusNode.TextContent);
    }

    private class EventosServiceFalsoLento : IEventosClienteService
    {
        private readonly Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> _tarea;

        public EventosServiceFalsoLento(Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> tarea) => _tarea = tarea;

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default) => _tarea;

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(
            DateTimeOffset? desde, DateTimeOffset? hasta, int? pagina = null, int? tamano = null, CancellationToken ct = default) => _tarea;

        public Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Correcto(new List<ActividadAsignadaDto>()));

        public Task<EstadoDeCarga<Guid>> CrearEventoAsync(CrearEventoRequest request, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<Guid>.Correcto(Guid.NewGuid()));

        public Task<ResultadoOperacion> ActualizarEventoAsync(Guid id, ActualizarEventoRequest request, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<ResultadoOperacion> DesactivarOEliminarEventoAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<IReadOnlyList<OpcionPatronRecurrencia>> ObtenerPatronesRecurrenciaSoportadosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OpcionPatronRecurrencia>>(new List<OpcionPatronRecurrencia>());
    }
}
