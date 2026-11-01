using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Priorizacion;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Priorizacion;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Prueba obligatoria Fase 5 (docs/arquitectura/08-guia-de-implementacion.md, skill pruebas-obligatorias-fitnessfrontend):
// Aislamiento por rol y manejo de 403 al navegar por URL directa a pantallas de priorización.
public class PriorizacionAislamientoRolTests : BunitContext
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

    private class FakePriorizacionService : IPriorizacionClienteService
    {
        private readonly bool _esAdmin;
        private readonly ConfiguracionPriorizacionDto? _configDto;
        private readonly int _nivel;

        public FakePriorizacionService(bool esAdmin, ConfiguracionPriorizacionDto? configDto = null, int nivel = 0)
        {
            _esAdmin = esAdmin;
            _configDto = configDto;
            _nivel = nivel;
        }

        public Task<EstadoDeCarga<IndicadorAdministradorDto>> ConsultarIndicadorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
        {
            if (!_esAdmin)
            {
                // El backend devuelve 403 cuando un no-administrador intenta consultar
                return Task.FromResult(EstadoDeCarga<IndicadorAdministradorDto>.Fallo(
                    "Acceso denegado: Se requiere rol de Administrador para consultar indicadores de priorización.",
                    "403"));
            }

            return Task.FromResult(EstadoDeCarga<IndicadorAdministradorDto>.Correcto(new IndicadorAdministradorDto
            {
                UsuarioId = usuarioId,
                Email = "estudiante.auditado@itm.edu.co",
                NombreUsuario = "Estudiante Auditado",
                Nivel = _nivel,
                RachaAsistenciasActual = 5,
                TotalAsistidas = 12,
                TotalInasistencias = 0,
                UltimoRecalculo = DateTimeOffset.UtcNow
            }));
        }

        public Task<EstadoDeCarga<ConfiguracionPriorizacionDto>> ObtenerConfiguracionAsync(CancellationToken ct = default)
        {
            if (!_esAdmin)
            {
                return Task.FromResult(EstadoDeCarga<ConfiguracionPriorizacionDto>.Fallo(
                    "Acceso denegado: Se requiere rol de Administrador para ver la configuración del sistema.",
                    "403"));
            }

            return Task.FromResult(EstadoDeCarga<ConfiguracionPriorizacionDto>.Correcto(_configDto ?? new ConfiguracionPriorizacionDto
            {
                UmbralAsistenciasPrioritario = 8,
                UmbralInasistenciasPenalizacion = 3,
                CriterioRecuperacionAsistenciasConsecutivas = 4,
                DesplazamientoNivelNormalMinutos = 120,
                DesplazamientoNivelPenalizadoMinutos = 360,
                DuracionVentanaAsistenciaMinutos = 15,
                MargenAperturaAsistenciaMinutos = 10,
                IntervaloMinimoCheckInGeneralMinutos = 45
            }));
        }

        public Task<ResultadoOperacion> ActualizarConfiguracionAsync(ActualizarConfiguracionRequest request, CancellationToken ct = default)
        {
            if (!_esAdmin)
            {
                return Task.FromResult(ResultadoOperacion.Fallo("Acceso denegado", "403"));
            }

            return Task.FromResult(ResultadoOperacion.Exito("Configuración actualizada."));
        }
    }

    [Fact]
    public void IndicadorUsuario_UsuarioSinRolAdmin_Maneja403ConMensajeYNuncaEnBlanco()
    {
        // VERIFICACIÓN OBLIGATORIA FASE 5:
        // Un usuario con rol "Usuario" accede navegando por URL directa a /admin/priorizacion/usuarios/{id}
        // El backend responde 403 y la UI muestra el mensaje de acceso denegado sin quedarse en blanco.
        var auth = new FakeAuthStateProvider("estudiante@itm.edu.co", "Usuario");
        var servicio = new FakePriorizacionService(esAdmin: false);

        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddSingleton<IPriorizacionClienteService>(servicio);
        Services.AddSingleton<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService>(new UsuariosClienteServiceFalso());
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<IndicadorUsuario>(parameters => parameters
            .Add(p => p.IdUsuario, Guid.NewGuid()));

        var markup = cut.Markup;

        // Comprobación de que no se queda en blanco y muestra el mensaje claro del 403
        Assert.NotEmpty(markup);
        Assert.Contains("Acceso denegado", markup);
        Assert.Contains("Se requiere rol de Administrador", markup);

        // No debe mostrar tarjetas con datos de priorización
        Assert.DoesNotContain("Nivel de Priorización Actual", markup);
        Assert.DoesNotContain("Racha de Asistencia", markup);
    }

    [Fact]
    public void ConfiguracionSistema_UsuarioSinRolAdmin_Maneja403ConMensaje()
    {
        var auth = new FakeAuthStateProvider("entrenador@itm.edu.co", "Entrenador");
        var servicio = new FakePriorizacionService(esAdmin: false);

        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddSingleton<IPriorizacionClienteService>(servicio);
        Services.AddSingleton<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService>(new UsuariosClienteServiceFalso());

        var cut = Render<ConfiguracionSistema>();

        var markup = cut.Markup;
        Assert.NotEmpty(markup);
        Assert.Contains("Acceso denegado", markup);
        Assert.Contains("Se requiere rol de Administrador", markup);

        // No debe renderizar el formulario de edición de parámetros
        Assert.DoesNotContain("Guardar Configuración", markup);
    }

    [Fact]
    public void ConfiguracionSistema_AMB013_CargaValoresRealesDelBackend_SinDefaultsInventados()
    {
        // AMB-013: Verifica que el formulario renderice los valores específicos provistos por el backend
        // (por ejemplo, umbral 17 y desplazamiento 480) sin sustituirlos por defaults inventados.
        var configEspecificaBackend = new ConfiguracionPriorizacionDto
        {
            UmbralAsistenciasPrioritario = 17,
            UmbralInasistenciasPenalizacion = 5,
            CriterioRecuperacionAsistenciasConsecutivas = 6,
            DesplazamientoNivelNormalMinutos = 180,
            DesplazamientoNivelPenalizadoMinutos = 480,
            DuracionVentanaAsistenciaMinutos = 25,
            MargenAperturaAsistenciaMinutos = 15,
            IntervaloMinimoCheckInGeneralMinutos = 60
        };

        var auth = new FakeAuthStateProvider("admin@itm.edu.co", "Administrador");
        var servicio = new FakePriorizacionService(esAdmin: true, configDto: configEspecificaBackend);

        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddSingleton<IPriorizacionClienteService>(servicio);
        Services.AddSingleton<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService>(new UsuariosClienteServiceFalso());

        var cut = Render<ConfiguracionSistema>();

        // Verifica que los inputs tengan los valores exactos devueltos por el backend
        var inputUmbral = cut.Find("#umbralPrioritario");
        Assert.Equal("17", inputUmbral.GetAttribute("value"));

        var inputDespPenalizado = cut.Find("#despPenalizado");
        Assert.Equal("480", inputDespPenalizado.GetAttribute("value"));

        var inputIntervalo = cut.Find("#intervaloGeneral");
        Assert.Equal("60", inputIntervalo.GetAttribute("value"));
    }

    [Theory]
    [InlineData(0, "Nuevo")]
    [InlineData(1, "Prioritario")]
    [InlineData(2, "Normal")]
    [InlineData(3, "Penalizado")]
    public void IndicadorUsuario_B03_MapeaCorrectamenteLosCuatroNivelesDelEnum(int nivelBackend, string textoEsperado)
    {
        // B-03: Verifica que el switch de IndicadorUsuario mapee exactamente los 4 casos del enum del backend:
        // Nuevo=0, Prioritario=1, Normal=2, Penalizado=3
        var auth = new FakeAuthStateProvider("admin@itm.edu.co", "Administrador");
        var servicio = new FakePriorizacionService(esAdmin: true, nivel: nivelBackend);

        Services.AddSingleton<AuthenticationStateProvider>(auth);
        Services.AddSingleton<IPriorizacionClienteService>(servicio);
        Services.AddSingleton<FitnessFrontend.Servicios.Usuarios.IUsuariosClienteService>(new UsuariosClienteServiceFalso());
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<IndicadorUsuario>(parameters => parameters
            .Add(p => p.IdUsuario, Guid.NewGuid()));

        var markup = cut.Markup;
        Assert.Contains(textoEsperado, markup);
    }
}
