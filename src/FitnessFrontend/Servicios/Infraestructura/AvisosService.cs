using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FitnessFrontend.Servicios.Infraestructura;

// Avisos flotantes (toasts) de la interfaz: confirmaciones, listas de espera y errores de una acción.
// Solo muestran el resultado que ya decidió el backend; no contienen reglas de negocio (RN-020: nada de priorización ni posición).
public enum TipoAviso { Exito, Espera, Info, Error }

public sealed record Aviso(Guid Id, TipoAviso Tipo, string Titulo, string? Mensaje, string? EnlaceTexto, string? EnlaceUrl);

public interface IAvisosService
{
    IReadOnlyList<Aviso> Activos { get; }
    event Action? Cambio;
    void Exito(string titulo, string? mensaje = null, string? enlaceTexto = null, string? enlaceUrl = null);
    void Espera(string titulo, string? mensaje = null);
    void Info(string titulo, string? mensaje = null);
    void Error(string titulo, string? mensaje = null);
    void Descartar(Guid id);
}

public sealed class AvisosService : IAvisosService, IDisposable
{
    private const int MaximoVisibles = 3;
    private readonly object _candado = new();
    private readonly List<Aviso> _avisos = new();
    private readonly Dictionary<Guid, Timer> _temporizadores = new();

    /// <summary>Segundos que permanece visible un aviso (los de error duran el doble).</summary>
    public int SegundosVisible { get; set; } = 6;

    public event Action? Cambio;

    public IReadOnlyList<Aviso> Activos { get { lock (_candado) return _avisos.ToList(); } }

    public void Exito(string titulo, string? mensaje = null, string? enlaceTexto = null, string? enlaceUrl = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Exito, titulo, mensaje, enlaceTexto, enlaceUrl));

    public void Espera(string titulo, string? mensaje = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Espera, titulo, mensaje, null, null));

    public void Info(string titulo, string? mensaje = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Info, titulo, mensaje, null, null));

    public void Error(string titulo, string? mensaje = null) =>
        Agregar(new Aviso(Guid.NewGuid(), TipoAviso.Error, titulo, mensaje, null, null));

    public void Descartar(Guid id)
    {
        bool cambio;
        lock (_candado)
        {
            cambio = _avisos.RemoveAll(a => a.Id == id) > 0;
            if (_temporizadores.Remove(id, out var t)) t.Dispose();
        }
        if (cambio) Cambio?.Invoke();
    }

    private void Agregar(Aviso aviso)
    {
        lock (_candado)
        {
            _avisos.Add(aviso);
            while (_avisos.Count > MaximoVisibles)
            {
                var viejo = _avisos[0];
                _avisos.RemoveAt(0);
                if (_temporizadores.Remove(viejo.Id, out var t)) t.Dispose();
            }
            var segundos = aviso.Tipo == TipoAviso.Error ? SegundosVisible * 2 : SegundosVisible;
            _temporizadores[aviso.Id] = new Timer(_ => Descartar(aviso.Id), null, TimeSpan.FromSeconds(segundos), Timeout.InfiniteTimeSpan);
        }
        Cambio?.Invoke();
    }

    public void Dispose()
    {
        lock (_candado)
        {
            foreach (var t in _temporizadores.Values) t.Dispose();
            _temporizadores.Clear();
        }
    }
}
