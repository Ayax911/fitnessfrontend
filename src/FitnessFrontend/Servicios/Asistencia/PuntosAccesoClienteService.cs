using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Asistencia;

// RF-024 — Fachada cliente (Patrón 1)
public class PuntosAccesoClienteService : IPuntosAccesoClienteService
{
    private readonly FitnessApiClient _apiClient;

    public PuntosAccesoClienteService(FitnessApiClient apiClient) => _apiClient = apiClient;

    public async Task<EstadoDeCarga<List<PuntoAccesoDto>>> ListarAsync(CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Asistencia.PuntosAcceso.GetAsync(cancellationToken: ct);
            return EstadoDeCarga<List<PuntoAccesoDto>>.Correcto(respuesta ?? new List<PuntoAccesoDto>());
        }
        catch (Exception ex)
        {
            var (mensaje, codigo) = ErroresApi.Traducir(ex, "obtener los puntos de acceso");
            return EstadoDeCarga<List<PuntoAccesoDto>>.Fallo(mensaje, codigo);
        }
    }

    public async Task<EstadoDeCarga<PuntoAccesoDto>> CrearAsync(string nombre, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Asistencia.PuntosAcceso.PostAsync(new CrearPuntoAccesoRequest { Nombre = nombre }, cancellationToken: ct);
            return respuesta == null
                ? EstadoDeCarga<PuntoAccesoDto>.Fallo("No se recibió respuesta del servidor.")
                : EstadoDeCarga<PuntoAccesoDto>.Correcto(respuesta);
        }
        catch (Exception ex)
        {
            var (mensaje, codigo) = ErroresApi.Traducir(ex, "crear el punto de acceso");
            return EstadoDeCarga<PuntoAccesoDto>.Fallo(mensaje, codigo);
        }
    }

    public async Task<EstadoDeCarga<PuntoAccesoDto>> ActualizarAsync(Guid id, string nombre, bool activo, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Asistencia.PuntosAcceso[id].PutAsync(
                new ActualizarPuntoAccesoRequest { Nombre = nombre, Activo = activo }, cancellationToken: ct);
            return respuesta == null
                ? EstadoDeCarga<PuntoAccesoDto>.Fallo("No se recibió respuesta del servidor.")
                : EstadoDeCarga<PuntoAccesoDto>.Correcto(respuesta);
        }
        catch (Exception ex)
        {
            var (mensaje, codigo) = ErroresApi.Traducir(ex, "actualizar el punto de acceso");
            return EstadoDeCarga<PuntoAccesoDto>.Fallo(mensaje, codigo);
        }
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> _nombresCache = new();

    public async Task<EstadoDeCarga<QrPuntoAccesoDto>> ObtenerQrActualAsync(Guid puntoAccesoId, CancellationToken ct = default)
    {
        try
        {
            var qr = await _apiClient.Api.Asistencia.PuntosAcceso[puntoAccesoId].QrActual.GetAsync(cancellationToken: ct);
            if (qr == null || string.IsNullOrWhiteSpace(qr.Token))
            {
                return EstadoDeCarga<QrPuntoAccesoDto>.Fallo("El servidor no devolvió un código QR vigente.");
            }

            if (!_nombresCache.TryGetValue(puntoAccesoId, out var nombre))
            {
                try
                {
                    var lista = await ListarAsync(ct);
                    if (lista.EsExito && lista.Valor != null)
                    {
                        foreach (var p in lista.Valor)
                        {
                            if (p.Id.HasValue && !string.IsNullOrWhiteSpace(p.Nombre))
                            {
                                _nombresCache[p.Id.Value] = p.Nombre;
                            }
                        }
                    }
                }
                catch
                {
                    // No bloquear la visualización si la consulta de nombres falla
                }

                _nombresCache.TryGetValue(puntoAccesoId, out nombre);
            }

            var periodo = qr.PeriodoSegundos ?? 15;
            var validoHasta = qr.ValidoHastaUtc ?? DateTimeOffset.UtcNow.AddSeconds(periodo);

            return EstadoDeCarga<QrPuntoAccesoDto>.Correcto(
                new QrPuntoAccesoDto(qr.Token, validoHasta, periodo, nombre ?? "Punto de Acceso"));
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<QrPuntoAccesoDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible obtener el código QR actual.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<QrPuntoAccesoDto>.Fallo(
                $"Error del servidor al obtener QR ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<QrPuntoAccesoDto>.Fallo(
                $"Error de conexión al obtener el código QR: {ex.Message}",
                "ErrorConexion");
        }
    }
}
