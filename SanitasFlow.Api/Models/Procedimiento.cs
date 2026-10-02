namespace SanitasFlow.Api.Models;

public class Procedimiento
{
    public string Id { get; set; } = string.Empty;
    public string EspecialidadId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionMin { get; set; }
    public decimal Precio { get; set; }
    public Especialidad Especialidad { get; set; } = null!;
}
