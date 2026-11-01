using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Priorizacion;

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada de cliente HTTP para Priorización
// Acceso exclusivo para rol Administrador (RN-020).
public class PriorizacionClienteService : IPriorizacionClienteService
{
    private readonly FitnessApiClient _apiClient;

    public PriorizacionClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // RF-016: Consultar indicador y trazabilidad de un usuario
    public async Task<EstadoDeCarga<IndicadorAdministradorDto>> ConsultarIndicadorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Priorizacion.Usuarios[usuarioId].GetAsync(cancellationToken: ct);
            if (respuesta == null)
            {
                return EstadoDeCarga<IndicadorAdministradorDto>.Fallo("No se encontró información del usuario solicitado.");
            }

            return EstadoDeCarga<IndicadorAdministradorDto>.Correcto(respuesta);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IndicadorAdministradorDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar el indicador del usuario.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            if (apiEx.ResponseStatusCode == 403)
            {
                return EstadoDeCarga<IndicadorAdministradorDto>.Fallo(
                    "Acceso denegado: Se requiere rol de Administrador para consultar indicadores de priorización.",
                    "403");
            }
            if (apiEx.ResponseStatusCode == 404)
            {
                return EstadoDeCarga<IndicadorAdministradorDto>.Fallo(
                    "Usuario no encontrado en el sistema de priorización.",
                    "404");
            }

            return EstadoDeCarga<IndicadorAdministradorDto>.Fallo(
                $"Error al consultar el indicador ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IndicadorAdministradorDto>.Fallo(
                $"Error de conexión al consultar indicador: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-018: Consultar configuración de priorización
    public async Task<EstadoDeCarga<ConfiguracionPriorizacionDto>> ObtenerConfiguracionAsync(CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Priorizacion.Configuracion.GetAsync(cancellationToken: ct);
            if (respuesta == null)
            {
                return EstadoDeCarga<ConfiguracionPriorizacionDto>.Fallo("No se encontró la configuración del sistema de priorización.");
            }

            return EstadoDeCarga<ConfiguracionPriorizacionDto>.Correcto(respuesta);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<ConfiguracionPriorizacionDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible obtener la configuración.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            if (apiEx.ResponseStatusCode == 403)
            {
                return EstadoDeCarga<ConfiguracionPriorizacionDto>.Fallo(
                    "Acceso denegado: Se requiere rol de Administrador para ver la configuración del sistema.",
                    "403");
            }

            return EstadoDeCarga<ConfiguracionPriorizacionDto>.Fallo(
                $"Error del servidor al obtener la configuración ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<ConfiguracionPriorizacionDto>.Fallo(
                $"Error de conexión al consultar configuración: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-018: Modificar parámetros de configuración de priorización
    public async Task<ResultadoOperacion> ActualizarConfiguracionAsync(ActualizarConfiguracionRequest request, CancellationToken ct = default)
    {
        try
        {
            await _apiClient.Api.Priorizacion.Configuracion.PutAsync(request, cancellationToken: ct);
            return ResultadoOperacion.Exito("Configuración actualizada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible actualizar la configuración.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            if (apiEx.ResponseStatusCode == 403)
            {
                return ResultadoOperacion.Fallo(
                    "Acceso denegado: Se requiere rol de Administrador para modificar la configuración.",
                    "403");
            }

            return ResultadoOperacion.Fallo(
                $"Error al actualizar configuración ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al guardar configuración: {ex.Message}",
                "ErrorConexion");
        }
    }
}
