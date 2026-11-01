using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Asistencia;
using FitnessFrontend.Servicios.Asistencia;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// RF-006: Registro de ingreso general por torniquete (RN-030)
public class IngresoTests : BunitContext
{
    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;
        public FakeAuthStateProvider(string email, params string[] roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, email),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            _user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_user));
    }

    private class FakeAsistenciaService : IAsistenciaClienteService
    {
        private readonly Func<string, EstadoDeCarga<CheckInResultadoDto>> _onIngreso;

        public FakeAsistenciaService(Func<string, EstadoDeCarga<CheckInResultadoDto>> onIngreso)
        {
            _onIngreso = onIngreso;
        }

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarCheckInAsync(string tokenSesion, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarIngresoGeneralAsync(string idPuntoAcceso, CancellationToken ct = default) =>
            Task.FromResult(_onIngreso(idPuntoAcceso));

        public Task<EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>> ObtenerHistorialAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<SesionQRDto>> ObtenerQRDeSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    [Fact]
    public void Ingreso_Exitoso_DebeMostrarConfirmacionConPuntoDeAcceso()
    {
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("usuario@itm.edu.co", "Usuario"));

        var servicio = new FakeAsistenciaService(punto =>
            EstadoDeCarga<CheckInResultadoDto>.Correcto(new CheckInResultadoDto
            {
                Exitoso = true,
                Mensaje = "Acceso concedido al gimnasio.",
                Detalle = "Torniquete Principal"
            }));

        Services.AddSingleton<IAsistenciaClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<Ingreso>(parameters => parameters
            .Add(p => p.IdPuntoAcceso, "Torniquete-Norte"));

        var markup = cut.Markup;
        Assert.Contains("¡Ingreso Autorizado!", markup);
        Assert.Contains("Acceso concedido al gimnasio.", markup);
        Assert.Contains("Torniquete-Norte", markup);
    }

    [Fact]
    public void Ingreso_RechazadoPorIntervaloMinimoRN030_DebeMostrarMensajeDelBackendSinCuentaRegresiva()
    {
        // RN-030: Intervalo mínimo entre ingresos. La UI muestra el rechazo devuelto por el backend
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("usuario@itm.edu.co", "Usuario"));

        var mensajeBackend = "Debe esperar al menos 45 minutos entre registros de ingreso al gimnasio.";
        var servicio = new FakeAsistenciaService(punto =>
            EstadoDeCarga<CheckInResultadoDto>.Fallo(mensajeBackend, "IntervaloMinimoNoCumplido"));

        Services.AddSingleton<IAsistenciaClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<Ingreso>(parameters => parameters
            .Add(p => p.IdPuntoAcceso, "Torniquete-Sur"));

        var markup = cut.Markup;
        // Aserción positiva: muestra el mensaje del backend
        Assert.Contains(mensajeBackend, markup);

        // Aserción negativa: la UI no inventó una cuenta regresiva propia en el cliente
        Assert.DoesNotContain("te quedan", markup.ToLowerInvariant());
        Assert.DoesNotContain("cuenta regresiva", markup.ToLowerInvariant());
    }

    private class FakeUnauthenticatedStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public void Ingreso_SinSesionIniciada_I09_RedirigeALoginConReturnUrl()
    {
        Services.AddSingleton<AuthenticationStateProvider>(new FakeUnauthenticatedStateProvider());
        Services.AddSingleton<IAsistenciaClienteService>(new FakeAsistenciaService(_ => EstadoDeCarga<CheckInResultadoDto>.Correcto(new CheckInResultadoDto())));
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Ingreso>(parameters => parameters
            .Add(p => p.IdPuntoAcceso, "Torniquete-Norte"));

        var expectedReturnUrl = "/ingreso/Torniquete-Norte";
        Assert.Contains("/login?returnUrl=", navMan.Uri);
        Assert.Contains(Uri.EscapeDataString(expectedReturnUrl), navMan.Uri);
    }
}
