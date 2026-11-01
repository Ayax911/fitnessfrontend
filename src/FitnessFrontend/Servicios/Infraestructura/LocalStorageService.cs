using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace FitnessFrontend.Servicios.Infraestructura;

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask GuardarItemAsync(string clave, string valor)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", clave, valor);
    }

    public async ValueTask<string?> ObtenerItemAsync(string clave)
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", clave);
    }

    public async ValueTask EliminarItemAsync(string clave)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", clave);
    }
}
