using System;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.ApiCliente.Models;
using Microsoft.JSInterop;

namespace FitnessFrontend.Servicios.Telemetria;

// RF-033 (analítica de uso): solo se envía la ruta normalizada y el tipo de dispositivo; el backend añade el rol
// si hay sesión. No se envía usuario, correo ni parámetros, y la telemetría nunca debe afectar al usuario.
public class TelemetriaService : ITelemetriaService
{
    private readonly FitnessApiClient _api;
    private readonly IJSRuntime _js;
    private string? _ultimaPantalla;

    public TelemetriaService(FitnessApiClient api, IJSRuntime js)
    {
        _api = api;
        _js = js;
    }

    public async Task RegistrarVistaAsync(string? url, CancellationToken ct = default)
    {
        try
        {
            var pantalla = NormalizadorDeRutas.Normalizar(url);
            // No se repite la misma pantalla seguida (p. ej. al refrescar datos o cambiar solo un parámetro)
            if (pantalla == null || pantalla == _ultimaPantalla) return;

            // null = el navegador pidió "No rastrear": se respeta
            var dispositivo = await _js.InvokeAsync<string?>("itmTelemetria.dispositivo", ct);
            if (string.IsNullOrEmpty(dispositivo)) return;

            _ultimaPantalla = pantalla;
            await _api.Api.Telemetria.Vistas.PostAsync(
                new RegistrarVistaRequest { Pantalla = pantalla, Dispositivo = dispositivo },
                cancellationToken: ct);
        }
        catch
        {
            // Una medición fallida (sin red, 429, JS no disponible) no debe verse ni molestar
        }
    }
}
