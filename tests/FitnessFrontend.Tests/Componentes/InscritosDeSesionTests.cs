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

// RF-011: Consultar lista de inscritos en una sesión (Entrenador)
public class InscritosDeSesionTests : BunitContext
{
    private class FakeReservasClienteService : IReservasClienteService
    {
        private readonly List<ReservaInscritoDto> _inscritos;

        public FakeReservasClienteService(List<ReservaInscritoDto> inscritos)
        {
            _inscritos = inscritos;
        }

        public Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<ReservaResumenDto>.Correcto(new ReservaResumenDto()));

        public Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>.Correcto(new List<ReservaResumenDto>()));

        public Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>.Correcto(_inscritos));
    }

    [Fact]
    public void InscritosDeSesion_DebeRenderizarListaDeUsuariosConfirmados()
    {
        var sesionId = Guid.NewGuid();
        var inscritos = new List<ReservaInscritoDto>
        {
            new()
            {
                UsuarioId = Guid.NewGuid(),
                NombreCompleto = "Juan Pérez Gómez",
                Estado = 0 // Confirmada
            },
            new()
            {
                UsuarioId = Guid.NewGuid(),
                NombreCompleto = "María Rodríguez Soto",
                Estado = 0 // Confirmada
            }
        };

        Services.AddSingleton<IReservasClienteService>(new FakeReservasClienteService(inscritos));

        var cut = Render<InscritosDeSesion>(parameters => parameters
            .Add(p => p.IdSesion, sesionId)
        );

        var markup = cut.Markup;

        Assert.Contains("Total inscritos: 2", markup);
        Assert.Contains("Juan Pérez Gómez", markup);
        Assert.Contains("María Rodríguez Soto", markup);
        Assert.Contains("Confirmada", markup);
    }
}
