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

// RF-001: Listar eventos disponibles con horario en America/Bogota y cupos
public class ListaEventosTests : BunitContext
{
    private class FakeEventosClienteService : IEventosClienteService
    {
        private readonly IReadOnlyList<EventoResumenDto> _eventos;

        public FakeEventosClienteService(IReadOnlyList<EventoResumenDto> eventos)
        {
            _eventos = eventos;
        }

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(_eventos));
        }

        public Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Correcto(new List<ActividadAsignadaDto>()));
        }

        public Task<EstadoDeCarga<Guid>> CrearEventoAsync(CrearEventoRequest request, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<Guid>.Correcto(Guid.NewGuid()));

        public Task<ResultadoOperacion> ActualizarEventoAsync(Guid id, ActualizarEventoRequest request, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<ResultadoOperacion> DesactivarOEliminarEventoAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<IReadOnlyList<OpcionPatronRecurrencia>> ObtenerPatronesRecurrenciaSoportadosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OpcionPatronRecurrencia>>(new List<OpcionPatronRecurrencia>());
    }

    private class FakeReservasClienteService : FitnessFrontend.Servicios.Reservas.IReservasClienteService
    {
        public Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<ReservaResumenDto>.Correcto(new ReservaResumenDto()));

        public Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(new List<ReservaResumenDto>()));

        public Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Correcto(new List<ReservaInscritoDto>()));
    }

    [Fact]
    public void ListaEventos_DebeRenderizarNombreEntrenadorYHorarioEnBogota()
    {
        // 2026-10-10 14:00 UTC -> 09:00 en Bogotá (UTC-5)
        var inicioUtc = new DateTimeOffset(2026, 10, 10, 14, 0, 0, TimeSpan.Zero);
        var finUtc = new DateTimeOffset(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

        var eventos = new List<EventoResumenDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Crossfit Funcional",
                Descripcion = "Entrenamiento de alta intensidad",
                EntrenadorNombre = "Profe Andrea",
                EsRecurrente = true,
                Sesiones = new List<SesionResumenDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        FechaHoraInicio = inicioUtc,
                        FechaHoraFin = finUtc,
                        CupoMaximo = 25,
                        CuposDisponibles = 10
                    }
                }
            }
        };

        var fakeReservasService = new FakeReservasClienteService();

        Services.AddSingleton<IEventosClienteService>(new FakeEventosClienteService(eventos));
        Services.AddSingleton<IReservasClienteService>(fakeReservasService);
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<ListaEventos>();
        var markup = cut.Markup;

        // Verifica nombre y entrenador
        Assert.Contains("Crossfit Funcional", markup);
        Assert.Contains("Profe Andrea", markup);
        Assert.Contains("10 cupos", markup);

        // Verifica que la hora renderizada es la de Bogotá (09:00 a. m.), no 14:00 UTC
        Assert.Contains("09:00", markup);
        Assert.DoesNotContain("14:00", markup);
    }

    [Fact]
    public void ListaEventos_I08_NoDebeExponerFechaAperturaReservas_ParaProtegerConfidencialidadRN020()
    {
        // I-08 / RN-020: La fecha base de apertura no debe mostrarse públicamente
        // para que un usuario penalizado no deduzca que su bloqueo es personal.
        var eventos = new List<EventoResumenDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nombre = "Spinning",
                Descripcion = "Cardio intenso",
                EntrenadorNombre = "Profe Carlos",
                FechaAperturaReservas = DateTimeOffset.UtcNow.AddDays(-1),
                Sesiones = new List<SesionResumenDto>()
            }
        };

        Services.AddSingleton<IEventosClienteService>(new FakeEventosClienteService(eventos));
        Services.AddSingleton<IReservasClienteService>(new FakeReservasClienteService());
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<ListaEventos>();
        var markup = cut.Markup;

        Assert.DoesNotContain("Apertura de reservas", markup);
    }
}
