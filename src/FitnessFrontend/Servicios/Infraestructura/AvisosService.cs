using System;
using System.Collections.Generic;
using System.Linq;

namespace FitnessFrontend.Servicios.Infraestructura;

// Ventanas de resultado (modal centrado) de la interfaz: reserva confirmada, lista de espera, denegada o error.
// Solo muestran el resultado que ya decidió el backend; no contienen reglas de negocio (RN-020: nada de priorización ni posición).
public enum TipoAviso { Exito, Espera, Info, Error }

/// <summary>Par etiqueta/valor que se muestra bajo el mensaje (p. ej. Actividad, Cuándo).</summary>
public sealed record DatoAviso(string Etiqueta, string Valor);

public sealed record Aviso(Guid Id, TipoAviso Tipo, string Titulo, string? Mensaje, string? EnlaceTexto, string? EnlaceUrl, IReadOnlyList<DatoAviso>? Datos);

public interface IAvisosService
{
    /// <summary>Cola de resultados pendientes; la ventana muestra siempre el primero.</summary>
    IReadOnlyList<Aviso> Activos { get; }
    event Action? Cambio;
    void Exito(string titulo, string? mensaje = null, string? enlaceTexto = null, string? enlaceUrl = null, IReadOnlyList<DatoAviso>? datos = null);
    void Espera(string titulo, string? mensaje = null, IReadOnlyList<DatoAviso>? datos = null);
    void Info(string titulo, string? mensaje = null, IReadOnlyList<DatoAviso>? datos = null);
    void Error(string titulo, string? mensaje = null, IReadOnlyList<DatoAviso>? datos = null);
    void Descartar(Guid id);
}

public sealed class AvisosService : IAvisosService
{
    private const int MaximoEnCola = 5;
    private readonly object _candado = new();
    private readonly List<Aviso> _avisos = new();

    public event Action? Cambio;

    public IReadOnlyList<Aviso> Activos { get { lock (_candado) return _avisos.ToList(); } }

    public void Exito(string titulo, string? mensaje = null, string? enlaceTexto = null, string? enlaceUrl = null, IReadOnlyList<DatoAviso>? datos = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Exito, titulo, mensaje, enlaceTexto, enlaceUrl, datos));

    public void Espera(string titulo, string? mensaje = null, IReadOnlyList<DatoAviso>? datos = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Espera, titulo, mensaje, null, null, datos));

    public void Info(string titulo, string? mensaje = null, IReadOnlyList<DatoAviso>? datos = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Info, titulo, mensaje, null, null, datos));

    public void Error(string titulo, string? mensaje = null, IReadOnlyList<DatoAviso>? datos = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Error, titulo, mensaje, null, null, datos));

    public void Descartar(Guid id)
    {
        bool cambio;
        lock (_candado) cambio = _avisos.RemoveAll(a => a.Id == id) > 0;
        if (cambio) Cambio?.Invoke();
    }

    private void Agregar(Aviso aviso)
    {
        lock (_candado)
        {
            _avisos.Add(aviso);
            // Si alguien dispara muchas acciones seguidas, se conservan las más recientes
            while (_avisos.Count > MaximoEnCola) _avisos.RemoveAt(0);
        }
        Cambio?.Invoke();
    }
}
