using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Reportes;

// RF-033: Cliente para reportes administrativos y exportación CSV conectado a la API
public class ReportesClienteService : IReportesClienteService
{
    private readonly FitnessApiClient _apiClient;
    private readonly IZonaHorariaService _zonaHoraria;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportesClienteService(FitnessApiClient apiClient, IZonaHorariaService zonaHoraria)
    {
        _apiClient = apiClient;
        _zonaHoraria = zonaHoraria;
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>> ConsultarAsistenciaPorSesionAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        try
        {
            var stream = await _apiClient.Api.Reportes.AsistenciaPorSesion.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct);

            if (stream == null)
            {
                return EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Correcto(new List<ReporteAsistenciaSesionDto>());
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Correcto(new List<ReporteAsistenciaSesionDto>());
            }

            var dtos = JsonSerializer.Deserialize<List<ReporteAsistenciaSesionBackend>>(json, _jsonOptions) ?? new();
            var resultado = dtos.Select(d => new ReporteAsistenciaSesionDto(
                d.SesionId,
                d.EventoNombre ?? "Actividad",
                d.FechaHoraInicio,
                d.Cupo,
                d.Inscritos,
                d.Asistentes,
                d.Inasistentes,
                d.PorcentajeAsistencia
            )).ToList();

            return EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Correcto(resultado);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar el reporte de asistencia.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Fallo(
                $"Error del servidor al consultar reporte ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Fallo(
                $"Error de conexión al consultar reporte: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>> ConsultarOcupacionAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        try
        {
            var stream = await _apiClient.Api.Reportes.Ocupacion.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct);

            if (stream == null)
            {
                return EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Correcto(new List<ReporteOcupacionDto>());
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Correcto(new List<ReporteOcupacionDto>());
            }

            var dtos = JsonSerializer.Deserialize<List<ReporteOcupacionBackend>>(json, _jsonOptions) ?? new();
            var resultado = dtos.Select(d => new ReporteOcupacionDto(
                d.EventoNombre ?? "Actividad",
                d.Fecha.ToDateTime(TimeOnly.MinValue),
                d.CupoOfrecido,
                d.CupoOcupado,
                d.PorcentajeOcupacion
            )).ToList();

            return EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Correcto(resultado);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar el reporte de ocupación.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Fallo(
                $"Error del servidor al consultar reporte ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Fallo(
                $"Error de conexión al consultar reporte: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>> ConsultarInasistenciasAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        try
        {
            var stream = await _apiClient.Api.Reportes.InasistenciasPorEvento.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct);

            if (stream == null)
            {
                return EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Correcto(new List<ReporteInasistenciasEventoDto>());
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Correcto(new List<ReporteInasistenciasEventoDto>());
            }

            var dtos = JsonSerializer.Deserialize<List<ReporteInasistenciasEventoBackend>>(json, _jsonOptions) ?? new();
            var resultado = dtos.Select(d => new ReporteInasistenciasEventoDto(
                d.EventoNombre ?? "Actividad",
                d.TotalInscripciones,
                d.TotalInasistencias,
                d.PorcentajeInasistencias
            )).ToList();

            return EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Correcto(resultado);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar el reporte de inasistencias.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Fallo(
                $"Error del servidor al consultar reporte ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Fallo(
                $"Error de conexión al consultar reporte: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>> ConsultarHorasPicoAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        try
        {
            var stream = await _apiClient.Api.Reportes.HorasPico.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct);

            if (stream == null)
            {
                return EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Correcto(new List<ReporteHorasPicoDto>());
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Correcto(new List<ReporteHorasPicoDto>());
            }

            var dtos = JsonSerializer.Deserialize<List<ReporteHorasPicoBackend>>(json, _jsonOptions) ?? new();
            var resultado = dtos.Select(d => new ReporteHorasPicoDto(
                d.DiaSemanaNombre ?? $"Día {d.DiaSemana}",
                d.FranjaHoraria,
                d.TotalIngresos
            )).ToList();

            return EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Correcto(resultado);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar el reporte de horas pico.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Fallo(
                $"Error del servidor al consultar reporte ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Fallo(
                $"Error de conexión al consultar reporte: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<ReporteUsoPlataformaDto>> ConsultarUsoPlataformaAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        try
        {
            var stream = await _apiClient.Api.Reportes.UsoPlataforma.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct);

            if (stream == null)
            {
                return EstadoDeCarga<ReporteUsoPlataformaDto>.Correcto(new ReporteUsoPlataformaDto(0, 0, 0, 0));
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return EstadoDeCarga<ReporteUsoPlataformaDto>.Correcto(new ReporteUsoPlataformaDto(0, 0, 0, 0));
            }

            // El backend devuelve UNA FILA POR DÍA (lista), no un objeto: se suman los totales y se conserva el detalle diario
            var filas = JsonSerializer.Deserialize<List<ReporteUsoPlataformaBackend>>(json, _jsonOptions) ?? new();
            var diario = filas.OrderBy(f => f.Fecha)
                .Select(f => new UsoPlataformaDiarioDto(f.Fecha.ToDateTime(TimeOnly.MinValue), f.RegistrosNuevos, f.UsuariosActivos, f.ReservasCreadas, f.Cancelaciones))
                .ToList();

            var resultado = new ReporteUsoPlataformaDto(
                filas.Sum(f => f.RegistrosNuevos),
                filas.Count == 0 ? 0 : filas.Max(f => f.UsuariosActivos),
                filas.Sum(f => f.ReservasCreadas),
                filas.Sum(f => f.Cancelaciones),
                diario
            );

            return EstadoDeCarga<ReporteUsoPlataformaDto>.Correcto(resultado);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<ReporteUsoPlataformaDto>.Fallo(
                errorEx.Mensaje ?? "No fue posible consultar el reporte de uso de la plataforma.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<ReporteUsoPlataformaDto>.Fallo(
                $"Error del servidor al consultar reporte ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<ReporteUsoPlataformaDto>.Fallo(
                $"Error de conexión al consultar reporte: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteResumenMensualDto>>> ConsultarResumenMensualAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        return await LeerListaAsync<ReporteResumenMensualBackend, ReporteResumenMensualDto>(
            () => _apiClient.Api.Reportes.ResumenMensual.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct),
            d => new ReporteResumenMensualDto(
                d.Mes.ToDateTime(TimeOnly.MinValue),
                d.SesionesProgramadas,
                d.SesionesCanceladas,
                d.ReservasCreadas,
                d.Cancelaciones,
                d.AsistenciasSesion,
                d.IngresosGenerales,
                d.UsuariosActivos,
                d.RegistrosNuevos),
            "el resumen mensual",
            ct);
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteUsoPantallasDto>>> ConsultarUsoPantallasAsync(
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        return await LeerListaAsync<ReporteUsoPantallasBackend, ReporteUsoPantallasDto>(
            () => _apiClient.Api.Reportes.UsoPantallas.GetAsync(q =>
            {
                q.QueryParameters.Desde = desde;
                q.QueryParameters.Hasta = hasta;
            }, ct),
            d => new ReporteUsoPantallasDto(d.Pantalla ?? string.Empty, d.Dispositivo ?? string.Empty, d.Rol ?? string.Empty, d.Vistas),
            "el uso de pantallas",
            ct);
    }

    // Lectura común de un reporte que el backend devuelve como lista JSON dentro de un Stream
    private static async Task<EstadoDeCarga<IReadOnlyList<TDestino>>> LeerListaAsync<TOrigen, TDestino>(
        Func<Task<Stream?>> consultar,
        Func<TOrigen, TDestino> mapear,
        string descripcion,
        CancellationToken ct)
    {
        try
        {
            var stream = await consultar();
            if (stream == null)
            {
                return EstadoDeCarga<IReadOnlyList<TDestino>>.Correcto(new List<TDestino>());
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return EstadoDeCarga<IReadOnlyList<TDestino>>.Correcto(new List<TDestino>());
            }

            var filas = JsonSerializer.Deserialize<List<TOrigen>>(json, _jsonOptions) ?? new();
            return EstadoDeCarga<IReadOnlyList<TDestino>>.Correcto(filas.Select(mapear).ToList());
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<IReadOnlyList<TDestino>>.Fallo(
                errorEx.Mensaje ?? $"No fue posible consultar {descripcion}.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<IReadOnlyList<TDestino>>.Fallo(
                $"Error del servidor al consultar reporte ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<IReadOnlyList<TDestino>>.Fallo(
                $"Error de conexión al consultar reporte: {ex.Message}",
                "ErrorConexion");
        }
    }

    public async Task<EstadoDeCarga<string>> DescargarCsvAsync(
        string reporte,
        DateTimeOffset desde,
        DateTimeOffset hasta,
        CancellationToken ct = default)
    {
        try
        {
            Stream? stream = reporte.ToLowerInvariant() switch
            {
                "asistencia" => await _apiClient.Api.Reportes.AsistenciaPorSesion.GetAsync(q =>
                {
                    q.QueryParameters.Desde = desde;
                    q.QueryParameters.Hasta = hasta;
                    q.QueryParameters.Formato = "csv";
                }, ct),
                "ocupacion" => await _apiClient.Api.Reportes.Ocupacion.GetAsync(q =>
                {
                    q.QueryParameters.Desde = desde;
                    q.QueryParameters.Hasta = hasta;
                    q.QueryParameters.Formato = "csv";
                }, ct),
                "inasistencias" => await _apiClient.Api.Reportes.InasistenciasPorEvento.GetAsync(q =>
                {
                    q.QueryParameters.Desde = desde;
                    q.QueryParameters.Hasta = hasta;
                    q.QueryParameters.Formato = "csv";
                }, ct),
                "horaspico" => await _apiClient.Api.Reportes.HorasPico.GetAsync(q =>
                {
                    q.QueryParameters.Desde = desde;
                    q.QueryParameters.Hasta = hasta;
                    q.QueryParameters.Formato = "csv";
                }, ct),
                "uso" => await _apiClient.Api.Reportes.UsoPlataforma.GetAsync(q =>
                {
                    q.QueryParameters.Desde = desde;
                    q.QueryParameters.Hasta = hasta;
                    q.QueryParameters.Formato = "csv";
                }, ct),
                "mensual" => await _apiClient.Api.Reportes.ResumenMensual.GetAsync(q =>
                {
                    q.QueryParameters.Desde = desde;
                    q.QueryParameters.Hasta = hasta;
                    q.QueryParameters.Formato = "csv";
                }, ct),
                "pantallas" => await _apiClient.Api.Reportes.UsoPantallas.GetAsync(q =>
                {
                    q.QueryParameters.Desde = desde;
                    q.QueryParameters.Hasta = hasta;
                    q.QueryParameters.Formato = "csv";
                }, ct),
                _ => throw new ArgumentException($"Reporte no reconocido: {reporte}")
            };

            if (stream == null)
            {
                return EstadoDeCarga<string>.Fallo("El servidor no devolvió contenido CSV.");
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var csv = await reader.ReadToEndAsync(ct);
            return EstadoDeCarga<string>.Correcto(csv);
        }
        catch (ErrorDetalleDto errorEx)
        {
            return EstadoDeCarga<string>.Fallo(
                errorEx.Mensaje ?? "No fue posible descargar el archivo CSV.",
                errorEx.CodigoError);
        }
        catch (ApiException apiEx)
        {
            return EstadoDeCarga<string>.Fallo(
                $"Error del servidor al exportar CSV ({apiEx.ResponseStatusCode}).",
                apiEx.ResponseStatusCode.ToString());
        }
        catch (Exception ex)
        {
            return EstadoDeCarga<string>.Fallo(
                $"Error de conexión al exportar CSV: {ex.Message}",
                "ErrorConexion");
        }
    }

    private sealed class ReporteAsistenciaSesionBackend
    {
        public Guid SesionId { get; set; }
        public string? EventoNombre { get; set; }
        public DateTimeOffset FechaHoraInicio { get; set; }
        public DateTimeOffset FechaHoraFin { get; set; }
        public int Cupo { get; set; }
        public int Inscritos { get; set; }
        public int Asistentes { get; set; }
        public int Inasistentes { get; set; }
        public double PorcentajeAsistencia { get; set; }
    }

    private sealed class ReporteOcupacionBackend
    {
        public Guid EventoId { get; set; }
        public string? EventoNombre { get; set; }
        public DateOnly Fecha { get; set; }
        public int CupoOfrecido { get; set; }
        public int CupoOcupado { get; set; }
        public double PorcentajeOcupacion { get; set; }
    }

    private sealed class ReporteInasistenciasEventoBackend
    {
        public Guid EventoId { get; set; }
        public string? EventoNombre { get; set; }
        public int TotalSesiones { get; set; }
        public int TotalInscripciones { get; set; }
        public int TotalInasistencias { get; set; }
        public double PorcentajeInasistencias { get; set; }
    }

    private sealed class ReporteHorasPicoBackend
    {
        public int DiaSemana { get; set; }
        public string? DiaSemanaNombre { get; set; }
        public int FranjaHoraria { get; set; }
        public int TotalCheckInsGeneral { get; set; }
        public int TotalCheckInsSesion { get; set; }
        public int TotalIngresos { get; set; }
    }

    private sealed class ReporteUsoPlataformaBackend
    {
        public DateOnly Fecha { get; set; }
        public int RegistrosNuevos { get; set; }
        public int UsuariosActivos { get; set; }
        public int ReservasCreadas { get; set; }
        public int Cancelaciones { get; set; }
    }

    private sealed class ReporteResumenMensualBackend
    {
        public DateOnly Mes { get; set; }
        public int SesionesProgramadas { get; set; }
        public int SesionesCanceladas { get; set; }
        public int ReservasCreadas { get; set; }
        public int Cancelaciones { get; set; }
        public int AsistenciasSesion { get; set; }
        public int IngresosGenerales { get; set; }
        public int UsuariosActivos { get; set; }
        public int RegistrosNuevos { get; set; }
    }

    private sealed class ReporteUsoPantallasBackend
    {
        public string? Pantalla { get; set; }
        public string? Dispositivo { get; set; }
        public string? Rol { get; set; }
        public int Vistas { get; set; }
    }
}
