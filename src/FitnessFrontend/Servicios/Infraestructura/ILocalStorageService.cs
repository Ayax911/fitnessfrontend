using System.Threading.Tasks;

namespace FitnessFrontend.Servicios.Infraestructura;

public interface ILocalStorageService
{
    ValueTask GuardarItemAsync(string clave, string valor);
    ValueTask<string?> ObtenerItemAsync(string clave);
    ValueTask EliminarItemAsync(string clave);
}
