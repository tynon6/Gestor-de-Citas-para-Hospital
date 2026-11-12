namespace SanitasFlow.Api.Models;

public class CitaConfirmada
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public int InicioMin { get; set; }
    public int FinMin { get; set; }
    public DateTime ConfirmadaEnUtc { get; set; } = DateTime.UtcNow;
}
