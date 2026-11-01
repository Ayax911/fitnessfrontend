using System;
using System.Collections.Generic;

namespace FitnessFrontend.Servicios.Identidad;

public record UsuarioSesion(Guid Id, string Email, IReadOnlyList<string> Roles)
{
    public bool EstaEnRol(string rol) =>
        Roles.Contains(rol, StringComparer.OrdinalIgnoreCase);

    public bool EsAdministrador => EstaEnRol("Administrador");
    public bool EsEntrenador => EstaEnRol("Entrenador");
    public bool EsUsuario => EstaEnRol("Usuario");
}

public record ResultadoOperacion(bool EsExitoso, string? Mensaje = null, string? CodigoError = null)
{
    public static ResultadoOperacion Exito(string? mensaje = null) => new(true, mensaje);
    public static ResultadoOperacion Fallo(string mensaje, string? codigo = null) => new(false, mensaje, codigo);
}
