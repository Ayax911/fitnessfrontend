using System;

namespace FitnessFrontend.Servicios.Asistencia;

// Patrón 6 (docs/arquitectura/04-patrones-de-diseno.md, docs/arquitectura/06-flujo-checkin-qr.md):
// Implementación pura sin dependencias de red ni de navegador.
public class FlujoCheckInService : IFlujoCheckInService
{
    public DecisionCheckIn EvaluarInicio(string? tokenSesion, bool estaAutenticado)
    {
        // Caso C previo al backend: El token de la ruta está vacío o en blanco
        if (string.IsNullOrWhiteSpace(tokenSesion))
        {
            return DecisionCheckIn.TokenInvalido("El token de sesión no es válido o está ausente.");
        }

        // Caso B: El estudiante no tiene sesión iniciada en el navegador
        // Redirige a Login con returnUrl = /checkin/{token} para volver automáticamente sin volver a escanear
        if (!estaAutenticado)
        {
            return DecisionCheckIn.RequiereAutenticacion(tokenSesion.Trim());
        }

        // Caso A: El estudiante ya tiene sesión iniciada en el navegador
        return DecisionCheckIn.ProcederCheckIn(tokenSesion.Trim());
    }
}
