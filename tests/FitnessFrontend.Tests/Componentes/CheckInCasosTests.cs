using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Asistencia;
using FitnessFrontend.Paginas.Identidad;
using FitnessFrontend.Servicios.Asistencia;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Pruebas obligatorias Fase 3 (docs/arquitectura/08-guia-de-implementacion.md, skill pruebas-obligatorias-fitnessfrontend):
// Demostración de los tres casos A/B/C del flujo de check-in por QR (docs/arquitectura/06-flujo-checkin-qr.md)
public class CheckInCasosTests : BunitContext
{
    public class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _user = new(new ClaimsIdentity());

        public void SetAuthorized(string email, params string[] roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, email),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            _user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void SetNotAuthorized()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_user));
    }

    private class FakeAsistenciaClienteService : IAsistenciaClienteService
    {
        public Func<string, EstadoDeCarga<CheckInResultadoDto>>? OnRegistrarCheckIn { get; set; }

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarCheckInAsync(string tokenSesion, CancellationToken ct = default)
        {
            if (OnRegistrarCheckIn != null)
            {
                return Task.FromResult(OnRegistrarCheckIn(tokenSesion));
            }

            return Task.FromResult(EstadoDeCarga<CheckInResultadoDto>.Correcto(new CheckInResultadoDto
            {
                Exitoso = true,
                Mensaje = "Asistencia confirmada.",
                Detalle = "Sesión Deportiva de Prueba"
            }));
        }

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarIngresoGeneralAsync(string idPuntoAcceso, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<CheckInResultadoDto>.Correcto(new CheckInResultadoDto { Exitoso = true }));

        public Task<EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>> ObtenerHistorialAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>.Correcto(new List<HistorialAsistenciaDto>()));

        public Task<EstadoDeCarga<SesionQRDto>> ObtenerQRDeSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<SesionQRDto>.Correcto(new SesionQRDto()));
    }

    private class FakeAutenticacionService : IAutenticacionService
    {
        public Task<ResultadoOperacion> IniciarSesionAsync(string email, string password) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<ResultadoOperacion> RegistrarAsync(string email, string password, string nombreCompleto, FitnessFrontend.ApiCliente.Models.TipoUsuario tipoUsuario, bool aceptaTratamientoDatos) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task CerrarSesionAsync() => Task.CompletedTask;
        public Task<string?> ObtenerTokenAsync() => Task.FromResult<string?>("token-jwt-test");
        public Task<UsuarioSesion?> ObtenerUsuarioActualAsync() => Task.FromResult<UsuarioSesion?>(new UsuarioSesion(Guid.NewGuid(), "estudiante@itm.edu.co", new[] { "Usuario" }));
        public Task LimpiarSesionAsync() => Task.CompletedTask;
    }

    [Fact]
    public void CasoA_ConSesionIniciada_CompletaCheckInYMuestraConfirmacion()
    {
        // CASO A: Usuario autenticado escanea QR -> se completa el check-in y se muestra confirmación
        var authProvider = new FakeAuthStateProvider();
        authProvider.SetAuthorized("estudiante@itm.edu.co", "Usuario");
        Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        var tokenQr = "token-qr-sesion-valida";
        var servicioAsistencia = new FakeAsistenciaClienteService
        {
            OnRegistrarCheckIn = token =>
            {
                Assert.Equal(tokenQr, token);
                return EstadoDeCarga<CheckInResultadoDto>.Correcto(new CheckInResultadoDto
                {
                    Exitoso = true,
                    Mensaje = "¡Asistencia registrada para Spinning!",
                    Detalle = "Sala 2 - Campus Robledo"
                });
            }
        };

        Services.AddSingleton<IAsistenciaClienteService>(servicioAsistencia);
        Services.AddSingleton<IFlujoCheckInService, FlujoCheckInService>();
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<CheckIn>(parameters => parameters
            .Add(p => p.Token, tokenQr));

        var markup = cut.Markup;
        Assert.Contains("¡Asistencia confirmada!", markup);
        Assert.Contains("¡Asistencia registrada para Spinning!", markup);
        Assert.Contains("Sala 2 - Campus Robledo", markup);
    }

    [Fact]
    public void CasoB_SinSesionIniciada_RedirigeALoginConReturnUrl_YVuelveAutomaticamenteTrasLogin()
    {
        // CASO B: Usuario no autenticado escanea QR
        var authProvider = new FakeAuthStateProvider();
        authProvider.SetNotAuthorized();
        Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        var tokenQr = "token-qr-sesion-b";
        var servicioAsistencia = new FakeAsistenciaClienteService();
        Services.AddSingleton<IAsistenciaClienteService>(servicioAsistencia);
        Services.AddSingleton<IFlujoCheckInService, FlujoCheckInService>();
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();
        Services.AddSingleton<IAutenticacionService, FakeAutenticacionService>();

        var navMan = Services.GetRequiredService<NavigationManager>();

        // 1. Renderiza CheckIn sin estar autenticado
        var cut = Render<CheckIn>(parameters => parameters
            .Add(p => p.Token, tokenQr));

        // Verificación 1: Redirige a Login incluyendo el returnUrl con el token de sesión
        var expectedReturnUrl = $"/checkin/{tokenQr}";
        Assert.Contains("/login?returnUrl=", navMan.Uri);
        Assert.Contains(Uri.EscapeDataString(expectedReturnUrl), navMan.Uri);

        // 2. Ahora el usuario está en Login.razor con el returnUrl que CheckIn configuró en la navegación
        var cutLogin = Render<Login>();

        // Llena credenciales y hace submit
        cutLogin.Find("#emailInput").Change("estudiante@itm.edu.co");
        cutLogin.Find("#passwordInput").Change("Password123!");
        cutLogin.Find("form").Submit();

        // Verificación 2 (DoD Caso B):
        // Tras el login exitoso, la aplicación vuelve AUTOMÁTICAMENTE a /checkin/{token}
        // SIN que el usuario tenga que volver a escanear nada
        Assert.EndsWith(expectedReturnUrl, navMan.Uri);
    }

    [Fact]
    public async Task CasoB_SinSesionIniciada_PasaPorRegistro_YCompletaCheckInAlFinal()
    {
        // Prueba obligatoria Ronda 2 (M-05 / I-09 / Caso B extendido):
        // 1. Usuario sin sesión escanea QR en /checkin/{token}
        var authProvider = new FakeAuthStateProvider();
        authProvider.SetNotAuthorized();
        Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        var tokenQr = "token-qr-flujo-registro";
        var servicioAsistencia = new FakeAsistenciaClienteService();
        Services.AddSingleton<IAsistenciaClienteService>(servicioAsistencia);
        Services.AddSingleton<IFlujoCheckInService, FlujoCheckInService>();
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();
        Services.AddSingleton<IAutenticacionService, FakeAutenticacionService>();

        var navMan = Services.GetRequiredService<NavigationManager>();

        // Paso 1: Accede a CheckIn sin sesión -> Redirige a /login con returnUrl
        var cutCheckInInicial = Render<CheckIn>(parameters => parameters
            .Add(p => p.Token, tokenQr));

        var expectedReturnUrl = $"/checkin/{tokenQr}";
        Assert.Contains("/login?returnUrl=", navMan.Uri);
        Assert.Contains(Uri.EscapeDataString(expectedReturnUrl), navMan.Uri);

        // Paso 2: En Login, el usuario no tiene cuenta y hace clic en "Registrarse"
        var cutLoginInicial = Render<Login>();
        var enlaceRegistro = cutLoginInicial.Find("a[href*='registro']");
        var hrefRegistro = enlaceRegistro.GetAttribute("href");

        // Verifica que el enlace hacia Registro propaga returnUrl (M-05)
        Assert.Contains("/registro?returnUrl=", hrefRegistro);
        Assert.Contains(Uri.EscapeDataString(expectedReturnUrl), hrefRegistro);

        // Paso 3: Renderiza Registro pasando el returnUrl propagado
        navMan.NavigateTo(hrefRegistro!);
        var cutRegistro = Render<Registro>();

        // Verifica que el enlace de vuelta a Login en Registro también propaga returnUrl (M-05)
        var enlaceVolverLogin = cutRegistro.Find("a[href*='login']");
        Assert.Contains("returnUrl=", enlaceVolverLogin.GetAttribute("href"));

        // Llena el formulario de Registro y envía
        cutRegistro.Find("#regNombreInput").Change("Nuevo Estudiante");
        cutRegistro.Find("#regTipoInput").Change("Estudiante");
        cutRegistro.Find("#regAutorizacionInput").Change(true);
        cutRegistro.Find("#regEmailInput").Change("nuevo.estudiante@itm.edu.co");
        cutRegistro.Find("#regPasswordInput").Change("Password123!");
        cutRegistro.Find("#regConfirmPasswordInput").Change("Password123!");
        cutRegistro.Find("form").Submit();

        // Espera a que complete el delay de 1.5s y navegue a Login
        cutRegistro.WaitForAssertion(() =>
        {
            Assert.Contains("/login?", navMan.Uri);
            Assert.Contains(Uri.EscapeDataString(expectedReturnUrl), navMan.Uri);
            Assert.Contains("registrado=true", navMan.Uri);
        }, TimeSpan.FromSeconds(3));

        // Paso 4: En Login con returnUrl y registrado=true, el usuario inicia sesión
        var cutLoginFinal = Render<Login>();
        Assert.Contains("Registro exitoso", cutLoginFinal.Markup);

        cutLoginFinal.Find("#emailInput").Change("nuevo.estudiante@itm.edu.co");
        cutLoginFinal.Find("#passwordInput").Change("Password123!");
        cutLoginFinal.Find("form").Submit();

        // Verifica que vuelve automáticamente a la URL original de checkin
        Assert.EndsWith(expectedReturnUrl, navMan.Uri);

        // Paso 5: Ahora autenticado, completa el check-in en /checkin/{token} sin volver a escanear
        authProvider.SetAuthorized("nuevo.estudiante@itm.edu.co");
        var cutCheckInFinal = Render<CheckIn>(parameters => parameters
            .Add(p => p.Token, tokenQr));

        Assert.Contains("¡Asistencia confirmada!", cutCheckInFinal.Markup);
    }

    [Fact]
    public void CasoC_ConTokenInvalidoOVentanaCerrada_MuestraErrorBackend_YNuncaPantallaDeLogin()
    {
        // CASO C: Usuario autenticado escanea token expirado o inválido
        var authProvider = new FakeAuthStateProvider();
        authProvider.SetAuthorized("estudiante@itm.edu.co");
        Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        var servicioAsistencia = new FakeAsistenciaClienteService
        {
            OnRegistrarCheckIn = _ => EstadoDeCarga<CheckInResultadoDto>.Fallo(
                "El código QR se encuentra fuera de la ventana de asistencia válida.",
                "FueraDeVentana")
        };

        Services.AddSingleton<IAsistenciaClienteService>(servicioAsistencia);
        Services.AddSingleton<IFlujoCheckInService, FlujoCheckInService>();
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<CheckIn>(parameters => parameters
            .Add(p => p.Token, "token-expirado"));

        var markup = cut.Markup;

        // Muestra el mensaje exacto del backend
        Assert.Contains("El código QR se encuentra fuera de la ventana de asistencia válida.", markup);
        Assert.Contains("No fue posible registrar la asistencia", markup);

        // NO navega a login (distinción crucial entre B y C)
        Assert.DoesNotContain("/login", navMan.Uri);
        Assert.DoesNotContain("Iniciar Sesión", markup);
    }

    [Fact]
    public void Bloque2Pregunta9_SinReservaConfirmada_MuestraErrorBackend_SinBotonReservarAhora()
    {
        // AMBIGÜEDAD Bloque 2, pregunta 9: Usuario escanea sin reserva confirmada
        var authProvider = new FakeAuthStateProvider();
        authProvider.SetAuthorized("estudiante@itm.edu.co");
        Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        var servicioAsistencia = new FakeAsistenciaClienteService
        {
            OnRegistrarCheckIn = _ => EstadoDeCarga<CheckInResultadoDto>.Fallo(
                "No tienes una reserva confirmada para esta sesión.",
                "SinReservaConfirmada")
        };

        Services.AddSingleton<IAsistenciaClienteService>(servicioAsistencia);
        Services.AddSingleton<IFlujoCheckInService, FlujoCheckInService>();
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<CheckIn>(parameters => parameters
            .Add(p => p.Token, "token-sin-reserva"));

        var markup = cut.Markup;

        // Muestra el mensaje del backend tal cual
        Assert.Contains("No tienes una reserva confirmada para esta sesión.", markup);

        // Comportamiento menos comprometedor: NO inventa una acción de "Reservar ahora"
        var markupMinuscula = markup.ToLowerInvariant();
        Assert.DoesNotContain("reservar ahora", markupMinuscula);
        Assert.DoesNotContain("reserva ahora", markupMinuscula);
    }
}
