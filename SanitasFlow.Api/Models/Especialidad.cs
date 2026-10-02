namespace SanitasFlow.Api.Models;

public class Especialidad
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public List<Procedimiento> Procedimientos { get; set; } = [];
}
