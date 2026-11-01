using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Eventos;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reservas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Prueba obligatoria Fase 2 (docs/arquitectura/08-guia-de-implementacion.md, skill pruebas-obligatorias-fitnessfrontend):
// Comportamiento de AMB-014 ante rechazo con codigoError = VentanaNoAbierta.
public class Amb014VentanaNoAbiertaTests : BunitContext
{
    private class FakeEventosClienteService : IEventosClienteService
    {
        private readonly List<EventoResumenDto> _eventos;
        public FakeEventosClienteService(List<EventoResumenDto> eventos) => _eventos = eventos;

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(_eventos));

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(DateTimeOffset? desde, DateTimeOffset? hasta, int? pagina = null, int? tamano = null, CancellationToken ct = default) =>
            ObtenerEventosDisponiblesAsync(ct);

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

    private class FakeReservasClienteService : IReservasClienteService
    {
        private readonly Func<Guid, EstadoDeCarga<ReservaResumenDto>> _onReservar;

        public FakeReservasClienteService(Func<Guid, EstadoDeCarga<ReservaResumenDto>> onReservar)
        {
            _onReservar = onReservar;
        }

        public Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(_onReservar(sesionEventoId));

        public Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(new List<ReservaResumenDto>()));

        public Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Correcto(new List<ReservaInscritoDto>()));
    }

    [Fact]
    public async Task CuandoVentanaNoAbierta_DebeMostrarMensajeGenericoNeutro_SinRevelarNivelNiFecha()
    {
        var sesionId = Guid.NewGuid();
        var eventos = new List<EventoResumenDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Clase Funcional",
                Descripcion = "Entrenamiento para todos",
                EntrenadorNombre = "Entrenador Prueba",
                Sesiones = new List<SesionResumenDto>
                {
                    new()
                    {
                        Id = sesionId,
                        FechaHoraInicio = DateTimeOffset.UtcNow.AddDays(3),
                        FechaHoraFin = DateTimeOffset.UtcNow.AddDays(3).AddHours(1),
                        CupoMaximo = 20,
                        CuposDisponibles = 20
                    }
                }
            }
        };

        // Simula rechazo del backend con VentanaNoAbierta
        var reservasService = new FakeReservasClienteService(id =>
            EstadoDeCarga<ReservaResumenDto>.Fallo(
                "La ventana de reserva para tu nivel de priorización aún no se encuentra abierta.",
                "VentanaNoAbierta"));

        Services.AddSingleton<IEventosClienteService>(new FakeEventosClienteService(eventos));
        Services.AddSingleton<IReservasClienteService>(reservasService);
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<ListaEventos>();

        // Encuentra el botón de reservar y hace clic
        var botonReservar = cut.Find("button.btn-itm-primario");
        Assert.False(botonReservar.HasAttribute("disabled"));

        await cut.InvokeAsync(() => botonReservar.Click());

        var markup = cut.Markup;

        // ASERCIÓN POSITIVA OBLIGATORIA (comportamiento menos comprometedor AMB-014):
        // Muestra texto genérico y neutro
        Assert.Contains("Reservas aún no disponibles para esta sesión", markup);

        // La acción queda deshabilitada para esa sesión
        var botonDeshabilitado = cut.Find("button.btn-itm-primario");
        Assert.True(botonDeshabilitado.HasAttribute("disabled"));

        // ASERCIONES NEGATIVAS OBLIGATORIAS (RN-020):
        // NO debe revelar nivel, ni penalización, ni motivo interno, ni fecha estimada de habilitación
        var markupMinuscula = markup.ToLowerInvariant();
        Assert.DoesNotContain("nivel", markupMinuscula);
        Assert.DoesNotContain("prioridad", markupMinuscula);
        Assert.DoesNotContain("priorización", markupMinuscula);
        Assert.DoesNotContain("penalizado", markupMinuscula);
        Assert.DoesNotContain("penalización", markupMinuscula);
        Assert.DoesNotContain("sanción", markupMinuscula);
        Assert.DoesNotContain("podrás reservar en", markupMinuscula);
    }
}
