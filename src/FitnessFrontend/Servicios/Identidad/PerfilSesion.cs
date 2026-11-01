using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FitnessFrontend.Servicios.Identidad;

// RF-013: identidad y roles de la sesión, obtenidos de GET /api/usuarios/yo tras iniciar sesión.
// El bearer token de Identity es OPACO (no un JWT), así que los roles no se pueden leer del token en el cliente.
// Los roles son los que el backend aplicó al emitir el token: lo que la interfaz muestra coincide con lo que la API autoriza.
public record PerfilSesion(Guid Id, string Email, IReadOnlyList<string> Roles)
{
    public const string TipoAutenticacion = "bearer-perfil";

    public IEnumerable<Claim> ComoClaims()
    {
        yield return new Claim(ClaimTypes.NameIdentifier, Id.ToString());
        yield return new Claim(ClaimTypes.Name, Email);
        yield return new Claim(ClaimTypes.Email, Email);
        foreach (var rol in (Roles ?? Array.Empty<string>()).Distinct())
        {
            yield return new Claim(ClaimTypes.Role, rol);
        }
    }
}
