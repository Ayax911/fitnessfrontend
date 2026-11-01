using System;
using FitnessFrontend.ApiCliente.Models;
using Microsoft.Kiota.Abstractions;

namespace FitnessFrontend.Servicios.Infraestructura;

// Patrón 2 (docs/arquitectura/04-patrones-de-diseno.md): traduce las excepciones del cliente Kiota a un
// mensaje y un código para la UI, usando SIEMPRE el mensaje del backend cuando existe (regla 1 del AGENTS:
// la UI no reinterpreta reglas de negocio).
public static class ErroresApi
{
    public static (string Mensaje, string? Codigo) Traducir(Exception ex, string accion) => ex switch
    {
        ErrorDetalleDto e => (e.Mensaje ?? $"No fue posible {accion}.", e.CodigoError),
        ApiException { ResponseStatusCode: 401 } => ("Su sesión expiró. Inicie sesión de nuevo.", "401"),
        ApiException { ResponseStatusCode: 403 } => ($"Acceso denegado: no tiene permisos para {accion}.", "403"),
        ApiException a => ($"Error del servidor al {accion} ({a.ResponseStatusCode}).", a.ResponseStatusCode.ToString()),
        _ => ($"Error de conexión al {accion}: {ex.Message}", "ErrorConexion")
    };
}
