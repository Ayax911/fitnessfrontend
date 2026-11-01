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
        Assert.DoesNotContain("admin/usuarios", markup);
        Assert.DoesNotContain("admin/puntos-acceso", markup);
        Assert.DoesNotContain("admin/auditoria", markup);
        Assert.DoesNotContain("admin/reportes", markup);
        Assert.DoesNotContain("admin/retroalimentacion", markup);
        Assert.DoesNotContain("admin/calendario", markup);
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
        Assert.DoesNotContain("admin/usuarios", markup);
        Assert.DoesNotContain("admin/puntos-acceso", markup);
        Assert.DoesNotContain("admin/auditoria", markup);
        Assert.DoesNotContain("admin/reportes", markup);
        Assert.DoesNotContain("admin/retroalimentacion", markup);
        Assert.DoesNotContain("admin/calendario", markup);
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
        Assert.Contains("href=\"admin/usuarios\"", markup);        // RF-023
        Assert.Contains("href=\"admin/puntos-acceso\"", markup);   // RF-024
        Assert.Contains("href=\"admin/auditoria\"", markup);       // RF-032
        Assert.Contains("href=\"admin/reportes\"", markup);        // RF-033
        Assert.Contains("href=\"admin/retroalimentacion\"", markup);// RF-038
        Assert.Contains("href=\"admin/calendario\"", markup);      // RF-031

        // No debe contener actividades exclusivas de entrenador
        Assert.DoesNotContain("entrenador/actividades", markup);
    }
}
