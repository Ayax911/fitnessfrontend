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
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Prueba obligatoria Fase 1 (docs/arquitectura/08-guia-de-implementacion.md, skill pruebas-obligatorias-fitnessfrontend):
// Aislamiento de pertenencia del entrenador con dos entrenadores de prueba (RF-010, RN-007)
public class MisActividadesPertenenciaTests : BunitContext
{
    private class MockEventosClienteService : IEventosClienteService
    {
        private readonly Dictionary<string, IReadOnlyList<ActividadAsignadaDto>> _actividadesPorEntrenador;
        private string _entrenadorActivo;

        public MockEventosClienteService(string entrenadorInicial, Dictionary<string, IReadOnlyList<ActividadAsignadaDto>> actividades)
        {
            _entrenadorActivo = entrenadorInicial;
            _actividadesPorEntrenador = actividades;
        }

        public void CambiarEntrenadorActivo(string entrenadorId)
        {
            _entrenadorActivo = entrenadorId;
        }

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(new List<EventoResumenDto>()));
        }

        public Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(DateTimeOffset? desde, DateTimeOffset? hasta, int? pagina = null, int? tamano = null, CancellationToken ct = default) =>
            ObtenerEventosDisponiblesAsync(ct);

        public Task<EstadoDeCarga<EventoResumenDto>> ObtenerEventoAsync(Guid id, CancellationToken ct = default) =>

            Task.FromResult(EstadoDeCarga<EventoResumenDto>.Fallo("No implementado en la prueba"));


        public Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default)
        {
            var actividades = _actividadesPorEntrenador.TryGetValue(_entrenadorActivo, out var list)
                ? list
                : new List<ActividadAsignadaDto>();

            return Task.FromResult(EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Correcto(actividades));
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

    [Fact]
    public void MisActividades_ConDosEntrenadores_MuestraExclusivamenteLasPropias()
    {
        // ARRANGE: Dos entrenadores con actividades asignadas independientes
        var entrenador1Id = "entrenador-1";
        var entrenador2Id = "entrenador-2";

        var actividadEntrenador1 = new ActividadAsignadaDto
        {
            EventoId = Guid.NewGuid(),
            Nombre = "Spinning Avanzado - Profe Carlos",
            Descripcion = "Clase de ciclismo bajo techo",
            EntrenadorNombre = "Carlos Entrenador 1",
            Sesiones = new List<SesionResumenDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FechaHoraInicio = DateTimeOffset.UtcNow.AddHours(2),
                    FechaHoraFin = DateTimeOffset.UtcNow.AddHours(3),
                    CupoMaximo = 20,
                    CuposOcupados = 12
                }
            }
        };

        var actividadEntrenador2 = new ActividadAsignadaDto
        {
            EventoId = Guid.NewGuid(),
            Nombre = "Yoga Integral - Profe Diana",
            Descripcion = "Clase de respiración y flexibilidad",
            EntrenadorNombre = "Diana Entrenadora 2",
            Sesiones = new List<SesionResumenDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FechaHoraInicio = DateTimeOffset.UtcNow.AddHours(5),
                    FechaHoraFin = DateTimeOffset.UtcNow.AddHours(6),
                    CupoMaximo = 15,
                    CuposOcupados = 8
                }
            }
        };

        var diccionario = new Dictionary<string, IReadOnlyList<ActividadAsignadaDto>>
        {
            [entrenador1Id] = new List<ActividadAsignadaDto> { actividadEntrenador1 },
            [entrenador2Id] = new List<ActividadAsignadaDto> { actividadEntrenador2 }
        };

        var mockService = new MockEventosClienteService(entrenador1Id, diccionario);

        Services.AddSingleton<IEventosClienteService>(mockService);
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        // ACT 1: Renderizado para Entrenador 1
        var cut1 = Render<MisActividades>();

        // ASSERT 1: Entrenador 1 solo ve Spinning y NO ve Yoga
        var htmlEntrenador1 = cut1.Markup;
        Assert.Contains("Spinning Avanzado - Profe Carlos", htmlEntrenador1);
        Assert.DoesNotContain("Yoga Integral - Profe Diana", htmlEntrenador1);

        // ACT 2: Cambiar contexto al Entrenador 2 y re-renderizar
        mockService.CambiarEntrenadorActivo(entrenador2Id);
        var cut2 = Render<MisActividades>();

        // ASSERT 2: Entrenador 2 solo ve Yoga y NO ve Spinning (RN-007)
        var htmlEntrenador2 = cut2.Markup;
        Assert.Contains("Yoga Integral - Profe Diana", htmlEntrenador2);
        Assert.DoesNotContain("Spinning Avanzado - Profe Carlos", htmlEntrenador2);
    }

    // Observación de uso (2026-11-02): con muchas sesiones recurrentes la página se volvía una lista larga.
    // Ahora cada actividad se despliega, muestra 8 sesiones y ofrece "Mostrar todas".
    [Fact]
    public void MisActividades_ColapsadasPorDefecto_YUnaActividadLargaSeLimitaConMostrarTodas()
    {
        var entrenadorId = Guid.NewGuid();
        var sesiones = new List<SesionResumenDto>();
        for (var i = 0; i < 12; i++)
        {
            sesiones.Add(new SesionResumenDto
            {
                Id = Guid.NewGuid(),
                FechaHoraInicio = DateTimeOffset.UtcNow.AddDays(i + 1),
                FechaHoraFin = DateTimeOffset.UtcNow.AddDays(i + 1).AddHours(1),
                CupoMaximo = 20,
                CuposOcupados = 3
            });
        }
        var actividad = new ActividadAsignadaDto
        {
            EventoId = Guid.NewGuid(),
            Nombre = "Funcional Recurrente",
            EntrenadorNombre = "Entrenador Uno",
            EsRecurrente = true,
            Sesiones = sesiones
        };
        var diccionario = new Dictionary<string, IReadOnlyList<ActividadAsignadaDto>>
        {
            [entrenadorId.ToString()] = new List<ActividadAsignadaDto> { actividad }
        };
        Services.AddSingleton<IEventosClienteService>(new MockEventosClienteService(entrenadorId.ToString(), diccionario));
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<MisActividades>();

        // Colapsada: se ve el nombre y el resumen, pero ninguna sesión
        Assert.Contains("Funcional Recurrente", cut.Markup);
        Assert.Contains("12 sesiones", cut.Markup);
        Assert.Empty(cut.FindAll("a.btn-outline-primary"));

        // Expandida: solo las primeras 8 con sus accesos, y un botón para ver las 12
        cut.Find("button.itm-actividad__cab").Click();
        Assert.Equal(8, cut.FindAll("li.itm-sesion").Count);
        Assert.Contains("Mostrar las 12 sesiones", cut.Markup);

        cut.Find(".itm-mostrar-mas button").Click();
        Assert.Equal(12, cut.FindAll("li.itm-sesion").Count);
        Assert.Contains("Mostrar menos", cut.Markup);
    }
}
