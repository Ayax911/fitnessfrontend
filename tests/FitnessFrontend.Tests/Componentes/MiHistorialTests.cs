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
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// RF-021: Consultar su propio historial de asistencias (Usuario)
public class MiHistorialTests : BunitContext
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
        private readonly IReadOnlyList<HistorialAsistenciaDto> _historial;
        public FakeAsistenciaService(IReadOnlyList<HistorialAsistenciaDto> historial) => _historial = historial;

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarCheckInAsync(string tokenSesion, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarIngresoGeneralAsync(string idPuntoAcceso, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>> ObtenerHistorialAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>.Correcto(_historial));

        public Task<EstadoDeCarga<SesionQRDto>> ObtenerQRDeSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    [Fact]
    public void MiHistorial_DebeRenderizarRegistrosEnBogota_YTiposDeAcceso()
    {
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("usuario@itm.edu.co", "Usuario"));

        // 2026-10-18 13:30 UTC -> 08:30 hora Bogotá
        var fechaUtc = new DateTimeOffset(2026, 10, 18, 13, 30, 0, TimeSpan.Zero);

        var lista = new List<HistorialAsistenciaDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FechaHora = fechaUtc,
                Tipo = "Sesion",
                NombreEvento = "Spinning Matutino",
                Estado = "Asistió"
            },
            new()
            {
                Id = Guid.NewGuid(),
                FechaHora = fechaUtc.AddHours(2),
                Tipo = "General",
                NombreEvento = null,
                Estado = "Ingreso OK"
            }
        };

        Services.AddSingleton<IAsistenciaClienteService>(new FakeAsistenciaService(lista));
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<MiHistorial>();
        var markup = cut.Markup;

        Assert.Contains("Spinning Matutino", markup);
        Assert.Contains("Sesión Deportiva", markup);
        Assert.Contains("Ingreso General", markup);
        Assert.Contains("08:30", markup);
    }

    [Fact]
    public void MiHistorial_SinRegistros_DebeMostrarEstadoVacio()
    {
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("usuario@itm.edu.co", "Usuario"));

        Services.AddSingleton<IAsistenciaClienteService>(new FakeAsistenciaService(new List<HistorialAsistenciaDto>()));
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<MiHistorial>();
        var markup = cut.Markup;

        Assert.Contains("No tienes asistencias registradas", markup);
    }
}
