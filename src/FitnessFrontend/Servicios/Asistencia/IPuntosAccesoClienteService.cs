using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;

namespace FitnessFrontend.Servicios.Asistencia;

public record QrPuntoAccesoDto(
    string Token,
    DateTimeOffset ValidoHasta,
    int PeriodoSegundos,
    string PuntoAccesoNombre);

// Patrón 1: Fachada cliente para la gestión de puntos de acceso (RF-024, RN-005, RF-039). Exclusivo Administrador.
public interface IPuntosAccesoClienteService
{
    Task<EstadoDeCarga<List<PuntoAccesoDto>>> ListarAsync(CancellationToken ct = default);
    Task<EstadoDeCarga<PuntoAccesoDto>> CrearAsync(string nombre, CancellationToken ct = default);
    Task<EstadoDeCarga<PuntoAccesoDto>> ActualizarAsync(Guid id, string nombre, bool activo, CancellationToken ct = default);
    Task<EstadoDeCarga<QrPuntoAccesoDto>> ObtenerQrActualAsync(Guid puntoAccesoId, CancellationToken ct = default);
}
