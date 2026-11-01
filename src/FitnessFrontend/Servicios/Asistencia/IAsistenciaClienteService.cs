using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Asistencia;

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada cliente para el módulo Asistencia
public interface IAsistenciaClienteService
{
    // RF-005: Registrar check-in por código QR en sesión deportiva
    Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarCheckInAsync(string tokenSesion, CancellationToken ct = default);

    // RF-006: Registrar ingreso general por torniquete / punto de acceso (RN-030)
    Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarIngresoGeneralAsync(string idPuntoAcceso, CancellationToken ct = default);

    // RF-021, RN-020: Consultar historial de asistencias propio (Usuario)
    Task<EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>> ObtenerHistorialAsync(CancellationToken ct = default);

    // RF-012: Consultar código QR dinámico de una sesión (Entrenador)
    Task<EstadoDeCarga<SesionQRDto>> ObtenerQRDeSesionAsync(Guid sesionId, CancellationToken ct = default);
}
