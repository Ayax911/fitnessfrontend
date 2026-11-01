using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Eventos;

// Fachada para RF-029 (Anuncios) y RF-034 (Notas e incidencias)
public class NotasYAnunciosClienteService : INotasYAnunciosClienteService
{
    private static readonly ConcurrentDictionary<Guid, List<NotaIncidenciaDto>> _notasPorSesion = new();
    private static readonly ConcurrentDictionary<Guid, List<AnuncioSesionDto>> _anunciosPorSesion = new();

    public Task<EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>> ObtenerNotasAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        var lista = _notasPorSesion.GetOrAdd(sesionId, id => new List<NotaIncidenciaDto>());
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Correcto(
            lista.OrderByDescending(n => n.CreadaEn).ToList()));
    }

    public Task<ResultadoOperacion> CrearNotaAsync(
        Guid sesionId,
        string tipo,
        string contenido,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El contenido no puede estar vacío."));
        }

        if (contenido.Length > 1000)
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El contenido no puede superar los 1000 caracteres."));
        }

        var lista = _notasPorSesion.GetOrAdd(sesionId, id => new List<NotaIncidenciaDto>());
        lista.Add(new NotaIncidenciaDto(
            Guid.NewGuid(),
            sesionId,
            tipo,
            contenido,
            DateTimeOffset.UtcNow,
            "Entrenador Responsable"));

        return Task.FromResult(ResultadoOperacion.Exito("Nota guardada correctamente."));
    }

    public Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosPorSesionAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        var lista = _anunciosPorSesion.GetOrAdd(sesionId, id => new List<AnuncioSesionDto>());
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(
            lista.OrderByDescending(a => a.FechaPublicacion).ToList()));
    }

    public Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosMisReservasAsync(
        CancellationToken ct = default)
    {
        var todos = _anunciosPorSesion.Values
            .SelectMany(x => x)
            .OrderByDescending(a => a.FechaPublicacion)
            .ToList();

        return Task.FromResult(EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(todos));
    }

    public Task<ResultadoOperacion> PublicarAnuncioAsync(
        Guid sesionId,
        string mensaje,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El mensaje no puede estar vacío."));
        }

        if (mensaje.Length > 500)
        {
            return Task.FromResult(ResultadoOperacion.Fallo("El mensaje no puede superar los 500 caracteres."));
        }

        var lista = _anunciosPorSesion.GetOrAdd(sesionId, id => new List<AnuncioSesionDto>());
        lista.Add(new AnuncioSesionDto(
            Guid.NewGuid(),
            sesionId,
            mensaje,
            DateTimeOffset.UtcNow,
            "Entrenador Responsable"));

        return Task.FromResult(ResultadoOperacion.Exito("Anuncio publicado para los inscritos."));
    }
}
