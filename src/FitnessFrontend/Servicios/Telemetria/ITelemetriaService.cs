using System.Threading;
using System.Threading.Tasks;

namespace FitnessFrontend.Servicios.Telemetria;

// RF-033 (analítica de uso): registro anónimo de las pantallas que se visitan
public interface ITelemetriaService
{
    /// <summary>
    /// Informa que se visitó <paramref name="url"/>. Nunca lanza excepciones ni retrasa al usuario:
    /// si falla, se ignora. No se envía nada si el navegador pidió "No rastrear".
    /// </summary>
    Task RegistrarVistaAsync(string? url, CancellationToken ct = default);
}
