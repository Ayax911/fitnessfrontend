using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;

namespace FitnessFrontend.Servicios.Identidad;

// RF-013: Servicio de autenticación y gestión de sesión (docs/arquitectura/03-modelo-de-estado-cliente.md)
public interface IAutenticacionService
{
    Task<ResultadoOperacion> IniciarSesionAsync(string email, string password);
    // RF-013, RF-025: registro con perfil y autorización expresa de tratamiento de datos (RNF-011, Ley 1581)
    Task<ResultadoOperacion> RegistrarAsync(string email, string password, string nombreCompleto, TipoUsuario tipoUsuario, bool aceptaTratamientoDatos);
    Task CerrarSesionAsync();
    Task<string?> ObtenerTokenAsync();
    Task<UsuarioSesion?> ObtenerUsuarioActualAsync();
    Task LimpiarSesionAsync();
}
