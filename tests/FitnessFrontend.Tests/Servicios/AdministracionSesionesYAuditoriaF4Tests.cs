using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.Paginas.Auditoria;
using FitnessFrontend.Paginas.Eventos.Administrar;
using FitnessFrontend.Servicios.Auditoria;
using FitnessFrontend.Servicios.Bloqueos;
using FitnessFrontend.Servicios.Eventos;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Sesiones;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class AdministracionSesionesYAuditoriaF4Tests : BunitContext
{
    private class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _principal;

        public FakeAuthStateProvider(string rol = "Administrador")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "Admin ITM"),
                new Claim(ClaimTypes.Role, rol),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }, "TestAuth");
            _principal = new ClaimsPrincipal(identity);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_principal));
    }

    private class AdminSesionesServiceFalso : IAdministracionSesionesClienteService
    {
        public SesionAdminDetalleDto? Detalle { get; set; }
        public string? UltimoMotivoCancelacion { get; private set; }
        public string? UltimoMotivoRetiro { get; private set; }
        public bool SimularFalloReprogramar { get; set; }

        public Task<EstadoDeCarga<SesionAdminDetalleDto>> ObtenerDetalleSesionAdminAsync(Guid sesionId, CancellationToken ct = default)
        {
            var res = Detalle ?? new SesionAdminDetalleDto(
                sesionId, Guid.NewGuid(), "Entrenamiento Funcional", "Carlos Entrenador",
                DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                20, 17, false, null,
                new List<ReservaAdminDetalleDto>
                {
                    new(Guid.NewGuid(), Guid.NewGuid(), "Ana Pérez", "ana@itm.edu.co", 0, DateTimeOffset.UtcNow.AddHours(-3)),
                    new(Guid.NewGuid(), Guid.NewGuid(), "Luis Gómez", "luis@itm.edu.co", 0, DateTimeOffset.UtcNow.AddHours(-2)),
                    new(Guid.NewGuid(), Guid.NewGuid(), "Marta Ruiz", "marta@itm.edu.co", 1, DateTimeOffset.UtcNow.AddHours(-1))
                });

            return Task.FromResult(EstadoDeCarga<SesionAdminDetalleDto>.Correcto(res));
        }

        public Task<ResultadoOperacion> CancelarSesionAsync(Guid sesionId, string motivo, CancellationToken ct = default)
        {
            UltimoMotivoCancelacion = motivo;
            return Task.FromResult(ResultadoOperacion.Exito("Sesión cancelada"));
        }

        public Task<ResultadoOperacion> ReprogramarSesionAsync(Guid sesionId, DateTimeOffset nuevoInicio, DateTimeOffset nuevoFin, string motivo, CancellationToken ct = default)
        {
            if (SimularFalloReprogramar)
            {
                return Task.FromResult(ResultadoOperacion.Fallo("La franja seleccionada coincide con un bloqueo de calendario por mantenimiento."));
            }
            return Task.FromResult(ResultadoOperacion.Exito("Reprogramada"));
        }

        public Task<ResultadoOperacion> InscribirTerceroAsync(Guid sesionId, Guid usuarioId, string motivo, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito("Inscrito"));

        public Task<ResultadoOperacion> RetirarTerceroAsync(Guid sesionId, Guid reservaId, string motivo, CancellationToken ct = default)
        {
            UltimoMotivoRetiro = motivo;
            return Task.FromResult(ResultadoOperacion.Exito("Retirado"));
        }
    }

    private class AuditoriaServiceFalso : IAuditoriaClienteService
    {
        public List<RegistroAuditoriaDto> Registros { get; set; } = new();

        public Task<EstadoDeCarga<PaginaAuditoriaDto>> ConsultarAuditoriaAsync(
            DateTimeOffset? desde = null, DateTimeOffset? hasta = null, string? tipoAccion = null, string? actor = null,
            int pagina = 1, int tamano = 25, CancellationToken ct = default)
        {
            var pag = new PaginaAuditoriaDto(Registros, 50, pagina, 2);
            return Task.FromResult(EstadoDeCarga<PaginaAuditoriaDto>.Correcto(pag));
        }
    }

    private class NotasYAnunciosServiceFalso : INotasYAnunciosClienteService
    {
        public Task<EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>> ObtenerNotasAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<NotaIncidenciaDto>>.Correcto(new List<NotaIncidenciaDto>()));

        public Task<ResultadoOperacion> CrearNotaAsync(Guid sesionId, string tipo, string contenido, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());

        public Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosPorSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(new List<AnuncioSesionDto>()));

        public Task<EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>> ObtenerAnunciosMisReservasAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<AnuncioSesionDto>>.Correcto(new List<AnuncioSesionDto>()));

        public Task<ResultadoOperacion> PublicarAnuncioAsync(Guid sesionId, string mensaje, CancellationToken ct = default) =>
            Task.FromResult(ResultadoOperacion.Exito());
    }

    [Fact]
    public void Prueba1_CancelarSesion_ExigeMotivo_YMuestraConteoReservasAfectadas()
    {
        // 1. Cancelar sesión exige motivo y confirma indicando número de reservas afectadas
        var sesionId = Guid.NewGuid();
        var adminService = new AdminSesionesServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IAdministracionSesionesClienteService>(adminService);
        Services.AddSingleton<INotasYAnunciosClienteService>(new NotasYAnunciosServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<AdministrarSesion>(p => p.Add(x => x.IdSesion, sesionId));

        // Clic en "Cancelar sesión"
        var botonCancelar = cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancelar sesión");
        botonCancelar.Click();

        // Verifica que se abre el modal y muestra conteo
        var modalContent = cut.Find("#resumenAfectados");
        Assert.NotNull(modalContent);
        Assert.Contains("3 reservas afectadas", modalContent.TextContent);
        Assert.Contains("2 confirmadas y 1 en lista de espera", modalContent.TextContent);

        // Intento de confirmar sin motivo
        var botonConfirmar = cut.FindAll("button").First(b => b.TextContent.Contains("Sí, cancelar sesión"));
        botonConfirmar.Click();

        Assert.Contains("El motivo de cancelación es obligatorio", cut.Markup);
        Assert.Null(adminService.UltimoMotivoCancelacion);

        // Ahora escribe motivo y confirma
        var inputMotivo = cut.Find("#motivoCancelacionSesion");
        inputMotivo.Change("Falla técnica en sistema de ventilación");

        botonConfirmar.Click();
        Assert.Equal("Falla técnica en sistema de ventilación", adminService.UltimoMotivoCancelacion);
    }

    [Fact]
    public void Prueba2_RetirarTercero_AdvierteSobrePromocionListaDeEspera()
    {
        // 2. Retirar tercero advierte sobre promoción de lista de espera
        var sesionId = Guid.NewGuid();
        var adminService = new AdminSesionesServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IAdministracionSesionesClienteService>(adminService);
        Services.AddSingleton<INotasYAnunciosClienteService>(new NotasYAnunciosServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<AdministrarSesion>(p => p.Add(x => x.IdSesion, sesionId));

        // Busca botón Retirar en la tabla
        var botonRetirar = cut.FindAll("button").First(b => b.TextContent.Trim() == "Retirar");
        botonRetirar.Click();

        // Verifica advertencia sobre promoción en lista de espera
        var markup = cut.Markup;
        Assert.Contains("Confirmar retiro de reserva", markup);
        Assert.Contains("se promoverá automáticamente al primer usuario en lista de espera", markup);
    }

    [Fact]
    public void Prueba3_ReprogramarSesion_SiBackendRechaza_MuestraMensajeSinRecargar()
    {
        // 3. Reprogramar a fecha bloqueada muestra el error del backend sin recargar
        var sesionId = Guid.NewGuid();
        var adminService = new AdminSesionesServiceFalso { SimularFalloReprogramar = true };

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IAdministracionSesionesClienteService>(adminService);
        Services.AddSingleton<INotasYAnunciosClienteService>(new NotasYAnunciosServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<AdministrarSesion>(p => p.Add(x => x.IdSesion, sesionId));

        var botonReprogramar = cut.FindAll("button").First(b => b.TextContent.Trim() == "Reprogramar fecha/hora");
        botonReprogramar.Click();

        var inputMotivo = cut.Find("#reprogMotivo");
        inputMotivo.Change("Ajuste por solicitud de bienestar");

        var botonConfirmar = cut.FindAll("button").First(b => b.TextContent.Contains("Confirmar reprogramación"));
        botonConfirmar.Click();

        // El error devuelto por el servicio se despliega dentro del modal
        Assert.Contains("La franja seleccionada coincide con un bloqueo de calendario", cut.Markup);
    }

    [Fact]
    public void Prueba4_Auditoria_FormateaAAmericaBogota_YPaginaCorrectamente()
    {
        // 4. Auditoría formatea a America/Bogota y pagina
        var offsetBogota = TimeSpan.FromHours(-5);
        // 2026-11-04 20:30 Bogota = 2026-11-05 01:30 UTC
        var fechaBogota = new DateTimeOffset(2026, 11, 4, 20, 30, 0, offsetBogota);
        var fechaUtc = fechaBogota.ToUniversalTime();

        var auditoriaService = new AuditoriaServiceFalso();
        auditoriaService.Registros.Add(new RegistroAuditoriaDto(
            Guid.NewGuid(), fechaUtc, "SuperAdmin", "Administrador", "CancelarSesion", "Sesion", "Detalle de prueba"));

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IAuditoriaClienteService>(auditoriaService);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<RegistroAuditoria>();
        var markup = cut.Markup;

        // Verifica que la hora formateada corresponde a hora local de Bogotá (08:30 p. m. / 20:30)
        Assert.Contains("08:30", markup);
        Assert.Contains("04/11/2026", markup);

        // Verifica presencia de controles de paginación
        Assert.Contains("Página 1 de 2", markup);
        Assert.Contains("Total: 50 registros", markup);
        Assert.Contains("Siguiente", markup);
    }

    [Fact]
    public void Prueba5_AusenciaPriorizacionRN020_EnAdministracionYAuditoria()
    {
        // 5. Ningún componente de administración o auditoría expone campos de priorización de usuario (RN-020)
        var sesionId = Guid.NewGuid();
        var adminService = new AdminSesionesServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IAdministracionSesionesClienteService>(adminService);
        Services.AddSingleton<INotasYAnunciosClienteService>(new NotasYAnunciosServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<AdministrarSesion>(p => p.Add(x => x.IdSesion, sesionId));
        var markup = cut.Markup;

        // Aserciones estrictas negativas
        Assert.DoesNotMatch(new Regex(@"puesto\s+\d+", RegexOptions.IgnoreCase), markup);
        Assert.DoesNotMatch(new Regex(@"posición\s+\d+", RegexOptions.IgnoreCase), markup);
        Assert.DoesNotContain("indicador", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("penalización", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nivel", markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Prueba6_H27_AdministrarSesion_ConInscritos403_NoFallaYRenderizaAccionesOperativas()
    {
        // H-27: Si la consulta secundaria de inscritos falla con 403, la pantalla debe ser resiliente:
        // no quedarse vacía con error y permitir al Administrador operar la sesión (cancelar, reprogramar, inscribir).
        var sesionId = Guid.NewGuid();
        var adminService = new AdminSesionesServiceFalso
        {
            Detalle = new SesionAdminDetalleDto(
                sesionId,
                Guid.NewGuid(),
                "Acondicionamiento Físico",
                "Entrenador Andrés",
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                25,
                15,
                false,
                null,
                new List<ReservaAdminDetalleDto>() // Lista vacía por 403 en inscritos
            )
        };

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("Administrador"));
        Services.AddSingleton<IAdministracionSesionesClienteService>(adminService);
        Services.AddSingleton<INotasYAnunciosClienteService>(new NotasYAnunciosServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<AdministrarSesion>(p => p.Add(x => x.IdSesion, sesionId));
        var markup = cut.Markup;

        // No debe mostrar banner rojo de error general de carga
        Assert.DoesNotContain("Error al cargar la sesión", markup);
        Assert.DoesNotContain("Error del servidor al consultar la sesión", markup);

        // Debe renderizar la información de la sesión
        Assert.Contains("Acondicionamiento Físico", markup);
        Assert.Contains("Entrenador Andrés", markup);
        Assert.Contains("25 inscritos", markup);

        // Debe renderizar los tres botones de acción operativa disponibles para el Administrador
        Assert.Contains("Cancelar sesión", markup);
        Assert.Contains("Reprogramar fecha/hora", markup);
        Assert.Contains("Inscribir tercero", markup);
        Assert.Contains("Publicar aviso", markup);
    }

    [Fact]
    public void Prueba7_H27_AdministrarSesion_CasoFeliz_MuestraInscritosYAcciones()
    {
        var sesionId = Guid.NewGuid();
        var adminService = new AdminSesionesServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("Administrador"));
        Services.AddSingleton<IAdministracionSesionesClienteService>(adminService);
        Services.AddSingleton<INotasYAnunciosClienteService>(new NotasYAnunciosServiceFalso());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<AdministrarSesion>(p => p.Add(x => x.IdSesion, sesionId));
        var markup = cut.Markup;

        Assert.Contains("Entrenamiento Funcional", markup);
        Assert.Contains("Ana Pérez", markup);
        Assert.Contains("Luis Gómez", markup);
        Assert.Contains("Cancelar sesión", markup);
        Assert.Contains("Reprogramar fecha/hora", markup);
        Assert.Contains("Inscribir tercero", markup);
    }
}

