using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Eventos;

public record NotaIncidenciaDto(
    Guid Id,
    Guid SesionId,
    string Tipo,
    string Contenido,
    DateTimeOffset CreadaEn,
    string CreadaPorNombre
);

public record AnuncioSesionDto(
    Guid Id,
    Guid SesionId,
    string Mensaje,
    DateTimeOffset FechaPublicacion,
    string PublicadoPorNombre
);

public interface INotasYAnunciosClienteService
{
    Task<EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>> ObtenerNotasAsync(
        Guid sesionId,
        CancellationToken ct = default);

    Task<ResultadoOperacion> CrearNotaAsync(
        Guid sesionId,
        string tipo,
        string contenido,
        CancellationToken ct = default);

    Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosPorSesionAsync(
        Guid sesionId,
        CancellationToken ct = default);

    Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosMisReservasAsync(
        CancellationToken ct = default);

    Task<ResultadoOperacion> PublicarAnuncioAsync(
        Guid sesionId,
        string mensaje,
        CancellationToken ct = default);
}
