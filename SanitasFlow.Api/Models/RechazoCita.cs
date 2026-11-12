namespace SanitasFlow.Api.Models;

public class RechazoCita
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public int MedicoId { get; set; }
    public DateTime RechazadaEnUtc { get; set; } = DateTime.UtcNow;
}
