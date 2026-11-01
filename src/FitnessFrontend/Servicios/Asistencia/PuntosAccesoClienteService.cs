using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Infraestructura;

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
}
