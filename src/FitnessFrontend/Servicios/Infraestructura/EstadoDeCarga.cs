using System;

namespace FitnessFrontend.Servicios.Infraestructura;

public enum TipoEstadoCarga
{
    Cargando,
    Exito,
    Error
}

// Patrón 2 (docs/arquitectura/04-patrones-de-diseno.md): Result Object de UI con tres estados posibles
public sealed class EstadoDeCarga<T>
{
    public TipoEstadoCarga Estado { get; }
    public T? Valor { get; }
    public string? MensajeError { get; }
    public string? CodigoError { get; }

    public bool EsCargando => Estado == TipoEstadoCarga.Cargando;
    public bool EsExito => Estado == TipoEstadoCarga.Exito;
    public bool EsError => Estado == TipoEstadoCarga.Error;

    private EstadoDeCarga(TipoEstadoCarga estado, T? valor, string? mensajeError, string? codigoError)
    {
        Estado = estado;
        Valor = valor;
        MensajeError = mensajeError;
        CodigoError = codigoError;
    }

    public static EstadoDeCarga<T> Cargando() =>
        new(TipoEstadoCarga.Cargando, default, null, null);

    public static EstadoDeCarga<T> Correcto(T valor) =>
        new(TipoEstadoCarga.Exito, valor, null, null);

    public static EstadoDeCarga<T> Fallo(string mensaje, string? codigo = null) =>
        new(TipoEstadoCarga.Error, default, mensaje, codigo);

    public TResult Match<TResult>(
        Func<TResult> onCargando,
        Func<T, TResult> onExito,
        Func<string, string?, TResult> onError)
    {
        return Estado switch
        {
            TipoEstadoCarga.Cargando => onCargando(),
            TipoEstadoCarga.Exito => onExito(Valor!),
            TipoEstadoCarga.Error => onError(MensajeError ?? "Error desconocido", CodigoError),
            _ => throw new InvalidOperationException($"Estado de carga no válido: {Estado}")
        };
    }
}
