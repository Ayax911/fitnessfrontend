using System;
using System.Linq;
using System.Reflection;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Bloqueos;
using Xunit;

namespace FitnessFrontend.Tests.Servicios;

// Prueba obligatoria Fase 2 (docs/arquitectura/08-guia-de-implementacion.md, skill pruebas-obligatorias-fitnessfrontend):
// Ausencia de campos de priorización (RN-020) verificada por inspección directa del tipo consumido.
public class AusenciaPriorizacionRN020Tests
{
    private static readonly string[] PalabrasProhibidas = 
    {
        "prioridad",
        "priorizacion",
        "nivel",
        "confiabilidad",
        "indicador",
        "penalizado",
        "sancion"
    };

    [Fact]
    public void ReservaInscritoDto_NoDebeContenerCamposDePriorizacionNiConfiabilidad()
    {
        // VERIFICACIÓN POR INSPECCIÓN DE TIPO:
        // El tipo consumido por InscritosDeSesion.razor no debe exponer nivel ni confiabilidad
        var tipo = typeof(ReservaInscritoDto);
        var propiedades = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in propiedades)
        {
            var nombrePropiedad = prop.Name.ToLowerInvariant();
            foreach (var prohibida in PalabrasProhibidas)
            {
                Assert.False(
                    nombrePropiedad.Contains(prohibida),
                    $"Violación de RN-020: La propiedad '{prop.Name}' en '{tipo.Name}' expone información de priorización.");
            }
        }
    }

    [Fact]
    public void ReservaResumenDto_NoDebeContenerCamposDePriorizacionNiConfiabilidad()
    {
        // VERIFICACIÓN POR INSPECCIÓN DE TIPO:
        // El tipo consumido por MisReservas.razor no debe exponer nivel ni confiabilidad
        var tipo = typeof(ReservaResumenDto);
        var propiedades = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in propiedades)
        {
            var nombrePropiedad = prop.Name.ToLowerInvariant();
            foreach (var prohibida in PalabrasProhibidas)
            {
                Assert.False(
                    nombrePropiedad.Contains(prohibida),
                    $"Violación de RN-020: La propiedad '{prop.Name}' en '{tipo.Name}' expone información de priorización.");
            }
        }
    }

    [Fact]
    public void ActividadAsignadaDto_NoDebeContenerCamposDePriorizacionNiConfiabilidad()
    {
        var tipo = typeof(ActividadAsignadaDto);
        var propiedades = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in propiedades)
        {
            var nombrePropiedad = prop.Name.ToLowerInvariant();
            foreach (var prohibida in PalabrasProhibidas)
            {
                Assert.False(
                    nombrePropiedad.Contains(prohibida),
                    $"Violación de RN-020: La propiedad '{prop.Name}' en '{tipo.Name}' expone información de priorización.");
            }
        }
    }

    [Fact]
    public void HistorialAsistenciaDto_NoDebeContenerCamposDePriorizacionNiConfiabilidad()
    {
        // VERIFICACIÓN POR INSPECCIÓN DE TIPO (Fase 3, RF-021, RN-020):
        // El tipo consumido por MiHistorial.razor no debe exponer nivel ni confiabilidad
        var tipo = typeof(HistorialAsistenciaDto);
        var propiedades = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in propiedades)
        {
            var nombrePropiedad = prop.Name.ToLowerInvariant();
            foreach (var prohibida in PalabrasProhibidas)
            {
                Assert.False(
                    nombrePropiedad.Contains(prohibida),
                    $"Violación de RN-020: La propiedad '{prop.Name}' en '{tipo.Name}' expone información de priorización.");
            }
        }
    }

    [Theory]
    [InlineData(typeof(UsuarioResumenDto))]
    [InlineData(typeof(EntrenadorDto))]
    [InlineData(typeof(PaginaUsuariosDto))]
    [InlineData(typeof(PuntoAccesoDto))]
    [InlineData(typeof(EventoResumenDto))]
    [InlineData(typeof(BloqueoCalendarioDto))]
    [InlineData(typeof(FitnessFrontend.Servicios.Asistencia.InscritoAsistenciaManualDto))]
    [InlineData(typeof(FitnessFrontend.Servicios.Eventos.NotaIncidenciaDto))]
    [InlineData(typeof(FitnessFrontend.Servicios.Eventos.AnuncioSesionDto))]
    [InlineData(typeof(FitnessFrontend.Servicios.Sesiones.ReservaAdminDetalleDto))]
    [InlineData(typeof(FitnessFrontend.Servicios.Sesiones.SesionAdminDetalleDto))]
    [InlineData(typeof(FitnessFrontend.Servicios.Auditoria.RegistroAuditoriaDto))]
    public void DtosDeGestionDeUsuariosYPuntos_NoContienenCamposDePriorizacionNiConfiabilidad(Type tipo)
    {
        // RF-023, RF-024, RN-020: lo que consumen las pantallas nuevas se verifica por inspección de tipo
        foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var nombre = prop.Name.ToLowerInvariant();
            foreach (var prohibida in PalabrasProhibidas)
            {
                Assert.False(nombre.Contains(prohibida),
                    $"Violación de RN-020: la propiedad '{prop.Name}' en '{tipo.Name}' expone información de priorización.");
            }
        }
    }
}
