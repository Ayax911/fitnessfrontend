using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Retroalimentacion;

public class RetroalimentacionClienteService : IRetroalimentacionClienteService
{
    private readonly IAutenticacionService _authService;
    private static readonly List<ReporteProblemaDto> _almacenReportes = new()
    {
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "estudiante@itm.edu.co",
            "Problema",
            "La pantalla del punto de acceso en el bloque F parpadeaba y no leía rápido el código.",
            "/ingreso",
            "Resuelto",
            DateTimeOffset.UtcNow.AddDays(-2),
            "Se reinició la pantalla del punto y se ajustó el brillo."),
        new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "estudiante@itm.edu.co",
            "Sugerencia",
            "Sería genial tener más sesiones de Spinning en la mañana los días viernes.",
            "/programacion",
            "Pendiente",
            DateTimeOffset.UtcNow.AddHours(-4)),
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "otro.usuario@itm.edu.co",
            "Pregunta",
            "¿Puedo asistir si llegué 10 minutos tarde a la clase?",
            "/mis-reservas",
            "EnRevisión",
            DateTimeOffset.UtcNow.AddDays(-1))
    };

    public RetroalimentacionClienteService(IAutenticacionService authService)
    {
        _authService = authService;
    }

    public async Task<EstadoDeCarga<ReporteProblemaDto>> EnviarReporteAsync(CrearReporteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Mensaje))
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo("El mensaje es obligatorio.");
        }

        if (dto.Mensaje.Length > 1000)
        {
            return EstadoDeCarga<ReporteProblemaDto>.Fallo("El mensaje no puede exceder 1000 caracteres.");
        }

        var usuario = await _authService.ObtenerUsuarioActualAsync();
        var usuarioId = usuario?.Id ?? Guid.NewGuid();
        var usuarioEmail = usuario?.Email ?? "anonimo@itm.edu.co";

        var nuevo = new ReporteProblemaDto(
            Guid.NewGuid(),
            usuarioId,
            usuarioEmail,
            dto.Tipo,
            dto.Mensaje,
            dto.RutaOrigen,
            "Pendiente",
            DateTimeOffset.UtcNow);

        _almacenReportes.Insert(0, nuevo);
        return EstadoDeCarga<ReporteProblemaDto>.Correcto(nuevo);
    }

    public async Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ObtenerMisReportesAsync(CancellationToken ct = default)
    {
        var usuario = await _authService.ObtenerUsuarioActualAsync();
        var usuarioId = usuario?.Id ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var misReportes = _almacenReportes
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.FechaCreacionUtc)
            .ToList();

        return EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(misReportes);
    }

    public Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ListarBandejaAdminAsync(string? estado = null, CancellationToken ct = default)
    {
        var query = _almacenReportes.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase));
        }

        var lista = query.OrderByDescending(r => r.FechaCreacionUtc).ToList();
        return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(lista));
    }

    public Task<EstadoDeCarga<ReporteProblemaDto>> CambiarEstadoAsync(Guid id, string nuevoEstado, string? respuesta = null, CancellationToken ct = default)
    {
        var indice = _almacenReportes.FindIndex(r => r.Id == id);
        if (indice < 0)
        {
            return Task.FromResult(EstadoDeCarga<ReporteProblemaDto>.Fallo("Reporte no encontrado.", "NoEncontrado"));
        }

        var actual = _almacenReportes[indice];
        var actualizado = actual with
        {
            Estado = nuevoEstado,
            RespuestaAdmin = respuesta ?? actual.RespuestaAdmin
        };

        _almacenReportes[indice] = actualizado;
        return Task.FromResult(EstadoDeCarga<ReporteProblemaDto>.Correcto(actualizado));
    }
}
