using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Retroalimentacion;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Retroalimentacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class RetroalimentacionF7Tests : BunitContext
{
    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _principal;

        public FakeAuthStateProvider(string rol = "Usuario", string email = "estudiante@itm.edu.co", Guid? id = null)
        {
            var userId = id ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, rol),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuth");
            _principal = new ClaimsPrincipal(identity);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_principal));
    }

    private class FakeAuthService : IAutenticacionService
    {
        private readonly UsuarioSesion _usuario;

        public FakeAuthService(string rol = "Usuario", string email = "estudiante@itm.edu.co", Guid? id = null)
        {
            _usuario = new UsuarioSesion(id ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), email, new[] { rol });
        }

        public Task<UsuarioSesion?> ObtenerUsuarioActualAsync() => Task.FromResult<UsuarioSesion?>(_usuario);
        public Task<ResultadoOperacion> IniciarSesionAsync(string email, string password) => throw new NotImplementedException();
        public Task<ResultadoOperacion> RegistrarAsync(string email, string password, string nombreCompleto, TipoUsuario tipoUsuario, bool aceptaTratamientoDatos) => throw new NotImplementedException();
        public Task CerrarSesionAsync() => Task.CompletedTask;
        public Task<string?> ObtenerTokenAsync() => Task.FromResult<string?>("token");
        public Task LimpiarSesionAsync() => Task.CompletedTask;
        public Task<ResultadoOperacion> RenovarSesionAsync() => Task.FromResult(ResultadoOperacion.Exito());
    }

    private class RetroalimentacionServiceFalso : IRetroalimentacionClienteService
    {
        public List<ReporteProblemaDto> Reportes { get; } = new();
        public bool Simular429 { get; set; }
        public bool SimularErrorGenerico { get; set; }

        public Task<EstadoDeCarga<ReporteProblemaDto>> EnviarReporteAsync(CrearReporteDto dto, CancellationToken ct = default)
        {
            if (Simular429)
            {
                return Task.FromResult(EstadoDeCarga<ReporteProblemaDto>.Fallo(
                    "Ha enviado varios reportes recientemente; por favor intente más tarde.", "429"));
            }

            if (SimularErrorGenerico)
            {
                return Task.FromResult(EstadoDeCarga<ReporteProblemaDto>.Fallo("Error interno"));
            }

            var nuevo = new ReporteProblemaDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "test@itm.edu.co",
                dto.Tipo,
                dto.Mensaje,
                dto.RutaOrigen,
                "Pendiente",
                DateTimeOffset.UtcNow);

            Reportes.Add(nuevo);
            return Task.FromResult(EstadoDeCarga<ReporteProblemaDto>.Correcto(nuevo));
        }

        public Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ObtenerMisReportesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(Reportes.ToList()));
        }

        public Task<EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>> ListarBandejaAdminAsync(string? estado = null, CancellationToken ct = default)
        {
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteProblemaDto>>.Correcto(Reportes.ToList()));
        }

        public Task<EstadoDeCarga<ReporteProblemaDto>> CambiarEstadoAsync(Guid id, string nuevoEstado, string? respuesta = null, CancellationToken ct = default)
        {
            var r = Reportes.First(x => x.Id == id);
            var modificado = r with { Estado = nuevoEstado, RespuestaAdmin = respuesta ?? r.RespuestaAdmin };
            return Task.FromResult(EstadoDeCarga<ReporteProblemaDto>.Correcto(modificado));
        }
    }

    [Fact]
    public void Prueba1_ContenidoSeRenderizaComoTextoPlano_SinInterpretarHtmlNiScripts()
    {
        // 1. El contenido se renderiza como texto plano; ninguna etiqueta se inyecta en el DOM
        var servicio = new RetroalimentacionServiceFalso();
        var mensajeXss = "<script>alert('xss')</script><b>Texto negrita</b>";
        servicio.Reportes.Add(new ReporteProblemaDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "estudiante@itm.edu.co",
            "Problema",
            mensajeXss,
            "/eventos",
            "Pendiente",
            DateTimeOffset.UtcNow,
            "Respuesta con <i>itálicas</i>"));

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IRetroalimentacionClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<MisReportes>();

        // Las etiquetas NO deben existir como nodos HTML reales
        Assert.Empty(cut.FindAll("script"));
        Assert.Empty(cut.FindAll("b"));
        Assert.Empty(cut.FindAll("i"));

        // El texto crudo debe estar presente en el markup pero escapado sin etiquetas vivas
        Assert.DoesNotContain("<script>", cut.Markup);
        Assert.DoesNotContain("<b>", cut.Markup);
        Assert.Contains("&lt;script&gt;", cut.Markup);
        Assert.Contains("xss", cut.Markup);
    }

    [Fact]
    public void Prueba2_Error429_MuestraTextoDelSistemaSinCulparAlUsuario()
    {
        // 2. 429 se muestra con el texto del sistema sin culpar
        var servicio = new RetroalimentacionServiceFalso { Simular429 = true };

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IRetroalimentacionClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<Reportar>();

        // Llenar mensaje
        cut.Find("textarea").Input("No funciona el escaneo de QR en torniquete norte.");
        cut.Find("form").Submit();

        var alert = cut.Find("[role='alert']");
        Assert.NotNull(alert);
        Assert.Contains("Ha enviado varios reportes recientemente; por favor intente más tarde.", alert.TextContent);
    }

    [Fact]
    public async Task Prueba3_Aislamiento_UsuarioNoVeReportesAjenos()
    {
        // 3. Un usuario no ve reportes ajenos (verificado en el servicio con aislamiento de usuario)
        var idUsuario1 = Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111");
        var idUsuario2 = Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222");

        var authService1 = new FakeAuthService(email: "usuario1@itm.edu.co", id: idUsuario1);
        var servicioReal = new RetroalimentacionClienteService(authService1);

        // Usuario 1 envía un reporte
        await servicioReal.EnviarReporteAsync(new CrearReporteDto("Sugerencia", "Mensaje de usuario 1", "/inicio"));

        // Usuario 1 consulta sus reportes: ve 1 reporte
        var misReportes1 = await servicioReal.ObtenerMisReportesAsync();
        Assert.True(misReportes1.EsExito);
        Assert.All(misReportes1.Valor!, r => Assert.Equal(idUsuario1, r.UsuarioId));

        // Ahora un usuario 2 consulta sus reportes: NO ve los del usuario 1
        var authService2 = new FakeAuthService(email: "usuario2@itm.edu.co", id: idUsuario2);
        var servicioRealParaUsuario2 = new RetroalimentacionClienteService(authService2);

        var misReportes2 = await servicioRealParaUsuario2.ObtenerMisReportesAsync();
        Assert.True(misReportes2.EsExito);
        Assert.DoesNotContain(misReportes2.Valor!, r => r.UsuarioId == idUsuario1);
    }

    [Fact]
    public void Prueba4_BandejaAdministrador_SoloAccesibleParaAdministrador()
    {
        // 4. Usuario/Entrenador no ven la bandeja de administración
        var attr = typeof(BandejaAdministrador).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Administrador", attr.Roles);
    }

    [Fact]
    public void Prueba5_ContadorCaracteres_ValidaLimiteDe1000()
    {
        // Validación de interfaz: contador en vivo de caracteres
        var servicio = new RetroalimentacionServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IRetroalimentacionClienteService>(servicio);

        var cut = Render<Reportar>();

        // Inicialmente 0 / 1000 y botón deshabilitado
        Assert.Contains("0 / 1000", cut.Markup);
        var botonEnviar = cut.Find("button[type='submit']");
        Assert.True(botonEnviar.HasAttribute("disabled"));

        // Escribir 50 caracteres
        var textoPrueba = new string('a', 50);
        cut.Find("textarea").Input(textoPrueba);

        Assert.Contains("50 / 1000", cut.Markup);
        botonEnviar = cut.Find("button[type='submit']");
        Assert.False(botonEnviar.HasAttribute("disabled"));
    }
}
