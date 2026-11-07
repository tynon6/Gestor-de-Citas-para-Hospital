namespace SanitasFlow.Api.Models;

public class Donativo
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "MXN";
    public string Estado { get; set; } = "Registrado";
    public DateTime CreadoEnUtc { get; set; } = DateTime.UtcNow;
}
