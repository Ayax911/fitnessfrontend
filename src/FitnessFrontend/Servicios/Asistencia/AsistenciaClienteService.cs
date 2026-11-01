using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Asistencia;

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada de cliente HTTP para Asistencia
public class AsistenciaClienteService : IAsistenciaClienteService
{
    private readonly FitnessApiClient _apiClient;

    public AsistenciaClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // RF-005: Registrar check-in por código QR en sesión deportiva
    public async Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarCheckInAsync(string tokenSesion, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Asistencia.Checkin[tokenSesion].PostAsync(cancellationToken: ct);
            if (respuesta == null)
            {
                return EstadoDeCarga<CheckInResultadoDto>.Fallo("El servidor no devolvió información del check-in.");
            }

            return EstadoDeCarga<CheckInResultadoDto>.Correcto(respuesta);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<CheckInResultadoDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible registrar el check-in.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<CheckInResultadoDto>.Fallo(
                $"Error del servidor al registrar check-in ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<CheckInResultadoDto>.Fallo(
                $"Error de conexión al registrar check-in: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-006: Registrar ingreso general por torniquete (RN-030)
    public async Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarIngresoGeneralAsync(string idPuntoAcceso, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Asistencia.Ingreso[idPuntoAcceso].PostAsync(cancellationToken: ct);
            if (respuesta == null)
            {
                return EstadoDeCarga<CheckInResultadoDto>.Fallo("El servidor no devolvió información del ingreso.");
            }

            return EstadoDeCarga<CheckInResultadoDto>.Correcto(respuesta);
        }
        catch (ErrorDetalleDto errorEx)
        {
            // Respeta el rechazo por intervalo mínimo (RN-030) mostrando el mensaje del backend sin inventar cuenta regresiva
            return EstadoDeCarga<CheckInResultadoDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible registrar el ingreso general.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<CheckInResultadoDto>.Fallo(
                $"Error al registrar ingreso ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<CheckInResultadoDto>.Fallo(
                $"Error de conexión al registrar ingreso: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-021, RN-020: Consultar historial de asistencias (sin datos de priorización)
    public async Task<EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>> ObtenerHistorialAsync(CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Asistencia.MiHistorial.GetAsync(cancellationToken: ct);
            var lista = respuesta ?? new List<HistorialAsistenciaDto>();
            return EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>.Correcto(lista);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible cargar su historial de asistencia.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>.Fallo(
                $"Error al consultar historial ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>.Fallo(
                $"Error de red al consultar historial: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-012: Consultar QR dinámico de sesión (Entrenador)
    public async Task<EstadoDeCarga<SesionQRDto>> ObtenerQRDeSesionAsync(Guid sesionId, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Asistencia.Sesiones[sesionId].Qr.GetAsync(cancellationToken: ct);
            if (respuesta == null)
            {
                return EstadoDeCarga<SesionQRDto>.Fallo("El servidor no devolvió información del código QR.");
            }

            return EstadoDeCarga<SesionQRDto>.Correcto(respuesta);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<SesionQRDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible generar el código QR de la sesión.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<SesionQRDto>.Fallo(
                $"Error al obtener código QR ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<SesionQRDto>.Fallo(
                $"Error de conexión al obtener código QR: {ex.Message}",
                "ErrorConexion");
        }
    }
}
