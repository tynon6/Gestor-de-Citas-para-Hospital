namespace SanitasFlow.Api.Models;

public class CitaCancelada
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public string Estado { get; set; } = "Cancelada";
    public int MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Procedimiento { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public int InicioMin { get; set; }
    public int FinMin { get; set; }
    public string? MotivoCancelacion { get; set; }
    public DateTime CanceladaEnUtc { get; set; }
    public int PorcentajeReembolso { get; set; }
    public decimal MontoReembolso { get; set; }
}
