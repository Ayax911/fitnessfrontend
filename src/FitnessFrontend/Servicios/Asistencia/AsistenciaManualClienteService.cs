using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Asistencia;

// Fachada de servicio para RF-027 (Asistencia manual de entrenador)
// Mantiene estado en memoria mientras Kiota no incorpore el contrato generado del backend.
public class AsistenciaManualClienteService : IAsistenciaManualClienteService
{
    private static readonly ConcurrentDictionary<Guid, List<InscritoAsistenciaManualDto>> _almacenSesiones = new();

    public Task<EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>> ObtenerInscritosAsistenciaAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        var lista = _almacenSesiones.GetOrAdd(sesionId, id => new List<InscritoAsistenciaManualDto>());
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Correcto(lista.ToList()));
    }

    public Task<ResultadoOperacion> MarcarAsistenciaManualAsync(
        Guid sesionId,
        Guid reservaId,
        string? motivo = null,
        CancellationToken ct = default)
    {
        var lista = _almacenSesiones.GetOrAdd(sesionId, id => new List<InscritoAsistenciaManualDto>());
        var item = lista.FirstOrDefault(x => x.ReservaId == reservaId);
        if (item != null)
        {
            lista.Remove(item);
            lista.Add(item with { Asistio = true, FechaHoraMarcado = DateTimeOffset.UtcNow, Motivo = motivo });
        }
        else
        {
            lista.Add(new InscritoAsistenciaManualDto(
                reservaId,
                Guid.NewGuid(),
                "Usuario Registrado",
                "usuario@correo.itm.edu.co",
                true,
                DateTimeOffset.UtcNow,
                motivo));
        }

        return Task.FromResult(ResultadoOperacion.Exito("Asistencia marcada correctamente."));
    }

    public Task<ResultadoOperacion> DesmarcarAsistenciaManualAsync(
        Guid sesionId,
        Guid reservaId,
        string? motivo = null,
        CancellationToken ct = default)
    {
        var lista = _almacenSesiones.GetOrAdd(sesionId, id => new List<InscritoAsistenciaManualDto>());
        var item = lista.FirstOrDefault(x => x.ReservaId == reservaId);
        if (item != null)
        {
            lista.Remove(item);
            lista.Add(item with { Asistio = false, FechaHoraMarcado = null, Motivo = motivo });
        }

        return Task.FromResult(ResultadoOperacion.Exito("Asistencia desmarcada correctamente."));
    }
}
