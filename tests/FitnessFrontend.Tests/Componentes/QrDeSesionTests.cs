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

// RF-012: Visualizar código QR dinámico de asistencia (Entrenador)
public class QrDeSesionTests : BunitContext
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
        private readonly SesionQRDto _dto;
        public FakeAsistenciaService(SesionQRDto dto) => _dto = dto;

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarCheckInAsync(string tokenSesion, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<CheckInResultadoDto>> RegistrarIngresoGeneralAsync(string idPuntoAcceso, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<IReadOnlyList<HistorialAsistenciaDto>>> ObtenerHistorialAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<EstadoDeCarga<SesionQRDto>> ObtenerQRDeSesionAsync(Guid sesionId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<SesionQRDto>.Correcto(_dto));
    }

    [Fact]
    public void QrDeSesion_DebeRenderizarSvgYVentanaEnBogota()
    {
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider("entrenador@itm.edu.co", "Entrenador"));

        // 2026-10-15 15:00 UTC -> 10:00 hora Bogotá (UTC-5)
        var inicioUtc = new DateTimeOffset(2026, 10, 15, 15, 0, 0, TimeSpan.Zero);
        var finUtc = new DateTimeOffset(2026, 10, 15, 16, 0, 0, TimeSpan.Zero);

        var dto = new SesionQRDto
        {
            SesionEventoId = Guid.NewGuid(),
            NombreEvento = "Crossfit Universitario",
            TokenQR = "token-qr-prueba-12345",
            VentanaAsistenciaInicio = inicioUtc,
            VentanaAsistenciaFin = finUtc
        };

        Services.AddSingleton<IAsistenciaClienteService>(new FakeAsistenciaService(dto));
        Services.AddSingleton<IZonaHorariaService, ZonaHorariaService>();

        var cut = Render<QrDeSesion>(parameters => parameters
            .Add(p => p.IdSesion, dto.SesionEventoId.Value));

        var markup = cut.Markup;

        // Renderiza el nombre del evento
        Assert.Contains("Crossfit Universitario", markup);

        // Renderiza el componente CodigoQr (que genera SVG con QRCoder)
        Assert.Contains("<svg", markup);

        // Horarios formateados en Bogotá (10:00 y 11:00)
        Assert.Contains("10:00", markup);
        Assert.Contains("11:00", markup);
    }
}
