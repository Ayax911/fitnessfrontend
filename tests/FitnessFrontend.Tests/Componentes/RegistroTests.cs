using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Paginas.Identidad;
using FitnessFrontend.Servicios.Identidad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessFrontend.Tests.Componentes;

// RF-025, RNF-011 (Ley 1581): el registro exige perfil y autorización expresa de tratamiento de datos
public class RegistroTests : BunitContext
{
    private class AutenticacionFalsa : IAutenticacionService
    {
        public (string Email, string Nombre, TipoUsuario Tipo, bool Acepta)? Llamada { get; private set; }

        public Task<ResultadoOperacion> RegistrarAsync(string email, string password, string nombreCompleto, TipoUsuario tipoUsuario, bool aceptaTratamientoDatos)
        {
            Llamada = (email, nombreCompleto, tipoUsuario, aceptaTratamientoDatos);
            return Task.FromResult(ResultadoOperacion.Exito("ok"));
        }

        public Task<ResultadoOperacion> IniciarSesionAsync(string email, string password) => throw new NotImplementedException();
        public Task CerrarSesionAsync() => Task.CompletedTask;
        public Task<string?> ObtenerTokenAsync() => Task.FromResult<string?>(null);
        public Task<UsuarioSesion?> ObtenerUsuarioActualAsync() => Task.FromResult<UsuarioSesion?>(null);
        public Task LimpiarSesionAsync() => Task.CompletedTask;
    }

    private IRenderedComponent<Registro> Llenar(AutenticacionFalsa auth, bool autoriza)
    {
        Services.AddSingleton<IAutenticacionService>(auth);
        var cut = Render<Registro>();
        cut.Find("#regNombreInput").Change("Ana Pérez");
        cut.Find("#regTipoInput").Change("Profesor");
        cut.Find("#regEmailInput").Change("ana@itm.edu.co");
        cut.Find("#regPasswordInput").Change("Passw0rd!");
        cut.Find("#regConfirmPasswordInput").Change("Passw0rd!");
        if (autoriza) cut.Find("#regAutorizacionInput").Change(true);
        return cut;
    }

    [Fact]
    public void Registro_SinAceptarTratamientoDeDatos_NoEnviaYMuestraElMotivo()
    {
        var auth = new AutenticacionFalsa();
        var cut = Llenar(auth, autoriza: false);

        cut.Find("form").Submit();

        Assert.Null(auth.Llamada);
        Assert.Contains("Debe autorizar el tratamiento de sus datos personales", cut.Markup);
    }

    [Fact]
    public void Registro_Completo_EnviaPerfilYAutorizacionAlServicio()
    {
        var auth = new AutenticacionFalsa();
        var cut = Llenar(auth, autoriza: true);

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.NotNull(auth.Llamada));
        Assert.Equal(("ana@itm.edu.co", "Ana Pérez", TipoUsuario.Profesor, true), auth.Llamada!.Value);
    }

    [Fact]
    public void Registro_OfreceLosCuatroTiposDeUsuario_YLaPistaDeContrasenaIncluyeSimbolo()
    {
        Services.AddSingleton<IAutenticacionService>(new AutenticacionFalsa());
        var cut = Render<Registro>();

        var opciones = cut.FindAll("#regTipoInput option").Select(o => o.TextContent.Trim()).ToArray();

        Assert.Contains("Estudiante", opciones);
        Assert.Contains("Profesor", opciones);
        Assert.Contains("Externo", opciones);
        Assert.Contains("Contratista", opciones);
        Assert.Contains("símbolo", cut.Markup);   // la regla real de Identity exige un carácter no alfanumérico
    }
}
