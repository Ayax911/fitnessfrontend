using System.Threading.Tasks;

namespace FitnessFrontend.Servicios.Identidad;

// RF-013: Servicio de autenticación y gestión de sesión (docs/arquitectura/03-modelo-de-estado-cliente.md)
public interface IAutenticacionService
{
    Task<ResultadoOperacion> IniciarSesionAsync(string email, string password);
    Task<ResultadoOperacion> RegistrarAsync(string email, string password);
    Task CerrarSesionAsync();
    Task<string?> ObtenerTokenAsync();
    Task<UsuarioSesion?> ObtenerUsuarioActualAsync();
    Task LimpiarSesionAsync();
}
