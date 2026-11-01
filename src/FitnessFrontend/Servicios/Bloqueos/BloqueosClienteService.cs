using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Bloqueos;

// RF-031: Implementación de fachada para bloqueos de calendario
public class BloqueosClienteService : IBloqueosClienteService
{
    private static readonly List<BloqueoCalendarioDto> BloqueosMemoria = new();

    public Task<EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>> ObtenerBloqueosAsync(
        DateTimeOffset? desde = null,
        DateTimeOffset? hasta = null,
        CancellationToken ct = default)
    {
        lock (BloqueosMemoria)
        {
            var consulta = BloqueosMemoria.AsEnumerable();
            if (desde.HasValue)
            {
                consulta = consulta.Where(b => b.Hasta >= desde.Value);
            }
            if (hasta.HasValue)
            {
                consulta = consulta.Where(b => b.Desde <= hasta.Value);
            }

            IReadOnlyList<BloqueoCalendarioDto> lista = consulta.OrderBy(b => b.Desde).ToList();
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<BloqueoCalendarioDto>>.Correcto(lista));
        }
    }

    public Task<ResultadoOperacion> CrearBloqueoAsync(CrearBloqueoRequest request, CancellationToken ct = default)
    {
        if (request.Hasta <= request.Desde)
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El rango de fechas es inválido. La fecha final debe ser posterior a la fecha inicial.", "RangoInvalido"));
        }

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El motivo del bloqueo es obligatorio.", "MotivoRequerido"));
        }

        var nuevo = new BloqueoCalendarioDto(
            Guid.NewGuid(),
            request.Desde,
            request.Hasta,
            request.Motivo,
            request.Tipo);

        lock (BloqueosMemoria)
        {
            BloqueosMemoria.Add(nuevo);
        }

        return Task.FromResult(ResultadoOperacion.Exito());
    }

    public Task<ResultadoOperacion> EliminarBloqueoAsync(Guid bloqueoId, CancellationToken ct = default)
    {
        lock (BloqueosMemoria)
        {
            BloqueosMemoria.RemoveAll(b => b.Id == bloqueoId);
        }

        return Task.FromResult(ResultadoOperacion.Exito());
    }

    // Auxiliar para pruebas o siembra inicial de bloqueos
    public static void SembrarBloqueo(BloqueoCalendarioDto bloqueo)
    {
        lock (BloqueosMemoria)
        {
            BloqueosMemoria.Add(bloqueo);
        }
    }

    public static void LimpiarBloqueos()
    {
        lock (BloqueosMemoria)
        {
            BloqueosMemoria.Clear();
        }
    }
}
