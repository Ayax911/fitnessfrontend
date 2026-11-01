using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Usuarios;

// Patrón 1: Fachada cliente para el módulo Usuarios (RF-023). Uso exclusivo del rol Administrador.
// RN-020: ninguno de estos tipos contiene nivel de priorización ni indicador de confiabilidad.
public interface IUsuariosClienteService
{
    // RF-023: búsqueda paginada por nombre o correo, con filtro opcional por rol
    Task<EstadoDeCarga<PaginaUsuariosDto>> BuscarAsync(string? busqueda, string? rol, int pagina, int tamano, CancellationToken ct = default);

    // RF-023: entrenadores para el selector de eventos (RF-007, RF-009)
    Task<EstadoDeCarga<List<EntrenadorDto>>> ListarEntrenadoresAsync(CancellationToken ct = default);

    // RF-023: fija los roles de un usuario; el mensaje de rechazo (último administrador, etc.) es el del backend
    Task<ResultadoOperacion> ActualizarRolesAsync(Guid usuarioId, IEnumerable<string> roles, CancellationToken ct = default);
}
