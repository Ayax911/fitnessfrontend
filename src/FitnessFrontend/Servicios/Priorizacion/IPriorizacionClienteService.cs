using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Priorizacion;

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada cliente para el módulo Priorización
// REGLA RN-020: Este servicio solo debe ser inyectado y utilizado en componentes con rol Administrador.
public interface IPriorizacionClienteService
{
    // RF-016: Consultar indicador y trazabilidad de un usuario (Administrador)
    Task<EstadoDeCarga<IndicadorAdministradorDto>> ConsultarIndicadorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    // RF-018: Consultar configuración del sistema de priorización (Administrador)
    Task<EstadoDeCarga<ConfiguracionPriorizacionDto>> ObtenerConfiguracionAsync(CancellationToken ct = default);

    // RF-018: Modificar parámetros del sistema de priorización (Administrador)
    Task<ResultadoOperacion> ActualizarConfiguracionAsync(ActualizarConfiguracionRequest request, CancellationToken ct = default);
}
