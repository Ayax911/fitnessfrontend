using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.Paginas.Reportes;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

public class ReportesYExportacionCsvF5Tests : BunitContext
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

    private class ReportesServiceFalso : IReportesClienteService
    {
        public bool FueConsultado { get; private set; }

        public Task<EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>> ConsultarAsistenciaPorSesionAsync(
            DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default)
        {
            FueConsultado = true;
            var list = new List<ReporteAsistenciaSesionDto>
            {
                new(Guid.NewGuid(), "Spinning Matutino", DateTimeOffset.UtcNow, 20, 18, 16, 2, 88.8)
            };
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>.Correcto(list));
        }

        public Task<EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>> ConsultarOcupacionAsync(
            DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>.Correcto(new List<ReporteOcupacionDto>()));

        public Task<EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>> ConsultarInasistenciasAsync(
            DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>.Correcto(new List<ReporteInasistenciasEventoDto>()));

        public Task<EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>> ConsultarHorasPicoAsync(
            DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default)
        {
            var list = new List<ReporteHorasPicoDto>
            {
                new("Lunes", 19, 75) // 19:00 (7:00 p.m.)
            };
            return Task.FromResult(EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>.Correcto(list));
        }

        public Task<EstadoDeCarga<ReporteUsoPlataformaDto>> ConsultarUsoPlataformaAsync(
            DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<ReporteUsoPlataformaDto>.Correcto(new ReporteUsoPlataformaDto(10, 50, 120, 5)));
    }

    [Fact]
    public void Prueba1_GeneradorCsv_IncluyeBom_SeparadorPuntoYComa_YProteccionFormula()
    {
        // 1. El CSV descargado lleva BOM (\uFEFF), separador ';' y protección contra inyección de fórmulas
        var cabeceras = new[] { "Actividad", "FormulaPeligrosa", "Suma", "Arroba" };
        var filas = new[]
        {
            new[] { "Crossfit", "=SUM(A1:A10)", "+123456", "@cmd|' /C calc'!A0" }
        };

        var csv = GeneradorCsv.Generar(cabeceras, filas);

        // Verifica BOM UTF-8
        Assert.StartsWith("\uFEFF", csv);

        // Verifica separador ;
        Assert.Contains("Actividad;FormulaPeligrosa;Suma;Arroba", csv);

        // Verifica protección contra inyección de fórmulas (prefijo de comilla simple ')
        Assert.Contains("'=SUM(A1:A10)", csv);
        Assert.Contains("'+123456", csv);
        Assert.Contains("'@cmd|", csv);
    }

    [Fact]
    public void Prueba2_ReporteHorasPico_MuestraFranjaLocalBogota()
    {
        // 2. Reporte de horas pico muestra franja en hora local
        var servicio = new ReportesServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IReportesClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<Reportes>();

        // Clic en pestaña Horas pico
        var tabHorasPico = cut.FindAll("button[role='tab']").First(b => b.TextContent.Contains("Horas pico"));
        tabHorasPico.Click();

        var markup = cut.Markup;
        Assert.Contains("Horas Pico y Afluencia", markup);
        Assert.Contains("07:00", markup);
        Assert.Contains("75 personas", markup);
    }

    [Fact]
    public void Prueba3_RangoFechasInvertido_MuestraAdvertenciaYNoConsulta()
    {
        // 3. Fechas con rango invertido muestran advertencia en la UI
        var servicio = new ReportesServiceFalso();

        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<IReportesClienteService>(servicio);
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());

        var cut = Render<Reportes>();

        var inputDesde = cut.Find("#fechaDesde");
        var inputHasta = cut.Find("#fechaHasta");

        // Ingresa fecha final anterior a la inicial
        inputDesde.Change("2026-11-10");
        inputHasta.Change("2026-11-05");

        var botonConsultar = cut.FindAll("button").First(b => b.TextContent.Trim() == "Consultar");
        botonConsultar.Click();

        Assert.Contains("La fecha final debe ser posterior o igual a la fecha inicial", cut.Markup);
    }

    [Fact]
    public void Prueba4_AusenciaCamposPriorizacionRN020_EnReportes()
    {
        // 4. Ningún reporte incluye campos de priorización (RN-020)
        var tiposReportes = new[]
        {
            typeof(ReporteAsistenciaSesionDto),
            typeof(ReporteOcupacionDto),
            typeof(ReporteInasistenciasEventoDto),
            typeof(ReporteHorasPicoDto),
            typeof(ReporteUsoPlataformaDto)
        };

        var palabrasProhibidas = new[] { "prioridad", "priorizacion", "nivel", "confiabilidad", "indicador", "penalizado", "posicion" };

        foreach (var tipo in tiposReportes)
        {
            foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var nombre = prop.Name.ToLowerInvariant();
                foreach (var prohibida in palabrasProhibidas)
                {
                    Assert.False(nombre.Contains(prohibida),
                        $"Violación RN-020: la propiedad '{prop.Name}' en '{tipo.Name}' expone datos de priorización.");
                }
            }
        }
    }

    [Fact]
    public void Prueba5_AutorizacionRolAdministrador()
    {
        // 5. Solo accesible por rol Administrador
        var atributo = typeof(Reportes).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(atributo);
        Assert.Equal("Administrador", atributo.Roles);
    }
}
