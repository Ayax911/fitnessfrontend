using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Reservas;

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada cliente para el módulo Reservas
public interface IReservasClienteService
{
    // RF-002, RF-003: Reservar cupo o quedar en lista de espera
    Task<EstadoDeCarga<ReservaResumenDto>> ReservarAsync(Guid sesionEventoId, CancellationToken ct = default);

    // RF-004: Cancelar reserva
    Task<ResultadoOperacion> CancelarReservaAsync(Guid reservaId, CancellationToken ct = default);

    // RF-020: Consultar reservas propias del usuario autenticado
    Task<EstadoDeCarga<IReadOnlyList<ReservaResumenDto>>> ObtenerMisReservasAsync(CancellationToken ct = default);

    // RF-011: Consultar inscritos de una sesión (Entrenador dueño de la sesión)
    Task<EstadoDeCarga<IReadOnlyList<ReservaInscritoDto>>> ConsultarInscritosSesionAsync(Guid sesionEventoId, CancellationToken ct = default);
}
