using System;
using System.Net.Http;
using FitnessFrontend;
using FitnessFrontend.ApiCliente;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL pública de la API. En desarrollo viene de appsettings.Development.json; en producción appsettings.json trae un
// marcador que se sustituye al desplegar (variable URL_BASE_API de la imagen Docker o el paso de CI). Sin valor real, falla
// al arrancar con un mensaje claro en lugar de llamar a una API equivocada.
var urlBaseApi = builder.Configuration["UrlBaseApi"];
if (string.IsNullOrWhiteSpace(urlBaseApi) || urlBaseApi.StartsWith("REEMPLAZAR", StringComparison.Ordinal))
{
    throw new InvalidOperationException("UrlBaseApi no está configurada. Defina la URL de la API en wwwroot/appsettings.json (o la variable URL_BASE_API de la imagen Docker).");
}

// Servicios transversales de infraestructura
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

// Autenticación y autorización (RF-013, Patrón 7)
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ProveedorEstadoAutenticacion>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ProveedorEstadoAutenticacion>());
builder.Services.AddScoped<ManejadorDeAutenticacionHttp>();

// HttpClient para la API con DelegatingHandler de autenticación
builder.Services.AddScoped(sp =>
{
    var manejador = sp.GetRequiredService<ManejadorDeAutenticacionHttp>();
    manejador.InnerHandler = new HttpClientHandler();
    var client = new HttpClient(manejador)
    {
        BaseAddress = new Uri(urlBaseApi)
    };
    return client;
});

// Cliente Kiota generado contra el backend
builder.Services.AddScoped(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)
    {
        BaseUrl = urlBaseApi
    };
    return new FitnessApiClient(adapter);
});

// Servicios de negocio
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Eventos.IEventosClienteService, FitnessFrontend.Servicios.Eventos.EventosClienteService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Reservas.IReservasClienteService, FitnessFrontend.Servicios.Reservas.ReservasClienteService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Asistencia.IAsistenciaClienteService, FitnessFrontend.Servicios.Asistencia.AsistenciaClienteService>();
builder.Services.AddSingleton<FitnessFrontend.Servicios.Asistencia.IFlujoCheckInService, FitnessFrontend.Servicios.Asistencia.FlujoCheckInService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Priorizacion.IPriorizacionClienteService, FitnessFrontend.Servicios.Priorizacion.PriorizacionClienteService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService, FitnessFrontend.Servicios.Usuarios.UsuariosClienteService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Asistencia.IPuntosAccesoClienteService, FitnessFrontend.Servicios.Asistencia.PuntosAccesoClienteService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Bloqueos.IBloqueosClienteService, FitnessFrontend.Servicios.Bloqueos.BloqueosClienteService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Asistencia.IAsistenciaManualClienteService, FitnessFrontend.Servicios.Asistencia.AsistenciaManualClienteService>();
builder.Services.AddScoped<FitnessFrontend.Servicios.Eventos.INotasYAnunciosClienteService, FitnessFrontend.Servicios.Eventos.NotasYAnunciosClienteService>();

await builder.Build().RunAsync();
