namespace FitnessFrontend.ApiCliente.Models;

// Extensión para compatibilidad sin editar ApiCliente/ directamente
public partial class ReservaResumenDto
{
    public string? MotivoCancelacion { get => MotivoCancelacionSesion; set => MotivoCancelacionSesion = value; }
    public string? OrigenCancelacion { get; set; }
}
