using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Componentes.Compartidos;
using FitnessFrontend.Servicios.Usuarios;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// RF-023, RF-007, RF-009: el administrador elige al entrenador de una lista en vez de escribir un GUID
public class SelectorEntrenadorTests : BunitContext
{
    private static readonly Guid IdAna = Guid.NewGuid();
    private static readonly Guid IdBeto = Guid.NewGuid();

    private IRenderedComponent<SelectorEntrenador> Render(Guid? valor, Action<Guid?> alCambiar)
    {
        Services.AddSingleton<IUsuariosClienteService>(new UsuariosClienteServiceFalso
        {
            Entrenadores = new List<EntrenadorDto>
            {
                new() { Id = IdAna, NombreCompleto = "Ana Entrenadora", Email = "ana@itm.edu.co" },
                new() { Id = IdBeto, NombreCompleto = "Beto Entrenador", Email = "beto@itm.edu.co" }
            }
        });

        return Render<SelectorEntrenador>(p => p
            .Add(c => c.Value, valor)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<Guid?>(this, alCambiar)));
    }

    [Fact]
    public void MuestraLosEntrenadoresPorNombre_NuncaPorGuid()
    {
        var cut = Render(null, _ => { });

        var texto = cut.Find("select").TextContent;
        Assert.Contains("Ana Entrenadora", texto);
        Assert.Contains("Beto Entrenador", texto);
        Assert.Contains("Sin entrenador asignado", texto);
        Assert.DoesNotContain(IdAna.ToString(), texto);
    }

    [Fact]
    public void ElegirUnEntrenador_NotificaSuIdentificador_Y_ElegirNinguno_Notifica_Nulo()
    {
        Guid? recibido = Guid.Empty;
        var cut = Render(null, v => recibido = v);

        cut.Find("select").Change(IdBeto.ToString());
        Assert.Equal(IdBeto, recibido);

        cut.Find("select").Change("");
        Assert.Null(recibido);
    }

    [Fact]
    public void SiElEntrenadorActualYaNoTieneElRol_SeConservaVisibleEnLugarDeBorrarloEnSilencio()
    {
        var cut = Render(Guid.NewGuid(), _ => { });

        Assert.Contains("ya no tiene rol de entrenador", cut.Find("select").TextContent);
    }
}
