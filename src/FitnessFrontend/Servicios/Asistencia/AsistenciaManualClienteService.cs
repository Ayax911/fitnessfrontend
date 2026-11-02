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

namespace FitnessFrontend.Servicios.Asistencia;

// RF-027: Servicio de cliente para asistencia manual de entrenador conectado a la API
public class AsistenciaManualClienteService : IAsistenciaManualClienteService
{
    private readonly FitnessApiClient _apiClient;

    public AsistenciaManualClienteService(FitnessApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>> ObtenerInscritosAsistenciaAsync(
        Guid sesionId,
        CancellationToken ct = default)
    {
        try
        {
            var inscritos = await _apiClient.Api.Reservas.Sesiones[sesionId].Inscritos.GetAsync(cancellationToken: ct);
            if (inscritos == null)
            {
                return EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Correcto(new List<InscritoAsistenciaManualDto>());
            }

            var dtos = inscritos.Select(i =>
            {
                var usuarioId = i.UsuarioId ?? Guid.Empty;
                var asistio = i.Estado == 3; // 3: Asistida
                return new InscritoAsistenciaManualDto(
                    ReservaId: usuarioId, // Mapeado como identificador de acción para la pantalla
                    UsuarioId: usuarioId,
                    NombreCompleto: i.NombreCompleto ?? "Usuario Registrado",
                    Correo: string.Empty,
                    Asistio: asistio,
                    FechaHoraMarcado: null,
                    Motivo: null
                );
            }).ToList();

            return EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Correcto(dtos);
        }
        catch (ErrorDetalleDto errorEx)
        {
            var codigo = errorEx.CodigoError ?? "Error";
            return EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar la lista de inscritos.",
                codigo);
        }
        catch (ApiException apiEx)
        {
            var codigo = apiEx.ResponseStatusCode.ToString();
            return EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Fallo(
                $"Error del servidor al consultar lista ({apiEx.ResponseStatusCode}).",
                codigo);
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<InscritoAsistenciaManualDto>>.Fallo(
                $"Error de conexión al consultar lista de asistencia: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> MarcarAsistenciaManualAsync(
        Guid sesionId,
        Guid reservaId,
        string? motivo = null,
        CancellationToken ct = default)
    {
        try
        {
            var body = new MarcarAsistenciaManualRequest
            {
                UsuarioId = reservaId,
                Presente = true,
                Motivo = motivo
            };

            await _apiClient.Api.Asistencia.Sesiones[sesionId].Manual.PutAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Asistencia marcada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible marcar la asistencia manual.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al marcar asistencia ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al marcar asistencia: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<ResultadoOperacion> DesmarcarAsistenciaManualAsync(
        Guid sesionId,
        Guid reservaId,
        string? motivo = null,
        CancellationToken ct = default)
    {
        try
        {
            var body = new MarcarAsistenciaManualRequest
            {
                UsuarioId = reservaId,
                Presente = false,
                Motivo = motivo
            };

            await _apiClient.Api.Asistencia.Sesiones[sesionId].Manual.PutAsync(body, cancellationToken: ct);
            return ResultadoOperacion.Exito("Asistencia desmarcada correctamente.");
        }
        catch (ErrorDetalleDto errorEx)
        {
            return ResultadoOperacion.Fallo(
                errorEx.Mensaje ?? "No fue posible desmarcar la asistencia manual.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return ResultadoOperacion.Fallo(
                $"Error del servidor al desmarcar asistencia ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallo(
                $"Error de conexión al desmarcar asistencia: {ex.Message}",
                "ErrorConexion");
        }
    }
}
