using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Eventos.Administrar;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Prueba obligatoria Fase 4 (docs/arquitectura/08-guia-de-implementacion.md, skill pruebas-obligatorias-fitnessfrontend):
// Recurrencia como dato extensible provisto por el servicio, no como lista fija en el marcado.
public class AdministrarEventosTests : BunitContext
{
    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;
        public FakeAuthStateProvider(string email, params string[] roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, email),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            _user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_user));
    }

    private class FakeEventosClienteService : IEventosClienteService
    {
        private readonly IReadOnlyList<OpcionPatronRecurrencia> _patrones;
        public CrearEventoRequest? UltimoRequestCreacion { get; private set; }

        public FakeEventosClienteService(IReadOnlyList<OpcionPatronRecurrencia> patrones)
        {
            _patrones = patrones;
        }

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(
            DateTimeOffset? desde,
            DateTimeOffset? hasta,
            int? pagina = null,
            int? tamano = null,
            CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<EventoResumenDto>> ObtenerEventoAsync(Guid id, CancellationToken ct = default) =>

            Task.FromResult(EstadoDeCarga<EventoResumenDto>.Fallo("No implementado en la prueba"));


        public Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<Guid>> CrearEventoAsync(CrearEventoRequest request, CancellationToken ct = default)
        {
            UltimoRequestCreacion = request;
            return Task.FromResult(EstadoDeCarga<Guid>.Correcto(Guid.NewGuid()));
        }

        public Task<ResultadoOperacion> ActualizarEventoAsync(Guid id, ActualizarEventoRequest request, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito("Actualizado"));

        public Task<ResultadoOperacion> DesactivarOEliminarEventoAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito("Eliminado"));

        public Task<IReadOnlyList<OpcionPatronRecurrencia>> ObtenerPatronesRecurrenciaSoportadosAsync(CancellationToken ct = default) =>
            Task.FromResult(_patrones);
    }

    [Fact]
    public void CrearEvento_RecurrenciaComoDatoExtensible_DebeReflejarOpcionesDelServicio()
    {
        // VERIFICACIÓN OBLIGATORIA FASE 4:
        // Los patrones soportados se inyectan como dato desde el servicio.
        // Si el backend añade un nuevo patrón (ej. Mensual), el formulario lo refleja sin modificar el marcado.
        var patronesDinamicos = new List<OpcionPatronRecurrencia>
        {
            new(0, "Días de la semana (Semanal)", "Se repite ciertos días cada semana"),
            new(1, "Patrón Extensible Mensual", "Se repite cada mes en fecha fija")
        };

        var auth = new FakeAuthStateProvider("admin@itm.edu.co", "Administrador");
        var servicio = new FakeEventosClienteService(patronesDinamicos);

        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddSingleton<IEventosClienteService>(servicio);
        Services.AddSingleton<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService>(new UsuariosClienteServiceFalso());
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<CrearEvento>();

        // Activa el switch de recurrencia
        var switchRecurrente = cut.Find("#esRecurrenteSwitch");
        switchRecurrente.Change(true);

        var markup = cut.Markup;

        // Comprueba que las opciones provistas por el servicio se renderizaron en el DOM
        Assert.Contains("Días de la semana (Semanal)", markup);
        Assert.Contains("Patrón Extensible Mensual", markup);
    }

    [Fact]
    public void CrearEvento_ExigeFechaAperturaReservas_RN028()
    {
        var auth = new FakeAuthStateProvider("admin@itm.edu.co", "Administrador");
        var servicio = new FakeEventosClienteService(new List<OpcionPatronRecurrencia>());

        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddSingleton<IEventosClienteService>(servicio);
        Services.AddSingleton<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService>(new UsuariosClienteServiceFalso());
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<CrearEvento>();

        // Llena nombre y cupo pero deja vacía la fecha de apertura
        cut.Find("#nombreEvento").Change("Torneo de Voleibol");
        cut.Find("#cupoMaximo").Change(16);

        // Envía el formulario
        cut.Find("form").Submit();

        var markup = cut.Markup;
        Assert.Contains("La fecha de apertura de reservas es obligatoria", markup);
    }

    [Fact]
    public void EditarEvento_ConfirmacionEliminar_NoPrometeComportamientoSobreReservas()
    {
        var auth = new FakeAuthStateProvider("admin@itm.edu.co", "Administrador");
        var servicio = new FakeEventosClienteService(new List<OpcionPatronRecurrencia>());

        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddSingleton<IEventosClienteService>(servicio);
        Services.AddSingleton<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService>(new UsuariosClienteServiceFalso());
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<EditarEvento>(parameters => parameters
            .Add(p => p.IdEvento, Guid.NewGuid()));

        // Hace clic en el botón de eliminar para abrir la confirmación
        var botonEliminar = cut.Find("button.btn-outline-danger");
        botonEliminar.Click();

        var markup = cut.Markup;
        // PENDIENTE (RF-009, información faltante): confirmación explícita sin promesas sobre reservas previas
        Assert.Contains("¿Confirmar eliminación del evento?", markup);
        Assert.Contains("Sí, eliminar", markup);
    }
}
