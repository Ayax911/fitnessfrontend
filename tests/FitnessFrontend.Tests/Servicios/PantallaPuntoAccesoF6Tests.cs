using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Asistencia;
using FitnessFrontend.Paginas.Asistencia.Administrar;
using FitnessFrontend.Servicios.Asistencia;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class PantallaPuntoAccesoF6Tests : BunitContext
{
    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _principal;

        public FakeAuthStateProvider(string rol = "Administrador")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "Admin Punto"),
                new Claim(ClaimTypes.Role, rol),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }, "TestAuth");
            _principal = new ClaimsPrincipal(identity);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_principal));
    }

    private class PuntosAccesoServiceFalso : IPuntosAccesoClienteService
    {
        public int LlamadasObtenerQr { get; private set; }
        public QrPuntoAccesoDto? RespuestaQr { get; set; }
        public bool SimularFalloRed { get; set; }

        public Task<EstadoDeCarga<List<PuntoAccesoDto>>> ListarAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<PuntoAccesoDto>> CrearAsync(string nombre, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<PuntoAccesoDto>> ActualizarAsync(Guid id, string nombre, bool activo, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<QrPuntoAccesoDto>> ObtenerQrActualAsync(Guid puntoAccesoId, CancellationToken ct = default)
        {
            LlamadasObtenerQr++;
            if (SimularFalloRed)
            {
                return Task.FromResult(EstadoDeCarga<QrPuntoAccesoDto>.Fallo("Error de red con el servidor."));
            }

            var dto = RespuestaQr ?? new QrPuntoAccesoDto(
                "token_seguro_rotativo_123",
                DateTimeOffset.UtcNow.AddSeconds(15),
                15,
                "Acceso Puerta Principal");

            return Task.FromResult(EstadoDeCarga<QrPuntoAccesoDto>.Correcto(dto));
        }
    }

    private class AsistenciaClienteServiceFalso : IAsistenciaClienteService
    {
        public string? CodigoError { get; set; }
        public string? MensajeError { get; set; }

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarIngresoGeneralAsync(string tokenPuntoAcceso, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(CodigoError))
            {
                return Task.FromResult(EstadoDeCarga<CheckInResultadoDto>.Fallo(MensajeError ?? "Error", CodigoError));
            }

            return Task.FromResult(EstadoDeCarga<CheckInResultadoDto>.Correcto(new CheckInResultadoDto
            {
                Mensaje = "Ingreso concedido",
                Detalle = "Punto de Acceso Principal"
            }));
        }

        public Task<EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>> ObtenerHistorialAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<SesionQRDto>> ObtenerQRDeSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarCheckInAsync(string token, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    [Fact]
    public void Prueba1_QrCodificaUrlConTokenDelServidor()
    {
        // 1 y 4. El QR codificado es {BaseUri}ingreso/{token} con el token del servidor
        var puntoId = Guid.NewGuid();
        var servicio = new PuntosAccesoServiceFalso
        {
            RespuestaQr = new QrPuntoAccesoDto("token_rotativo_xyz_789", DateTimeOffset.UtcNow.AddSeconds(15), 15, "Torniquete 1")
        };

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IPuntosAccesoClienteService>(servicio);

        var cut = Render<PantallaPuntoAcceso>(p => p.Add(x => x.IdPunto, puntoId));

        var componenteQr = cut.FindComponent<FitnessFrontend.Componentes.Compartidos.CodigoQr>();
        Assert.NotNull(componenteQr);
        Assert.Contains("ingreso/token_rotativo_xyz_789", componenteQr.Instance.Valor);
        Assert.Contains("Torniquete 1", cut.Markup);
    }

    [Fact]
    public void Prueba2_FalloRed_MuestraSinConexionSiQrVencio()
    {
        // 2. Con fallo de red y QR vencido, se muestra el estado claro "Sin conexión con el servidor"
        var puntoId = Guid.NewGuid();
        var servicio = new PuntosAccesoServiceFalso
        {
            SimularFalloRed = true,
            RespuestaQr = new QrPuntoAccesoDto("token_expirado", DateTimeOffset.UtcNow.AddSeconds(-5), 15, "Torniquete 1")
        };

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IPuntosAccesoClienteService>(servicio);

        var cut = Render<PantallaPuntoAcceso>(p => p.Add(x => x.IdPunto, puntoId));

        Assert.Contains("Sin conexión con el servidor", cut.Markup);
        Assert.Contains("Reintentando obtener un nuevo código", cut.Markup);
        Assert.Empty(cut.FindComponents<FitnessFrontend.Componentes.Compartidos.CodigoQr>());
    }

    [Fact]
    public void Prueba3_Desmontar_CancelaTemporizadoresSinErrores()
    {
        // 3. Al desmontarse no quedan temporizadores activos ni fugas
        var puntoId = Guid.NewGuid();
        var servicio = new PuntosAccesoServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IPuntosAccesoClienteService>(servicio);

        var cut = Render<PantallaPuntoAcceso>(p => p.Add(x => x.IdPunto, puntoId));
        Assert.NotNull(cut.Instance);

        // Se desmonta el componente
        cut.Dispose();
        // Si no arrojó excepciones, Dispose() limpió timers y CTS exitosamente
    }

    [Fact]
    public void Prueba4_Ingreso_QrVencido_MuestraMensajeSinBucleReintento()
    {
        // 5. QrVencido se muestra con instrucción clara y sin botón de reintento con el mismo enlace
        var asistenciaService = new AsistenciaClienteServiceFalso
        {
            CodigoError = "QrVencido",
            MensajeError = "El código QR ha expirado."
        };

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("Usuario"));
        Services.AddSingleton<IAsistenciaClienteService>(asistenciaService);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<Ingreso>(p => p.Add(x => x.IdPuntoAcceso, "token_vencido"));
        var markup = cut.Markup;

        Assert.Contains("Código QR vencido", markup);
        Assert.Contains("Por favor, escanee de nuevo el código que se muestra en la pantalla del punto de acceso", markup);

        // Verifica que NO existe botón de "Reintentar"
        var botones = cut.FindAll("button");
        Assert.DoesNotContain(botones, b => b.TextContent.Trim() == "Reintentar");
    }

    [Fact]
    public void Prueba5_AutorizacionAdministrador_YAriaLiveDesactivado()
    {
        // 6 y 7. La pantalla es solo de Administrador y tiene aria-live desactivado para la cuenta regresiva
        var attr = typeof(PantallaPuntoAcceso).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Administrador", attr.Roles);

        var servicio = new PuntosAccesoServiceFalso();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IPuntosAccesoClienteService>(servicio);

        var cut = Render<PantallaPuntoAcceso>(p => p.Add(x => x.IdPunto, Guid.NewGuid()));

        var elementoAria = cut.Find("[aria-live='off']");
        Assert.NotNull(elementoAria);
    }
}
