using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;

using FitnessFrontend.Layout;
using FitnessFrontend.Paginas.Eventos;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reservas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Observaciones de uso (2026-11-02): aviso flotante al reservar (RF-002, RF-003), botón "Menú" del Administrador,
// lista con "Mostrar más". RN-020: ningún aviso muestra posición en la lista de espera ni nivel de priorización.
public class AvisosYMenuQA3Tests : BunitContext
{
    // ------------------------------------------------------------------ Servicio de avisos
    [Fact]
    public void Avisos_Exito_SeAgregaYSeDescartaAlCerrar()
    {
        var avisos = new AvisosService();
        avisos.Exito("¡Reserva confirmada!", "Puedes consultarla en Mis reservas.", "Ver mis reservas", "/reservas");

        var aviso = Assert.Single(avisos.Activos);
        Assert.Equal(TipoAviso.Exito, aviso.Tipo);
        avisos.Descartar(aviso.Id);
        Assert.Empty(avisos.Activos);
    }

    [Fact]
    public void Avisos_MuestraComoMaximoTres_ElMasViejoSale()
    {
        var avisos = new AvisosService();
        for (var i = 1; i <= 5; i++) avisos.Info($"Aviso {i}");

        Assert.Equal(3, avisos.Activos.Count);
        Assert.Equal("Aviso 3", avisos.Activos[0].Titulo);
    }

    [Fact]
    public async Task Avisos_DesaparecenSolosTrasElTiempoConfigurado()
    {
        var avisos = new AvisosService { SegundosVisible = 0 };
        avisos.Exito("Listo");

        for (var i = 0; i < 40 && avisos.Activos.Count > 0; i++) await Task.Delay(50);
        Assert.Empty(avisos.Activos);
    }

    // ------------------------------------------------------------------ Componente flotante
    [Fact]
    public void ContenedorAvisos_RenderizaTituloMensajeEnlaceYRolSegunTipo()
    {
        var avisos = new AvisosService();
        Services.AddSingleton<IAvisosService>(avisos);
        var cut = Render<FitnessFrontend.Componentes.Compartidos.ContenedorAvisos>();

        avisos.Exito("¡Reserva confirmada!", "Puedes consultarla en Mis reservas.", "Ver mis reservas", "/reservas");
        avisos.Error("No se pudo reservar", "Inténtalo de nuevo.");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("¡Reserva confirmada!", cut.Markup);
            Assert.Contains("href=\"/reservas\"", cut.Markup);
            Assert.Single(cut.FindAll("div.itm-aviso--exito[role='status']"));
            Assert.Single(cut.FindAll("div.itm-aviso--error[role='alert']"));
        });

        cut.Find("div.itm-aviso--exito .itm-aviso__cerrar").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("div.itm-aviso--exito")));
    }

    // ------------------------------------------------------------------ Reservar desde la lista
    private class ReservasFalso : IReservasClienteService
    {
        public int EstadoDevuelto { get; set; }
        public Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<ReservaResumenDto>.Correcto(new ReservaResumenDto { Estado = EstadoDevuelto }));
        public Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default) => Task.FromResult(ResultadoOperacion.Exito());
        public Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(new List<ReservaResumenDto>()));
        public Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Correcto(new List<ReservaInscritoDto>()));
    }

    private class EventosFalso : IEventosClienteService
    {
        private readonly IReadOnlyList<EventoResumenDto> _eventos;
        public EventosFalso(IReadOnlyList<EventoResumenDto> eventos) => _eventos = eventos;
        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(_eventos));
        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(DateTimeOffset? desde, DateTimeOffset? hasta, int? pagina = null, int? tamano = null, CancellationToken ct = default) =>
            ObtenerEventosDisponiblesAsync(ct);
        public Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Correcto(new List<ActividadAsignadaDto>()));
        public Task<EstadoDeCarga<Guid>> CrearEventoAsync(CrearEventoRequest request, CancellationToken ct = default) => Task.FromResult(EstadoDeCarga<Guid>.Correcto(Guid.NewGuid()));
        public Task<ResultadoOperacion> ActualizarEventoAsync(Guid id, ActualizarEventoRequest request, CancellationToken ct = default) => Task.FromResult(ResultadoOperacion.Exito());
        public Task<ResultadoOperacion> DesactivarOEliminarEventoAsync(Guid id, CancellationToken ct = default) => Task.FromResult(ResultadoOperacion.Exito());
        public Task<IReadOnlyList<OpcionPatronRecurrencia>> ObtenerPatronesRecurrenciaSoportadosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OpcionPatronRecurrencia>>(new List<OpcionPatronRecurrencia>());
    }

    private static List<EventoResumenDto> EventoConSesiones(int cantidad)
    {
        var inicio = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(14);
        return new List<EventoResumenDto>
        {
            new()
            {
                Id = Guid.NewGuid(), Nombre = "Spinning", EntrenadorNombre = "Profe Carlos",
                Sesiones = Enumerable.Range(0, cantidad).Select(i => new SesionResumenDto
                {
                    Id = Guid.NewGuid(), FechaHoraInicio = inicio.AddMinutes(i), FechaHoraFin = inicio.AddMinutes(i + 50),
                    CupoMaximo = 20, CuposDisponibles = 10
                }).ToList()
            }
        };
    }

    private (IRenderedComponent<ListaEventos> cut, AvisosService avisos) RenderLista(int estado, int sesiones = 1)
    {
        var avisos = new AvisosService();
        Services.AddSingleton<IEventosClienteService>(new EventosFalso(EventoConSesiones(sesiones)));
        Services.AddSingleton<IReservasClienteService>(new ReservasFalso { EstadoDevuelto = estado });
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();
        Services.AddSingleton<IAvisosService>(avisos);
        return (Render<ListaEventos>(), avisos);
    }

    [Fact]
    public void Reservar_Confirmada_MuestraAvisoFlotanteConEnlaceAMisReservas()
    {
        var (cut, avisos) = RenderLista(estado: 0);
        cut.WaitForElement("button.btn-itm-primario").Click();

        cut.WaitForAssertion(() =>
        {
            var aviso = Assert.Single(avisos.Activos);
            Assert.Equal(TipoAviso.Exito, aviso.Tipo);
            Assert.Equal("¡Reserva confirmada!", aviso.Titulo);
            Assert.Equal("/reservas", aviso.EnlaceUrl);
        });
        // Ya no se pinta una alerta dentro de la página (obligaba a desplazarse para verla)
        Assert.DoesNotContain("alert-success", cut.Markup);
    }

    [Fact]
    public void Reservar_ListaDeEspera_AvisaSinRevelarPosicionNiNivel_RN020()
    {
        var (cut, avisos) = RenderLista(estado: 1);
        cut.WaitForElement("button.btn-itm-primario").Click();

        cut.WaitForAssertion(() =>
        {
            var aviso = Assert.Single(avisos.Activos);
            Assert.Equal(TipoAviso.Espera, aviso.Tipo);
            var texto = (aviso.Titulo + " " + aviso.Mensaje).ToLowerInvariant();
            foreach (var prohibido in new[] { "posición", "posicion", "prioriz", "nivel", "confiab" })
                Assert.DoesNotContain(prohibido, texto);
        });
    }

    [Fact]
    public void ListaEventos_MuestraDoceTarjetasYUnBotonMostrarMas()
    {
        var (cut, _) = RenderLista(estado: 0, sesiones: 15);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(12, cut.FindAll("article.itm-tarjeta").Count);
            Assert.Contains("Mostrar más actividades (3 restantes)", cut.Markup);
        });

        cut.Find(".itm-mas button").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(15, cut.FindAll("article.itm-tarjeta").Count);
            Assert.Empty(cut.FindAll(".itm-mas"));
        });
    }

    // ------------------------------------------------------------------ Barra y botón Menú
    private class AutenticacionFalsa : IAutenticacionService
    {
        public Task<ResultadoOperacion> IniciarSesionAsync(string email, string password) => Task.FromResult(ResultadoOperacion.Exito());
        public Task<ResultadoOperacion> RegistrarAsync(string email, string password, string nombreCompleto, TipoUsuario tipoUsuario, bool aceptaTratamientoDatos) => Task.FromResult(ResultadoOperacion.Exito());
        public Task CerrarSesionAsync() => Task.CompletedTask;
        public Task<string?> ObtenerTokenAsync() => Task.FromResult<string?>("t");
        public Task<UsuarioSesion?> ObtenerUsuarioActualAsync() => Task.FromResult<UsuarioSesion?>(null);
        public Task LimpiarSesionAsync() => Task.CompletedTask;
        public Task<ResultadoOperacion> RenovarSesionAsync() => Task.FromResult(ResultadoOperacion.Exito());
    }

    private IRenderedComponent<MainLayout> RenderLayout(string rol)
    {
        var auth = AddAuthorization();
        auth.SetAuthorized("persona@itm.test");
        auth.SetRoles(rol);
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<IAutenticacionService>(new AutenticacionFalsa());
        Services.AddSingleton<IAvisosService>(new AvisosService());
        return Render<MainLayout>(p => p.Add(x => x.Body, (RenderFragment)(b => { })));
    }

    [Fact]
    public void Administrador_BotonMenuSiempreDisponible_ElPanelTraeLosDiezDestinos_YNoDesbordaLaBarra()
    {
        var cut = RenderLayout("Administrador");

        // Los 10 destinos NO van en la barra (se salían de ella): solo en el panel
        Assert.Empty(cut.FindAll(".itm-barra .itm-barra__enlaces"));
        Assert.Empty(cut.FindAll("#menu-movil"));

        var boton = cut.Find("button.itm-menu-boton--siempre");
        Assert.Equal("false", boton.GetAttribute("aria-expanded"));
        boton.Click();

        cut.WaitForAssertion(() =>
        {
            var panel = cut.Find("nav#menu-movil.itm-menu-movil--siempre");
            Assert.Equal(10, panel.QuerySelectorAll("a").Length);
            Assert.Equal("true", cut.Find("button.itm-menu-boton--siempre").GetAttribute("aria-expanded"));
        });

        cut.Find("button.itm-menu-boton--siempre").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("#menu-movil")));
    }

    [Fact]
    public void Entrenador_LlevaSusDosEnlacesEnLaBarra_YElBotonMenuEsSoloDeCelular()
    {
        var cut = RenderLayout("Entrenador");

        Assert.Equal(2, cut.FindAll(".itm-barra .itm-barra__enlaces a").Count);
        var boton = cut.Find("button.itm-menu-boton");
        Assert.DoesNotContain("itm-menu-boton--siempre", boton.ClassName);
    }
}
