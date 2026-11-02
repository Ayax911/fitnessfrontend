using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.Paginas.Reportes;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Reportes;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// Observación de uso (2026-11-02): las pestañas Ocupación, Inasistencias, Horas pico y Uso de plataforma se quedaban
// "cargando" para siempre con la API real, porque la carga no se esperaba y Blazor no volvía a pintar al terminar.
// Estos servicios falsos responden de forma ASÍNCRONA (como el HTTP real); con respuestas ya completadas el fallo no se ve.
public class ReportesPestanasAsincronasTests : BunitContext
{
    private class Autenticacion : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "Admin ITM"), new Claim(ClaimTypes.Role, "Administrador")
            }, "TestAuth"))));
    }

    private class ReportesLentos : IReportesClienteService
    {
        private static async Task<EstadoDeCarga<T>> Lento<T>(T valor)
        {
            await Task.Delay(60);
            return EstadoDeCarga<T>.Correcto(valor);
        }

        public Task<EstadoDeCarga<IReadOnlyList<ReporteAsistenciaSesionDto>>> ConsultarAsistenciaPorSesionAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Lento<IReadOnlyList<ReporteAsistenciaSesionDto>>(new List<ReporteAsistenciaSesionDto> { new(Guid.NewGuid(), "Spinning Matutino", DateTimeOffset.UtcNow, 20, 18, 16, 2, 88.8) });

        public Task<EstadoDeCarga<IReadOnlyList<ReporteOcupacionDto>>> ConsultarOcupacionAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Lento<IReadOnlyList<ReporteOcupacionDto>>(new List<ReporteOcupacionDto> { new("Yoga Vespertino", new DateTime(2026, 11, 2), 30, 21, 70.0) });

        public Task<EstadoDeCarga<IReadOnlyList<ReporteInasistenciasEventoDto>>> ConsultarInasistenciasAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Lento<IReadOnlyList<ReporteInasistenciasEventoDto>>(new List<ReporteInasistenciasEventoDto> { new("Pilates Nocturno", 40, 6, 15.0) });

        public Task<EstadoDeCarga<IReadOnlyList<ReporteHorasPicoDto>>> ConsultarHorasPicoAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Lento<IReadOnlyList<ReporteHorasPicoDto>>(new List<ReporteHorasPicoDto> { new("Lunes", 19, 75) });

        public Task<EstadoDeCarga<ReporteUsoPlataformaDto>> ConsultarUsoPlataformaAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Lento(new ReporteUsoPlataformaDto(10, 50, 123, 5));

        public Task<EstadoDeCarga<string>> DescargarCsvAsync(string tipo, DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
            Lento("﻿a;b");
    }

    private IRenderedComponent<Reportes> Preparar()
    {
        Services.AddSingleton<AuthenticationStateProvider>(new Autenticacion());
        Services.AddSingleton<IReportesClienteService>(new ReportesLentos());
        Services.AddSingleton<IZonaHorariaService>(new ZonaHorariaService());
        var cut = Render<Reportes>();
        cut.WaitForAssertion(() => Assert.Contains("Spinning Matutino", cut.Markup));
        return cut;
    }

    private static void IrAPestana(IRenderedComponent<Reportes> cut, string texto) =>
        cut.FindAll("button[role='tab']").First(b => b.TextContent.Contains(texto)).Click();

    [Theory]
    [InlineData("Ocupación", "Yoga Vespertino")]
    [InlineData("Inasistencias", "Pilates Nocturno")]
    [InlineData("Horas pico", "75 personas")]
    [InlineData("Uso de plataforma", "123")]
    public void CadaPestana_MuestraSusDatos_CuandoLaRespuestaLlegaDespues(string pestana, string esperado)
    {
        var cut = Preparar();

        IrAPestana(cut, pestana);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(esperado, cut.Markup);
            Assert.Empty(cut.FindAll(".itm-esq")); // ya no queda el esqueleto de carga
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void AtajosDeFecha_TambienTerminanDeCargar()
    {
        var cut = Preparar();

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Últimos 7 días").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Spinning Matutino", cut.Markup);
            Assert.Empty(cut.FindAll(".itm-esq"));
        }, TimeSpan.FromSeconds(3));
    }

    // Observación de uso (2026-11-02): al cambiar de pestaña la pantalla se subía al inicio. La región que cambia debe
    // conservar su altura y mostrar un esqueleto con forma de tabla (no reemplazar la página entera).
    [Fact]
    public void AlCambiarDePestana_LaRegionEstableConservaSuAlturaYMuestraEsqueleto()
    {
        var cut = Preparar();

        IrAPestana(cut, "Ocupación");

        Assert.NotEmpty(cut.FindAll(".itm-area-estable"));
        cut.WaitForAssertion(() => Assert.Contains("Yoga Vespertino", cut.Markup), TimeSpan.FromSeconds(3));
        Assert.NotEmpty(cut.FindAll(".itm-area-estable"));
    }
}
