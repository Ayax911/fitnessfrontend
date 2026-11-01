using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente.Models;
using FitnessFrontend.Servicios.Identidad;
using FitnessFrontend.Servicios.Infraestructura;
using FitnessFrontend.Servicios.Usuarios;

namespace FitnessFrontend.Tests.Componentes;

// Doble de prueba de IUsuariosClienteService (RF-023): sin red, con resultados configurables y llamadas registradas
public class UsuariosClienteServiceFalso : IUsuariosClienteService
{
    public List<UsuarioResumenDto> Usuarios { get; set; } = new();
    public List<EntrenadorDto> Entrenadores { get; set; } = new();
    public ResultadoOperacion ResultadoRoles { get; set; } = ResultadoOperacion.Exito("ok");

    public List<(string? Busqueda, string? Rol, int Pagina)> Busquedas { get; } = new();
    public List<(Guid Id, string[] Roles)> CambiosDeRoles { get; } = new();

    public Task<EstadoDeCarga<PaginaUsuariosDto>> BuscarAsync(string? busqueda, string? rol, int pagina, int tamano, CancellationToken ct = default)
    {
        Busquedas.Add((busqueda, rol, pagina));
        return Task.FromResult(EstadoDeCarga<PaginaUsuariosDto>.Correcto(
            new PaginaUsuariosDto { Items = Usuarios, Pagina = pagina, Tamano = tamano, Total = Usuarios.Count }));
    }

    public Task<EstadoDeCarga<List<EntrenadorDto>>> ListarEntrenadoresAsync(CancellationToken ct = default) =>
        Task.FromResult(EstadoDeCarga<List<EntrenadorDto>>.Correcto(Entrenadores));

    public Task<ResultadoOperacion> ActualizarRolesAsync(Guid usuarioId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        CambiosDeRoles.Add((usuarioId, roles.OrderBy(r => r).ToArray()));
        return Task.FromResult(ResultadoRoles);
    }
}
