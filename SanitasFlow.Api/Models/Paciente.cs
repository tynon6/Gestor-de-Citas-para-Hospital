namespace SanitasFlow.Api.Models;

public class Paciente
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreadoEnUtc { get; set; } = DateTime.UtcNow;
    public List<Cita> Citas { get; set; } = [];
}
