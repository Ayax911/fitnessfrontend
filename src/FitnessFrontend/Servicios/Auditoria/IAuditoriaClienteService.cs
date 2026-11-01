using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Auditoria;

// RF-032: Registro de auditoría de solo anexar para acciones administrativas
// RN-035: Sin datos sensibles ni campos de priorización (RN-020)
public record RegistroAuditoriaDto(
    Guid Id,
    DateTimeOffset FechaHoraUtc,
    string ActorNombre,
    string ActorRol,
    string Accion,
    string Entidad,
    string Detalle
);

public record PaginaAuditoriaDto(
    IReadOnlyList<RegistroAuditoriaDto> Elementos,
    int TotalRegistros,
    int PaginaActual,
    int TotalPaginas
);

public interface IAuditoriaClienteService
{
    Task<EstadoDeCarga<PaginaAuditoriaDto>> ConsultarAuditoriaAsync(
        DateTimeOffset? desde = null,
        DateTimeOffset? hasta = null,
        string? tipoAccion = null,
        string? actor = null,
        int pagina = 1,
        int tamano = 25,
        CancellationToken ct = default);
}
