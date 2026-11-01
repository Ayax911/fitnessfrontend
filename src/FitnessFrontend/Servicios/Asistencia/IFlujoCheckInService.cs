using System;

namespace FitnessFrontend.Servicios.Asistencia;

// Patrón 6 (docs/arquitectura/04-patrones-de-diseno.md, docs/arquitectura/06-flujo-checkin-qr.md):
// Lógica pura de decisión para el flujo de check-in por QR desacoplada de la UI.
public interface IFlujoCheckInService
{
    // Evalúa el estado del escaneo y devuelve la acción a tomar (Caso A, B o C)
    DecisionCheckIn EvaluarInicio(string? tokenSesion, bool estaAutenticado);
}

public enum TipoDecisionCheckIn
{
    CasoA_ProcederCheckIn,
    CasoB_RequiereAutenticacion,
    CasoC_TokenInvalido
}

public record DecisionCheckIn(
    TipoDecisionCheckIn Tipo,
    string? TokenSesion = null,
    string? ReturnUrl = null,
    string? MensajeError = null)
{
    public static DecisionCheckIn ProcederCheckIn(string token) =>
        new(TipoDecisionCheckIn.CasoA_ProcederCheckIn, TokenSesion: token);

    public static DecisionCheckIn RequiereAutenticacion(string token) =>
        new(TipoDecisionCheckIn.CasoB_RequiereAutenticacion, TokenSesion: token, ReturnUrl: $"/checkin/{Uri.EscapeDataString(token)}");

    public static DecisionCheckIn TokenInvalido(string mensaje = "El código QR escaneado es inválido o no contiene un identificador de sesión.") =>
        new(TipoDecisionCheckIn.CasoC_TokenInvalido, MensajeError: mensaje);
}
