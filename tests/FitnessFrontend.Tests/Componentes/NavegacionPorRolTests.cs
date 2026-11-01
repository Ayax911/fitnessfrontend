using Bunit;
using FitnessFrontend.Layout;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// Patrón 4 (docs/arquitectura/04-patrones-de-diseno.md): Strategy de navegación por rol
public class NavegacionPorRolTests : BunitContext
{
    [Fact]
    public void NavMenuUsuario_DebeContenerSoloRutasDeUsuario()
    {
        var cut = Render<NavMenuUsuario>();
        var markup = cut.Markup;

        Assert.Contains("href=\"eventos\"", markup);
        Assert.Contains("href=\"reservas\"", markup);
        Assert.Contains("href=\"historial\"", markup);

        // No debe contener rutas de otros roles
        Assert.DoesNotContain("entrenador/actividades", markup);
        Assert.DoesNotContain("admin/eventos/crear", markup);
        Assert.DoesNotContain("admin/priorizacion", markup);
    }

    [Fact]
    public void NavMenuEntrenador_DebeContenerSoloRutasDeEntrenador()
    {
        var cut = Render<NavMenuEntrenador>();
        var markup = cut.Markup;

        Assert.Contains("href=\"entrenador/actividades\"", markup);
        Assert.Contains("href=\"eventos\"", markup);

        // No debe contener rutas de administrador
        Assert.DoesNotContain("admin/eventos/crear", markup);
        Assert.DoesNotContain("admin/priorizacion", markup);
        Assert.DoesNotContain("admin/configuracion", markup);
    }

    [Fact]
    public void NavMenuAdministrador_DebeContenerRutasDeAdministracion()
    {
        var cut = Render<NavMenuAdministrador>();
        var markup = cut.Markup;

        Assert.Contains("href=\"eventos\"", markup);
        Assert.Contains("href=\"admin/eventos/crear\"", markup);
        Assert.Contains("href=\"admin/priorizacion/usuarios\"", markup);
        Assert.Contains("href=\"admin/configuracion\"", markup);

        // No debe contener actividades exclusivas de entrenador
        Assert.DoesNotContain("entrenador/actividades", markup);
    }
}
