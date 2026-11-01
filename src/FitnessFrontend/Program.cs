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

var urlBaseApi = builder.Configuration["UrlBaseApi"] ?? "http://localhost:5224";

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

await builder.Build().RunAsync();
