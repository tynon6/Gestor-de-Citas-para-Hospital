namespace SanitasFlow.Api.Contracts;

public record RegisterRequest(string Nombres, string ApellidoPaterno, string? ApellidoMaterno, int Edad, string Correo, string NombreUsuario, string Password);
public record LoginRequest(string Identificador, string Password);
public record UpdateProfileRequest(string Nombres, string ApellidoPaterno, string? ApellidoMaterno, int Edad);
public record CreateAppointmentRequest(string EspecialidadId, string ProcedimientoId, string Fecha, int InicioMin, string? Motivo);
public record PatientResponse(int Id, string Nombres, string ApellidoPaterno, string ApellidoMaterno, int Edad, string Correo, string NombreUsuario = "", string Rol = "Paciente");
public record StaffResponse(int Id, string Nombres, string NombreUsuario, string Correo, string Rol);
public record AltaMedicoRequest(string Nombre, string ApellidoPaterno, string ApellidoMaterno, int Edad, string Sexo, string EspecialidadId, string Correo, string NombreUsuario, string Password);
public record AppointmentResponse(int Id, string EspecialidadId, string ProcedimientoId, string Especialidad, string Procedimiento, string Fecha, int InicioMin, int FinMin, decimal Precio, string Motivo, string Estado);
public record CancellationQuoteResponse(bool PuedeCancelar, int PorcentajeReembolso, decimal MontoReembolso, decimal MontoPagado, bool ReservadaHoy, string Mensaje);
public record CancelAppointmentRequest(string MotivoCancelacion, bool DeseaReagendar);
public record CreateDonationRequest(decimal Monto);
public record DonationResponse(int Id, decimal Monto, string Moneda, string Estado, DateTime CreadoEnUtc);
