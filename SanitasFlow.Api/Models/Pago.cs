namespace SanitasFlow.Api.Models;

public class Pago
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public decimal Monto { get; set; }
    public string Metodo { get; set; } = "Simulado";
    public string Estado { get; set; } = "Aprobado";
    public string ReferenciaExterna { get; set; } = string.Empty;
    public DateTime CreadoEnUtc { get; set; } = DateTime.UtcNow;
    public Cita Cita { get; set; } = null!;
}
