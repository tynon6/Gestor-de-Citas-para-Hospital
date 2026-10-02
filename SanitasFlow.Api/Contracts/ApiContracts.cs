namespace SanitasFlow.Api.Contracts;

public record RegisterRequest(string Nombres, string ApellidoPaterno, string? ApellidoMaterno, int Edad, string Correo, string Password);
public record LoginRequest(string Correo, string Password);
public record UpdateProfileRequest(string Nombres, string ApellidoPaterno, string? ApellidoMaterno, int Edad);
public record CreateAppointmentRequest(string EspecialidadId, string ProcedimientoId, string Fecha, int InicioMin, string? Motivo);
public record PatientResponse(int Id, string Nombres, string ApellidoPaterno, string ApellidoMaterno, int Edad, string Correo);
public record AppointmentResponse(int Id, string Especialidad, string Procedimiento, string Fecha, int InicioMin, int FinMin, decimal Precio, string Motivo, string Estado);
