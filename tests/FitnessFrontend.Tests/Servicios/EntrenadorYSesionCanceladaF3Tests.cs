using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Asistencia;
using FitnessFrontend.Paginas.Eventos;
using FitnessFrontend.Paginas.Reservas;
using FitnessFrontend.Servicios.Asistencia;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reservas;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;
using AnuncioSesionDto = FitnessFrontend.Servicios.Eventos.AnuncioSesionDto;

namespace FitnessFrontend.Tests.Servicios;

public class EntrenadorYSesionCanceladaF3Tests : BunitContext
{
    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _principal;

        public FakeAuthStateProvider(string rol = "Entrenador", string nombre = "Carlos Entrenador")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, nombre),
                new Claim(ClaimTypes.Role, rol),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }, "TestAuth");
            _principal = new ClaimsPrincipal(identity);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_principal));
    }

    private class AsistenciaManualServiceFalso : IAsistenciaManualClienteService
    {
        public List<InscritoAsistenciaManualDto> Inscritos { get; set; } = new();
        public bool Simular403 { get; set; }
        public Guid? UltimoDesmarcadoReservaId { get; private set; }
        public Guid? UltimoMarcadoReservaId { get; private set; }

        public Task<EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>> ObtenerInscritosAsistenciaAsync(
            Guid sesionId, CancellationToken ct = default)
        {
            if (Simular403)
            {
                return Task.FromResult(EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Fallo(
                    "No tienes permisos para gestionar la asistencia de esta sesión (403)", "403"));
            }

            return Task.FromResult(EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Correcto(Inscritos));
        }

        public Task<ResultadoOperacion> MarcarAsistenciaManualAsync(
            Guid sesionId, Guid reservaId, string? motivo = null, CancellationToken ct = default)
        {
            UltimoMarcadoReservaId = reservaId;
            var item = Inscritos.FirstOrDefault(x => x.ReservaId == reservaId);
            if (item != null)
            {
                Inscritos.Remove(item);
                Inscritos.Add(item with { Asistio = true, FechaHoraMarcado = DateTimeOffset.UtcNow, Motivo = motivo });
            }
            return Task.FromResult(ResultadoOperacion.Exito("Marcado"));
        }

        public Task<ResultadoOperacion> DesmarcarAsistenciaManualAsync(
            Guid sesionId, Guid reservaId, string? motivo = null, CancellationToken ct = default)
        {
            UltimoDesmarcadoReservaId = reservaId;
            var item = Inscritos.FirstOrDefault(x => x.ReservaId == reservaId);
            if (item != null)
            {
                Inscritos.Remove(item);
                Inscritos.Add(item with { Asistio = false, FechaHoraMarcado = null, Motivo = motivo });
            }
            return Task.FromResult(ResultadoOperacion.Exito("Desmarcado"));
        }
    }

    private class NotasYAnunciosServiceFalso : INotasYAnunciosClienteService
    {
        public List<NotaIncidenciaDto> Notas { get; set; } = new();
        public List<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto> Anuncios { get; set; } = new();

        public Task<EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>> ObtenerNotasAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Correcto(Notas));

        public Task<ResultadoOperacion> CrearNotaAsync(Guid sesionId, string tipo, string contenido, CancellationToken ct = default)
        {
            Notas.Add(new NotaIncidenciaDto(Guid.NewGuid(), sesionId, tipo, contenido, DateTimeOffset.UtcNow, "Entrenador"));
            return Task.FromResult(ResultadoOperacion.Exito("Nota guardada"));
        }

        public Task<EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>> ObtenerAnunciosPorSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>.Correcto(Anuncios));

        public Task<EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>> ObtenerAnunciosMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>.Correcto(Anuncios));

        public Task<ResultadoOperacion> PublicarAnuncioAsync(Guid sesionId, string mensaje, CancellationToken ct = default)
        {
            Anuncios.Add(new FitnessFrontend.Servicios.Eventos.AnuncioSesionDto(Guid.NewGuid(), sesionId, mensaje, DateTimeOffset.UtcNow, "Entrenador"));
            return Task.FromResult(ResultadoOperacion.Exito("Anuncio publicado"));
        }
    }

    private class ReservasServiceFalso : IReservasClienteService
    {
        public List<ReservaResumenDto> MisReservas { get; set; } = new();

        public Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(MisReservas));

        public Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito("Cancelada"));

        public Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    [Fact]
    public void Prueba1_Aislamiento403_EntrenadorAjeno_MuestraErrorPermisos()
    {
        // 1. Aislamiento 403: un entrenador ajeno a la sesión ve error de permisos claro
        var sesionId = Guid.NewGuid();
        var servicio = new AsistenciaManualServiceFalso { Simular403 = true };

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IAsistenciaManualClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());
        Services.AddSingleton<FitnessFrontend.Servicios.Infraestructura.IAvisosService>(new FitnessFrontend.Servicios.Infraestructura.AvisosService());

        var cut = Render<AsistenciaManual>(p => p.Add(x => x.IdSesion, sesionId));

        var markup = cut.Markup;
        Assert.Contains("Acceso no autorizado", markup);
        Assert.Contains("No tienes permisos para gestionar la asistencia", markup);
        Assert.Empty(cut.FindAll("table"));
    }

    [Fact]
    public void Prueba2_DesmarcarAsistenciaManual_PideConfirmacionConConsecuencia_YRevertirEstado()
    {
        // 2. Confirmación al desmarcar asistencia manual: no se desmarca de inmediato, pide confirmación
        var sesionId = Guid.NewGuid();
        var reservaId = Guid.NewGuid();
        var servicio = new AsistenciaManualServiceFalso();
        servicio.Inscritos.Add(new InscritoAsistenciaManualDto(
            reservaId, Guid.NewGuid(), "Estudiante Juan Gomez", "juan.gomez@itm.edu.co", true, DateTimeOffset.UtcNow, null));

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IAsistenciaManualClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());
        Services.AddSingleton<FitnessFrontend.Servicios.Infraestructura.IAvisosService>(new FitnessFrontend.Servicios.Infraestructura.AvisosService());

        var cut = Render<AsistenciaManual>(p => p.Add(x => x.IdSesion, sesionId));

        // Busca botón "Desmarcar"
        var botonDesmarcar = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == "Desmarcar");
        Assert.NotNull(botonDesmarcar);

        // Hace clic en Desmarcar
        botonDesmarcar.Click();

        // Verifica que se abrió el diálogo de confirmación y aún NO se ha llamado al servicio
        Assert.Null(servicio.UltimoDesmarcadoReservaId);
        Assert.Contains("Confirmar desmarcado de asistencia", cut.Markup);
        Assert.Contains("Esta acción revertirá el estado a", cut.Markup);

        // Confirma en el diálogo modal
        var botonConfirmar = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Sí, desmarcar asistencia"));
        Assert.NotNull(botonConfirmar);
        botonConfirmar.Click();

        // Ahora sí se invocó el servicio con el ID de la reserva
        Assert.Equal(reservaId, servicio.UltimoDesmarcadoReservaId);
    }

    [Fact]
    public void Prueba3_EscapeEstrictoEnNotasYAnuncios_NoInterpretaScriptsNiHtml()
    {
        // 3. Escape estricto contra <script> y tags HTML en notas y anuncios
        var sesionId = Guid.NewGuid();
        var servicio = new NotasYAnunciosServiceFalso();

        var scriptPeligrosoNota = "<script>alert('xss-nota')</script><b>Texto en negrita</b>";
        var scriptPeligrosoAnuncio = "<img src=x onerror=alert('xss-anuncio')><a href='javascript:void(0)'>Link malicioso</a>";

        servicio.Notas.Add(new NotaIncidenciaDto(
            Guid.NewGuid(), sesionId, "Incidencia", scriptPeligrosoNota, DateTimeOffset.UtcNow, "Entrenador"));

        servicio.Anuncios.Add(new AnuncioSesionDto(
            Guid.NewGuid(), sesionId, scriptPeligrosoAnuncio, DateTimeOffset.UtcNow, "Entrenador"));

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<INotasYAnunciosClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());
        Services.AddSingleton<FitnessFrontend.Servicios.Infraestructura.IAvisosService>(new FitnessFrontend.Servicios.Infraestructura.AvisosService());

        var cut = Render<NotasDeSesion>(p => p.Add(x => x.IdSesion, sesionId));

        // Verificamos que NO existen elementos <script> ni <img> en el DOM parseado
        Assert.Empty(cut.FindAll("script"));
        Assert.Empty(cut.FindAll("img"));

        // Verificamos que el texto literal sí aparece pero escapado como texto plano
        var markup = cut.Markup;
        Assert.DoesNotContain("<script>", markup);
        Assert.DoesNotContain("<img", markup);
        Assert.Contains("&lt;script&gt;", markup);
        Assert.Contains("xss-nota", markup);
        Assert.Contains("xss-anuncio", markup);
    }

    [Fact]
    public void Prueba4_SesionCanceladaLadoUsuario_InsigniaYMotivo_SinBotonCancelar_RN020()
    {
        // 4. Sesión cancelada en MisReservas muestra insignia "Sesión cancelada", motivo, sin botón cancelar ni datos de priorización
        var reservasService = new ReservasServiceFalso();
        reservasService.MisReservas.Add(new ReservaResumenDto
        {
            Id = Guid.NewGuid(),
            EventoNombre = "Spinning Matutino",
            Estado = 2, // Cancelada
            SesionCancelada = true,
            OrigenCancelacion = "SesionCancelada",
            MotivoCancelacion = "Mantenimiento preventivo en bicicletas estáticas",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddDays(1),
            FechaHoraFin = DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
        });

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("Usuario", "Estudiante ITM"));
        Services.AddSingleton<IReservasClienteService>(reservasService);
        Services.AddSingleton<INotasYAnunciosClienteService>(new NotasYAnunciosServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());
        Services.AddSingleton<FitnessFrontend.Servicios.Infraestructura.IAvisosService>(new FitnessFrontend.Servicios.Infraestructura.AvisosService());

        var cut = Render<MisReservas>();
        var markup = cut.Markup;

        // Verifica insignia y mensaje
        Assert.Contains("Sesión cancelada", markup);
        Assert.Contains("Mantenimiento preventivo en bicicletas estáticas", markup);

        // Verifica que NO hay botón cancelar para esa reserva
        var botonesCancelar = cut.FindAll("button").Where(b => b.TextContent.Trim() == "Cancelar").ToList();
        Assert.Empty(botonesCancelar);

        // RN-020 Estricto: Ningún dato de nivel, confiabilidad ni posición
        Assert.DoesNotMatch(new Regex(@"puesto\s+\d+", RegexOptions.IgnoreCase), markup);
        Assert.DoesNotMatch(new Regex(@"posición\s+\d+", RegexOptions.IgnoreCase), markup);
        Assert.DoesNotContain("indicador", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prioridad", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("penalización", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nivel", markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Prueba5_ContadorEnVivoCaracteres_NotasYAnuncios()
    {
        // 5. Contador en vivo de caracteres en notas (1000) y anuncios (500)
        var sesionId = Guid.NewGuid();
        var servicio = new NotasYAnunciosServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<INotasYAnunciosClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());
        Services.AddSingleton<FitnessFrontend.Servicios.Infraestructura.IAvisosService>(new FitnessFrontend.Servicios.Infraestructura.AvisosService());

        var cut = Render<NotasDeSesion>(p => p.Add(x => x.IdSesion, sesionId));

        var textareaNota = cut.Find("#contenidoNota");
        var contadorNota = cut.Find("#contadorNota");
        Assert.Contains("0 / 1000", contadorNota.TextContent);

        // Simula tipeo en vivo (evento oninput)
        textareaNota.Input("Se presentó fuga de agua cerca del área de mancuernas.");
        contadorNota = cut.Find("#contadorNota");
        Assert.Contains("54 / 1000", contadorNota.TextContent);

        var textareaAnuncio = cut.Find("#mensajeAnuncio");
        var contadorAnuncio = cut.Find("#contadorAnuncio");
        Assert.Contains("0 / 500", contadorAnuncio.TextContent);

        textareaAnuncio.Input("Por favor traer calzado limpio.");
        contadorAnuncio = cut.Find("#contadorAnuncio");
        Assert.Contains("31 / 500", contadorAnuncio.TextContent);
    }
}
