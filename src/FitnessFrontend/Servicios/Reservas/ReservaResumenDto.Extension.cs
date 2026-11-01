using System;

namespace FitnessFrontend.ApiCliente.Models;

// Extensión para RF-028 (sesión cancelada lado usuario) sin editar ApiCliente/ directamente
public partial class ReservaResumenDto
{
    public bool? SesionCancelada { get; set; }
    public string? MotivoCancelacion { get; set; }
    public string? OrigenCancelacion { get; set; }
}
