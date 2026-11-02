using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Reservas;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reservas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// RF-004: Cancelar una reserva activa
// RF-020: Consultar sus propias reservas
public class MisReservasTests : BunitContext
{
    private class FakeReservasClienteService : IReservasClienteService
    {
        public bool CancelarFueLlamado { get; private set; }
        public Guid ReservaCanceladaId { get; private set; }

        private readonly List<ReservaResumenDto> _reservas;

        public FakeReservasClienteService(List<ReservaResumenDto> reservas)
        {
            _reservas = reservas;
        }

        public Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<ReservaResumenDto>.Correcto(new ReservaResumenDto()));

        public Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default)
        {
            CancelarFueLlamado = true;
            ReservaCanceladaId = reservaId;
            return Task.FromResult(ResultadoOperacion.Exito("Reserva cancelada correctamente."));
        }

        public Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(_reservas));

        public Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Correcto(new List<ReservaInscritoDto>()));
    }

    private class FakeNotasYAnunciosClienteService : FitnessFrontend.Servicios.Eventos.INotasYAnunciosClienteService
    {
        public Task<EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.NotaIncidenciaDto>>> ObtenerNotasAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.NotaIncidenciaDto>>.Correcto(new List<FitnessFrontend.Servicios.Eventos.NotaIncidenciaDto>()));

        public Task<ResultadoOperacion> CrearNotaAsync(Guid sesionId, string tipo, string contenido, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>> ObtenerAnunciosPorSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>.Correcto(new List<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>()));

        public Task<EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>> ObtenerAnunciosMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>>.Correcto(new List<FitnessFrontend.Servicios.Eventos.AnuncioSesionDto>()));

        public Task<ResultadoOperacion> PublicarAnuncioAsync(Guid sesionId, string mensaje, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());
    }

    [Fact]
    public async Task MisReservas_DebeMostrarListadoYPermitirCancelar()
    {
        var reservaId = Guid.NewGuid();
        // Una reserva futura (aparece en "Próximas" y se puede cancelar): 14:00 UTC = 9:00 a. m. en Bogotá
        var inicioUtc = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(14), TimeSpan.Zero);
        var finUtc = inicioUtc.AddHours(1);

        var reservas = new List<ReservaResumenDto>
        {
            new()
            {
                Id = reservaId,
                EventoNombre = "Natación Libre",
                Estado = 0, // Confirmada
                FechaHoraInicio = inicioUtc,
                FechaHoraFin = finUtc,
                FechaHoraSolicitud = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        var fakeService = new FakeReservasClienteService(reservas);

        Services.AddSingleton<IReservasClienteService>(fakeService);
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();
        Services.AddSingleton<FitnessFrontend.Servicios.Infraestructura.IAvisosService>(new FitnessFrontend.Servicios.Infraestructura.AvisosService());
        Services.AddSingleton<FitnessFrontend.Servicios.Eventos.INotasYAnunciosClienteService>(new FakeNotasYAnunciosClienteService());

        var cut = Render<MisReservas>();
        var markup = cut.Markup;

        // Verifica renderizado de datos
        Assert.Contains("Natación Libre", markup);
        Assert.Contains("Reserva confirmada", markup);
        Assert.Contains("9:00", markup);
        Assert.Contains("Próximas", markup);

        // "Cancelar reserva" pide confirmación antes de cancelar (diseño "Confirmar cancelación")
        await cut.InvokeAsync(() => cut.Find("button.itm-cancelar").Click());
        Assert.False(fakeService.CancelarFueLlamado);
        Assert.Contains("¿Cancelar tu reserva?", cut.Markup);
        await cut.InvokeAsync(() => cut.Find(".itm-modal__acciones button.btn-danger").Click());

        Assert.True(fakeService.CancelarFueLlamado);
        Assert.Equal(reservaId, fakeService.ReservaCanceladaId);
    }
}
