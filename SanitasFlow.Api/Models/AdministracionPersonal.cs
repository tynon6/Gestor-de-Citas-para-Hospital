namespace SanitasFlow.Api.Models;

public class CuentaMaestra
{
    public int Id { get; set; } = 1;
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public class SolicitudBajaMedico
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;
    public int EnfermeroId { get; set; }
    public string EnfermeroNombre { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public DateTime CreadaEnUtc { get; set; } = DateTime.UtcNow;
    public int? RevisadaPorCuentaId { get; set; }
    public DateTime? RevisadaEnUtc { get; set; }
    public string? NotaRevision { get; set; }
}

public class MovimientoPersonal
{
    public int Id { get; set; }
    public string ActorRol { get; set; } = string.Empty;
    public int? ActorId { get; set; }
    public string ActorUsuario { get; set; } = string.Empty;
    public string ActorNombre { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string DestinoTipo { get; set; } = string.Empty;
    public int? DestinoId { get; set; }
    public string DestinoNombre { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}

public class MedicoBaja
{
    public int Id { get; set; }
    public int MedicoOriginalId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string EspecialidadId { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string SolicitadoPor { get; set; } = string.Empty;
    public string AprobadoPor { get; set; } = string.Empty;
    public DateTime DadoDeBajaEnUtc { get; set; } = DateTime.UtcNow;
}

public class MedicoBajaCita
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Procedimiento { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public int InicioMin { get; set; }
    public int FinMin { get; set; }
    public string MedicoAnterior { get; set; } = string.Empty;
    public string? MedicoDestino { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public int PorcentajeReembolso { get; set; }
    public decimal MontoReembolso { get; set; }
    public DateTime ProcesadaEnUtc { get; set; } = DateTime.UtcNow;
}
