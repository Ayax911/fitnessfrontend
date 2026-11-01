using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Auditoria;

// Fachada para RF-032 (Auditoría administrativa)
public class AuditoriaClienteService : IAuditoriaClienteService
{
    private static readonly List<RegistroAuditoriaDto> _almacenAuditoria = new()
    {
        new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-10), "Admin Sistema", "Administrador", "CancelarSesion", "Sesion", "Cancelada sesión de Spinning por mantenimiento. 8 reservas afectadas sin penalización."),
        new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), "Admin Sistema", "Administrador", "CrearBloqueo", "Calendario", "Creado bloqueo Festivo Nacional del 02/11/2026 al 02/11/2026."),
        new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-3), "Carlos Entrenador", "Entrenador", "AsistenciaManual", "Asistencia", "Marcada asistencia presencial manual al estudiante Juan Gomez."),
        new(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-5), "Admin Sistema", "Administrador", "AsignarRol", "Usuario", "Asignado rol Entrenador a pedro.entrenador@itm.edu.co.")
    };

    public Task<EstadoDeCarga<PaginaAuditoriaDto>> ConsultarAuditoriaAsync(
        DateTimeOffset? desde = null,
        DateTimeOffset? hasta = null,
        string? tipoAccion = null,
        string? actor = null,
        int pagina = 1,
        int tamano = 25,
        CancellationToken ct = default)
    {
        var query = _almacenAuditoria.AsEnumerable();

        if (desde.HasValue)
        {
            query = query.Where(a => a.FechaHoraUtc >= desde.Value);
        }
        if (hasta.HasValue)
        {
            query = query.Where(a => a.FechaHoraUtc <= hasta.Value);
        }
        if (!string.IsNullOrWhiteSpace(tipoAccion))
        {
            query = query.Where(a => a.Accion.Equals(tipoAccion, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(actor))
        {
            query = query.Where(a => a.ActorNombre.Contains(actor, StringComparison.OrdinalIgnoreCase) ||
                                     a.ActorRol.Contains(actor, StringComparison.OrdinalIgnoreCase));
        }

        var ordenados = query.OrderByDescending(a => a.FechaHoraUtc).ToList();
        var total = ordenados.Count;
        var paginados = ordenados.Skip((pagina - 1) * tamano).Take(tamano).ToList();
        var totalPaginas = (int)Math.Ceiling((double)total / tamano);

        var resultado = new PaginaAuditoriaDto(paginados, total, pagina, Math.Max(1, totalPaginas));
        return Task.FromResult(EstadoDeCarga<PaginaAuditoriaDto>.Correcto(resultado));
    }
}
