namespace SanitasFlow.Api.Models
{
    public class Cita
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string EspecialidadId { get; set; } = string.Empty;
        public string ProcedimientoId { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public int InicioMin { get; set; }
        public int FinMin { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Estado { get; set; } = "Confirmada";
        public DateTime CreadaEnUtc { get; set; } = DateTime.UtcNow;
        public Paciente Paciente { get; set; } = null!;
        public Especialidad Especialidad { get; set; } = null!;
        public Procedimiento Procedimiento { get; set; } = null!;
        public List<Pago> Pagos { get; set; } = [];
    }
}
