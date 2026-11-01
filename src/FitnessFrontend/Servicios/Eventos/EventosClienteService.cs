using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Eventos;

// Patrón 1 (docs/arquitectura/04-patrones-de-diseno.md): Fachada de cliente HTTP para Eventos
public class EventosClienteService : IEventosClienteService
{
    private readonly FitnessApiClient _apiClient;

    public EventosClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // RF-001: Listar eventos disponibles
    public async Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Eventos.GetAsync(cancellationToken: ct);
            var lista = respuesta ?? new List<EventoResumenDto>();
            return EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(lista);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible cargar los eventos.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Fallo(
                $"No fue posible cargar los eventos ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Fallo(
                $"Error de red al consultar eventos: {ex.Message}",
                "ErrorRed");
        }
    }

    // RF-035: Programación por rango de fechas y paginación
    public async Task<EstadoDeCarga<IReadOnlyList<EventoResumenDto>>> ObtenerEventosDisponiblesAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int? pagina = null,
        int? tamano = null,
        CancellationToken ct = default)
    {
        var resultadoBase = await ObtenerEventosDisponiblesAsync(ct);
        if (!resultadoBase.EsExito || resultadoBase.Valor == null)
        {
            return resultadoBase;
        }

        var eventos = resultadoBase.Valor.AsEnumerable();

        if (desde.HasValue)
        {
            eventos = eventos.Where(e => (e.Sesiones ?? new List<SesionResumenDto>())
                .Any(s => (s.FechaHoraFin ?? s.FechaHoraInicio ?? DateTimeOffset.MinValue) >= desde.Value));
        }

        if (hasta.HasValue)
        {
            eventos = eventos.Where(e => (e.Sesiones ?? new List<SesionResumenDto>())
                .Any(s => (s.FechaHoraInicio ?? DateTimeOffset.MaxValue) <= hasta.Value));
        }

        if (pagina.HasValue && tamano.HasValue && tamano.Value > 0)
        {
            eventos = eventos.Skip((pagina.Value - 1) * tamano.Value).Take(tamano.Value);
        }

        return EstadoDeCarga<IReadOnlyList<EventoResumenDto>>.Correcto(eventos.ToList());
    }

    // RF-010: Consultar actividades asignadas al entrenador autenticado (RN-007)
    public async Task<EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>> ObtenerMisActividadesAsync(CancellationToken ct = default)
    {
        try
        {
            var respuesta = await _apiClient.Api.Eventos.MisActividades.GetAsync(cancellationToken: ct);
            var lista = respuesta ?? new List<ActividadAsignadaDto>();
            return EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Correcto(lista);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible cargar sus actividades asignadas.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Fallo(
                $"No fue posible cargar sus actividades asignadas ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ActividadAsignadaDto>>.Fallo(
                $"Error de red al consultar actividades: {ex.Message}",
                "ErrorRed");
        }
    }

    // RF-007, RF-008: Crear un nuevo evento (Administrador)
    public async Task<EstadoDeCarga<Guid>> CrearEventoAsync(CrearEventoRequest request, CancellationToken ct = default)
    {
        try
        {
            var idCreado = await _apiClient.Api.Eventos.PostAsync(request, cancellationToken: ct);
            if (!idCreado.HasValue)
            {
                return EstadoDeCarga<Guid>.Fallo("El servidor no devolvió el identificador del evento creado.");
            }

            return EstadoDeCarga<Guid>.Correcto(idCreado.Value);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<Guid>.Fallo(
                errorEx.Mensaje ?? "No fue posible crear el evento.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<Guid>.Fallo(
                $"Error del servidor al crear evento ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<Guid>.Fallo(
                $"Error de red al crear evento: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-009: Actualizar evento (Administrador)
    public async Task<ResultadoOperacion> ActualizarEventoAsync(Guid id, ActualizarEventoRequest request, CancellationToken ct = default)
    {
        try
        {
            await _apiClient.Api.Eventos[id].PutAsync(request, cancellationToken: ct);
            return ResultadoOperacion.Exito("Evento actualizado correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible actualizar el evento.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error al actualizar evento ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al actualizar evento: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-009: Desactivar o eliminar evento (Administrador)
    public async Task<ResultadoOperacion> DesactivarOEliminarEventoAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _apiClient.Api.Eventos[id].DeleteAsync(cancellationToken: ct);
            return ResultadoOperacion.Exito("Evento desactivado o eliminado correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible eliminar el evento.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error al eliminar evento ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al eliminar evento: {ex.Message}",
                "ErrorConexion");
        }
    }

    // RF-008: Patrones de periodicidad provistos como datos extensibles desde backend (Bloque 2)
    public Task<IReadOnlyList<OpcionPatronRecurrencia>> ObtenerPatronesRecurrenciaSoportadosAsync(CancellationToken ct = default)
    {
        // En lugar de una lista fija en el marcado Razor, los patrones soportados
        // provienen de los tipos que el backend implementa (TipoPeriodicidad.DiasSemana = 0).
        var patrones = new List<OpcionPatronRecurrencia>
        {
            new(0, "Días de la semana", "Sesiones recurrentes en días específicos cada semana")
        };

        return Task.FromResult<IReadOnlyList<OpcionPatronRecurrencia>>(patrones);
    }
}
