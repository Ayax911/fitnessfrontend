using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Eventos;

public record OpcionPatronRecurrencia(int Id, string Nombre, string Descripcion);

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada cliente para el módulo Eventos
public interface IEventosClienteService
{
    // RF-001: Listar eventos disponibles
    Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default);

    // RF-035: Programación por rango de fechas y paginación
    Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(DateTimeOffset? desde, DateTimeOffset? hasta, int? pagina = null, int? tamano = null, CancellationToken ct = default);

    // RF-010: Consultar actividades asignadas al entrenador autenticado
    Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default);

    // RF-007, RF-008: Crear un nuevo evento (único o recurrente)
    Task<EstadoDeCarga<Guid>> CrearEventoAsync(CrearEventoRequest request, CancellationToken ct = default);

    // RF-009: Actualizar datos de un evento existente
    Task<ResultadoOperacion> ActualizarEventoAsync(Guid id, ActualizarEventoRequest request, CancellationToken ct = default);

    // RF-009: Desactivar o eliminar un evento
    Task<ResultadoOperacion> DesactivarOEliminarEventoAsync(Guid id, CancellationToken ct = default);

    // RF-008: Patrones de recurrencia soportados por el backend (como dato extensible, no fijo en el marcado)
    Task<IReadOnlyList<OpcionPatronRecurrencia>> ObtenerPatronesRecurrenciaSoportadosAsync(CancellationToken ct = default);
}
