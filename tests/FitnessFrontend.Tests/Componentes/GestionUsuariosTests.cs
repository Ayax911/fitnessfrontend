using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Usuarios;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Usuarios;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// RF-023: gestión de usuarios y roles (Administrador)
public class GestionUsuariosTests : BunitContext
{
    private static readonly Guid IdAna = Guid.NewGuid();

    private (UsuariosClienteServiceFalso, IRenderedComponent<GestionUsuarios>) Preparar()
    {
        var falso = new UsuariosClienteServiceFalso
        {
            Usuarios = new List<UsuarioResumenDto>
            {
                new() { Id = IdAna, NombreCompleto = "Ana Pérez", Email = "ana@itm.edu.co", TipoUsuario = TipoUsuario.Estudiante, Roles = new List<string> { "Usuario" } },
                new() { Id = Guid.NewGuid(), NombreCompleto = "Beto Admin", Email = "beto@itm.edu.co", Roles = new List<string> { "Administrador" } }
            }
        };
        Services.AddSingleton<IUsuariosClienteService>(falso);
        return (falso, Render<GestionUsuarios>());
    }

    [Fact]
    public void ListaLosUsuariosConSusRoles_Y_NoMuestraDatosDePriorizacion()
    {
        var (falso, cut) = Preparar();

        Assert.Contains("Ana Pérez", cut.Markup);
        Assert.Contains("beto@itm.edu.co", cut.Markup);
        Assert.Contains("Administrador", cut.Markup);
        Assert.Single(falso.Busquedas);   // carga inicial
        Assert.DoesNotContain("Penalizado", cut.Markup);
        Assert.DoesNotContain("Prioritario", cut.Markup);
    }

    [Fact]
    public void Buscar_EnviaTextoYFiltroDeRol_ReiniciandoEnLaPrimeraPagina()
    {
        var (falso, cut) = Preparar();

        cut.Find("#busquedaUsuario").Input("maria");
        cut.Find("#filtroRol").Change("Entrenador");
        cut.Find("form").Submit();

        Assert.Equal(("maria", "Entrenador", 1), falso.Busquedas.Last());
    }

    [Fact]
    public void EditarRoles_AgregarEntrenador_EnviaElConjuntoCompletoDeRoles()
    {
        var (falso, cut) = Preparar();

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Editar roles").Click();
        Assert.True(cut.Find($"#rol-{IdAna}-Usuario").HasAttribute("checked"));   // refleja los roles actuales
        cut.Find($"#rol-{IdAna}-Entrenador").Change(true);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        var cambio = Assert.Single(falso.CambiosDeRoles);
        Assert.Equal(IdAna, cambio.Id);
        Assert.Equal(new[] { "Entrenador", "Usuario" }, cambio.Roles);
    }

    [Fact]
    public void EditarRoles_ElRechazoDelBackend_SeMuestraTalCual_SinInventarTexto()
    {
        var (falso, cut) = Preparar();
        const string mensajeBackend = "No se puede retirar el rol al último administrador del sistema.";
        falso.ResultadoRoles = ResultadoOperacion.Fallo(mensajeBackend, "UltimoAdministrador");

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Editar roles").Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        cut.WaitForAssertion(() => Assert.Contains(mensajeBackend, cut.Find("[role=alert]").TextContent));
    }
}
