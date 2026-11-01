using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Asistencia.Administrar;
using FitnessFrontend.Servicios.Asistencia;
using FitnessFrontend.Servicios.Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// RF-024, RN-005: puntos de acceso con QR fijo
public class PuntosAccesoTests : BunitContext
{
    private static readonly Guid IdRecepcion = Guid.NewGuid();
    private const string TokenRecepcion = "tokenFijoDeRecepcion_abc123";

    private class PuntosFalso : IPuntosAccesoClienteService
    {
        public List<PuntoAccesoDto> Puntos { get; } = new()
        {
            new() { Id = IdRecepcion, Nombre = "Recepción", TokenQR = TokenRecepcion, Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Torniquete viejo", TokenQR = "tokenViejo", Activo = false }
        };
        public List<string> Creados { get; } = new();
        public List<(Guid Id, string Nombre, bool Activo)> Actualizados { get; } = new();
        public string? ErrorAlCrear { get; set; }

        public Task<EstadoDeCarga<List<PuntoAccesoDto>>> ListarAsync(CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<List<PuntoAccesoDto>>.Correcto(Puntos.ToList()));

        public Task<EstadoDeCarga<PuntoAccesoDto>> CrearAsync(string nombre, CancellationToken ct = default)
        {
            if (ErrorAlCrear != null) return Task.FromResult(EstadoDeCarga<PuntoAccesoDto>.Fallo(ErrorAlCrear, "NombreDuplicado"));
            Creados.Add(nombre);
            var nuevo = new PuntoAccesoDto { Id = Guid.NewGuid(), Nombre = nombre, TokenQR = "tokenNuevo", Activo = true };
            Puntos.Add(nuevo);
            return Task.FromResult(EstadoDeCarga<PuntoAccesoDto>.Correcto(nuevo));
        }

        public Task<EstadoDeCarga<PuntoAccesoDto>> ActualizarAsync(Guid id, string nombre, bool activo, CancellationToken ct = default)
        {
            Actualizados.Add((id, nombre, activo));
            var p = Puntos.First(x => x.Id == id);
            p.Nombre = nombre; p.Activo = activo;
            return Task.FromResult(EstadoDeCarga<PuntoAccesoDto>.Correcto(p));
        }

        public Task<EstadoDeCarga<QrPuntoAccesoDto>> ObtenerQrActualAsync(Guid puntoAccesoId, CancellationToken ct = default) =>
            Task.FromResult(EstadoDeCarga<QrPuntoAccesoDto>.Correcto(new QrPuntoAccesoDto("token_actual", DateTimeOffset.UtcNow.AddSeconds(15), 15, "Recepción")));
    }

    private (PuntosFalso, IRenderedComponent<PuntosAcceso>) Preparar()
    {
        var falso = new PuntosFalso();
        Services.AddSingleton<IPuntosAccesoClienteService>(falso);
        return (falso, Render<PuntosAcceso>());
    }

    private static void Clic(IRenderedComponent<PuntosAcceso> cut, string texto, int indice = 0) =>
        cut.FindAll("button").Where(b => b.TextContent.Trim() == texto).ElementAt(indice).Click();

    [Fact]
    public void ListaLosPuntosConSuEstado()
    {
        var (_, cut) = Preparar();

        Assert.Contains("Recepción", cut.Markup);
        Assert.Contains("Activo", cut.Markup);
        Assert.Contains("Desactivado", cut.Markup);
    }

    [Fact]
    public void AbrirPantalla_ContieneEnlaceConIdDelPunto_Y_TargetBlank()
    {
        var (_, cut) = Preparar();

        var enlace = cut.FindAll("a").FirstOrDefault(a => a.TextContent.Trim() == "Abrir pantalla");
        Assert.NotNull(enlace);
        Assert.Equal($"/admin/puntos-acceso/{IdRecepcion}/pantalla", enlace.GetAttribute("href"));
        Assert.Equal("_blank", enlace.GetAttribute("target"));
    }

    [Fact]
    public void Crear_EnviaElNombre_Y_AgregaElPuntoALaLista()
    {
        var (falso, cut) = Preparar();

        cut.Find("#nombrePuntoNuevo").Input("Gimnasio sur");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Gimnasio sur", cut.Markup));
        Assert.Equal("Gimnasio sur", Assert.Single(falso.Creados));
    }

    [Fact]
    public void Crear_ConNombreDuplicado_MuestraElMensajeDelBackend()
    {
        var (falso, cut) = Preparar();
        falso.ErrorAlCrear = "Ya existe un punto de acceso con ese nombre.";

        cut.Find("#nombrePuntoNuevo").Input("Recepción");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Ya existe un punto de acceso con ese nombre.", cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public void Desactivar_EnviaActivoFalse_ConservandoElNombre()
    {
        var (falso, cut) = Preparar();

        Clic(cut, "Desactivar");

        var cambio = Assert.Single(falso.Actualizados);
        Assert.Equal((IdRecepcion, "Recepción", false), cambio);
    }
}
