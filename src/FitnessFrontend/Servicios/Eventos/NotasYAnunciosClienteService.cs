using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Eventos;

// RF-029 (Anuncios) y RF-034 (Notas e incidencias) conectados a la API
public class NotasYAnunciosClienteService : INotasYAnunciosClienteService
{
    private readonly FitnessApiClient _apiClient;

    public NotasYAnunciosClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>> ObtenerNotasAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        try
        {
            var notas = await _apiClient.Api.Eventos.Sesiones[sesionId].Notas.GetAsync(cancellationToken: ct);
            if (notas == null)
            {
                return EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Correcto(new List<NotaIncidenciaDto>());
            }

            var dtos = notas.Select(n => new NotaIncidenciaDto(
                n.Id ?? Guid.Empty,
                n.SesionEventoId ?? sesionId,
                n.Tipo == 1 ? "Incidencia" : "Nota",
                n.Texto ?? string.Empty,
                n.FechaHoraUtc ?? DateTimeOffset.UtcNow,
                n.AutorNombre ?? "Entrenador"
            )).OrderByDescending(n => n.CreadaEn).ToList();

            return EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Correcto(dtos);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar las notas de la sesión.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Fallo(
                $"Error del servidor al consultar notas ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Fallo(
                $"Error de conexión al consultar notas: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> CrearNotaAsync(
        Guid sesionId,
        string tipo,
        string contenido,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return ResultadoOperacion.Fallo("El contenido no puede estar vacío.", "ContenidoRequerido");
        }

        if (contenido.Length > 1000)
        {
            return ResultadoOperacion.Fallo("El contenido no puede superar los 1000 caracteres.", "LongitudExcedida");
        }

        try
        {
            var body = new CrearNotaSesionRequest
            {
                Texto = contenido,
                Tipo = string.Equals(tipo, "Incidencia", StringComparison.OrdinalIgnoreCase) ? 1 : 0
            };

            await _apiClient.Api.Eventos.Sesiones[sesionId].Notas.PostAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Nota guardada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible guardar la nota.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al guardar la nota ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al guardar la nota: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosPorSesionAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        try
        {
            var anuncios = await _apiClient.Api.Eventos.Sesiones[sesionId].Anuncios.GetAsync(cancellationToken: ct);
            if (anuncios == null)
            {
                return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(new List<AnuncioSesionDto>());
            }

            var dtos = anuncios.Select(a => new AnuncioSesionDto(
                a.Id ?? Guid.Empty,
                a.SesionEventoId ?? sesionId,
                a.Mensaje ?? string.Empty,
                a.FechaHoraUtc ?? DateTimeOffset.UtcNow,
                a.AutorNombre ?? "Organizador"
            )).OrderByDescending(a => a.FechaPublicacion).ToList();

            return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(dtos);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar los avisos de la sesión.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Fallo(
                $"Error del servidor al consultar avisos ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Fallo(
                $"Error de conexión al consultar avisos: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosMisReservasAsync(
        CancellationToken ct = default)
    {
        try
        {
            var reservas = await _apiClient.Api.Reservas.Mias.GetAsync(cancellationToken: ct);
            if (reservas == null || reservas.Count == 0)
            {
                return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(new List<AnuncioSesionDto>());
            }

            // H-30: Pedir anuncios únicamente para reservas Confirmada (Estado == 0) no canceladas
            var sesionIds = reservas
                .Where(r => r.SesionEventoId.HasValue && r.SesionCancelada != true && r.Estado == 0)
                .Select(r => r.SesionEventoId!.Value)
                .Distinct()
                .ToList();

            var todosLosAnuncios = new List<AnuncioSesionDto>();
            foreach (var sesionId in sesionIds)
            {
                var res = await ObtenerAnunciosPorSesionAsync(sesionId, ct);
                if (res.EsExito && res.Valor != null)
                {
                    todosLosAnuncios.AddRange(res.Valor);
                }
            }

            return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(
                todosLosAnuncios.OrderByDescending(a => a.FechaPublicacion).ToList());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Fallo(
                $"Error al cargar anuncios de reservas: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> PublicarAnuncioAsync(
        Guid sesionId,
        string mensaje,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return ResultadoOperacion.Fallo("El mensaje del aviso no puede estar vacío.", "MensajeRequerido");
        }

        try
        {
            var body = new CrearAnuncioRequest
            {
                Mensaje = mensaje
            };

            await _apiClient.Api.Eventos.Sesiones[sesionId].Anuncios.PostAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Aviso publicado para los inscritos en la sesión.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible publicar el aviso.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al publicar aviso ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al publicar aviso: {ex.Message}",
                "ErrorConexion");
        }
    }
}
