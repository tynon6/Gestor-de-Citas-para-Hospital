using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;
using SanitasFlow.Api.Services;

namespace SanitasFlow.Api.Controllers;

public record BajaRequest(string Motivo);
public record RevisionBajaRequest(string? Nota);

[ApiController]
[Route("api/maestra")]
[Authorize(Policy = "MasterOnly")]
public class MaestraController(AppDbContext context, DoctorOffboardingService offboarding) : ControllerBase
{
    [HttpGet("panel")]
    public async Task<IActionResult> Panel(CancellationToken ct)
    {
        var doctors = await context.Medicos.AsNoTracking()
            .OrderBy(m => m.EspecialidadId).ThenBy(m => m.Nombre)
            .Select(m => new { m.Id, m.Nombre, m.ApellidoPaterno, m.ApellidoMaterno, m.Edad, m.Sexo, m.Correo, m.NombreUsuario, m.EspecialidadId, m.Activo })
            .ToListAsync(ct);
        var specialtyNames = await context.Especialidades.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Nombre, ct);
        var doctorRows = doctors.Select(m => new { m.Id, m.Nombre, m.ApellidoPaterno, m.ApellidoMaterno, m.Edad, m.Sexo, m.Correo, m.NombreUsuario, m.EspecialidadId, Especialidad = specialtyNames.GetValueOrDefault(m.EspecialidadId, m.EspecialidadId), m.Activo });
        var nurses = await context.Enfermeros.AsNoTracking().OrderBy(n => n.Nombre)
            .Select(n => new { n.Id, n.Nombre, n.ApellidoPaterno, n.ApellidoMaterno, n.Edad, n.Correo, n.NombreUsuario, n.Activo }).ToListAsync(ct);
        var requests = await context.SolicitudesBajaMedico.AsNoTracking().OrderByDescending(r => r.CreadaEnUtc)
            .Select(r => new { r.Id, r.MedicoId, r.MedicoNombre, r.EnfermeroNombre, r.Motivo, r.Estado, r.CreadaEnUtc, r.RevisadaEnUtc, r.NotaRevision }).ToListAsync(ct);
        var movements = await context.MovimientosPersonal.AsNoTracking().OrderByDescending(m => m.FechaUtc).Take(300)
            .Select(m => new { m.Id, m.ActorRol, m.ActorNombre, m.ActorUsuario, m.Accion, m.DestinoTipo, m.DestinoNombre, m.Detalle, m.FechaUtc }).ToListAsync(ct);
        var appointments = await context.Citas.AsNoTracking().Include(c => c.Paciente).Include(c => c.Medico).Include(c => c.Especialidad).Include(c => c.Procedimiento)
            .Where(c => c.Fecha >= DateOnly.FromDateTime(DateTime.Today) && c.Estado != "Cancelada" && c.Estado != "Rechazada")
            .OrderBy(c => c.Fecha).ThenBy(c => c.InicioMin)
            .Select(c => new { c.Id, c.Estado, c.Fecha, c.InicioMin, c.FinMin, Paciente = c.Paciente.Nombres + " " + c.Paciente.ApellidoPaterno, Especialidad = c.Especialidad.Nombre, Procedimiento = c.Procedimiento.Nombre, Medico = c.Medico == null ? "Sin asignar" : c.Medico.Nombre + " " + c.Medico.ApellidoPaterno, c.ReasignacionUrgente, c.AvisoReasignacion }).ToListAsync(ct);
        var deactivated = await context.MedicosBaja.AsNoTracking().OrderByDescending(m => m.DadoDeBajaEnUtc).Take(100)
            .Select(m => new { m.Id, m.MedicoOriginalId, m.Nombre, m.NombreUsuario, m.EspecialidadId, m.Motivo, m.SolicitadoPor, m.AprobadoPor, m.DadoDeBajaEnUtc }).ToListAsync(ct);
        var archivedAppointments = await context.MedicosBajaCitas.AsNoTracking().OrderByDescending(a => a.ProcesadaEnUtc).Take(300)
            .Select(a => new { a.CitaId, a.Paciente, a.Especialidad, a.Procedimiento, a.Fecha, a.InicioMin, a.MedicoAnterior, a.MedicoDestino, a.Resultado, a.PorcentajeReembolso, a.MontoReembolso, a.ProcesadaEnUtc }).ToListAsync(ct);
        return Ok(new { medicos = doctorRows, enfermeros = nurses, solicitudesBaja = requests, movimientos = movements, citas = appointments, medicosBaja = deactivated, citasProcesadasPorBaja = archivedAppointments });
    }

    [HttpPost("solicitudes-baja/{id:int}/aprobar")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var request = await context.SolicitudesBajaMedico.SingleOrDefaultAsync(r => r.Id == id && r.Estado == "Pendiente", ct);
        if (request is null) return NotFound(new { message = "La solicitud ya no está pendiente o no existe." });
        var doctor = await context.Medicos.SingleOrDefaultAsync(m => m.Id == request.MedicoId && m.Activo, ct);
        if (doctor is null) return Conflict(new { message = "El médico ya no está activo." });
        var masterName = User.FindFirstValue(ClaimTypes.Name) ?? "Cuenta maestra";
        try
        {
            await offboarding.ProcessAsync(doctor, request.Motivo, request.EnfermeroNombre, masterName, request.Id, ct);
            return Ok(new { message = "La baja fue aprobada y las citas se reasignaron o cancelaron." });
        }
        catch (DbUpdateException) { return Conflict(new { message = "No se pudo completar la baja. Revisa las citas relacionadas." }); }
    }

    [HttpPost("solicitudes-baja/{id:int}/rechazar")]
    public async Task<IActionResult> Reject(int id, RevisionBajaRequest input, CancellationToken ct)
    {
        var request = await context.SolicitudesBajaMedico.SingleOrDefaultAsync(r => r.Id == id && r.Estado == "Pendiente", ct);
        if (request is null) return NotFound(new { message = "La solicitud ya no está pendiente o no existe." });
        request.Estado = "Rechazada";
        request.NotaRevision = input.Nota?.Trim();
        request.RevisadaPorCuentaId = 1;
        request.RevisadaEnUtc = DateTime.UtcNow;
        context.MovimientosPersonal.Add(new MovimientoPersonal
        {
            ActorRol = "Maestra", ActorNombre = User.Identity?.Name ?? "Cuenta maestra", ActorUsuario = "maestra", Accion = "Solicitud de baja rechazada",
            DestinoTipo = "Médico", DestinoId = request.MedicoId, DestinoNombre = request.MedicoNombre,
            Detalle = $"Motivo solicitado: {request.Motivo}. Nota de revisión: {request.NotaRevision}", FechaUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("medicos/{id:int}/baja")]
    public async Task<IActionResult> DirectDoctorOffboarding(int id, BajaRequest input, CancellationToken ct)
    {
        var reason = input.Motivo?.Trim() ?? string.Empty;
        if (reason.Length < 5 || reason.Length > 1000) return BadRequest(new { message = "Escribe un motivo de entre 5 y 1000 caracteres." });
        var doctor = await context.Medicos.SingleOrDefaultAsync(m => m.Id == id && m.Activo, ct);
        if (doctor is null) return NotFound(new { message = "No se encontró un médico activo con ese identificador." });
        var masterName = User.Identity?.Name ?? "Cuenta maestra";
        await offboarding.ProcessAsync(doctor, reason, masterName, masterName, null, ct);
        return Ok(new { message = "El médico fue dado de baja." });
    }

    [HttpPost("enfermeros/{id:int}/baja")]
    public async Task<IActionResult> DisableNurse(int id, BajaRequest input, CancellationToken ct)
    {
        var reason = input.Motivo?.Trim() ?? string.Empty;
        if (reason.Length < 5 || reason.Length > 1000) return BadRequest(new { message = "Escribe un motivo de entre 5 y 1000 caracteres." });
        var nurse = await context.Enfermeros.SingleOrDefaultAsync(n => n.Id == id && n.Activo, ct);
        if (nurse is null) return NotFound(new { message = "No se encontró una enfermera o enfermero activo." });
        nurse.Activo = false;
        context.MovimientosPersonal.Add(new MovimientoPersonal
        {
            ActorRol = "Maestra", ActorNombre = User.Identity?.Name ?? "Cuenta maestra", ActorUsuario = "maestra", Accion = "Baja de enfermería",
            DestinoTipo = "Enfermero", DestinoId = nurse.Id, DestinoNombre = $"{nurse.Nombre} {nurse.ApellidoPaterno} {nurse.ApellidoMaterno}".Trim(), Detalle = reason, FechaUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/medicos")]
[Authorize(Policy = "NurseOnly")]
public class SolicitudesBajaController(AppDbContext context) : ControllerBase
{
    [HttpPost("{id:int}/solicitud-baja")]
    public async Task<IActionResult> RequestOffboarding(int id, BajaRequest input, CancellationToken ct)
    {
        var reason = input.Motivo?.Trim() ?? string.Empty;
        if (reason.Length < 5 || reason.Length > 1000) return BadRequest(new { message = "Escribe un motivo de entre 5 y 1000 caracteres." });
        var doctor = await context.Medicos.SingleOrDefaultAsync(m => m.Id == id && m.Activo, ct);
        if (doctor is null) return NotFound(new { message = "No se encontró un médico activo." });
        if (await context.SolicitudesBajaMedico.AnyAsync(r => r.MedicoId == id && r.Estado == "Pendiente", ct))
            return Conflict(new { message = "Ya existe una solicitud de baja pendiente para este médico." });
        var nurseId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var nurse = await context.Enfermeros.SingleAsync(n => n.Id == nurseId && n.Activo, ct);
        var nurseName = $"{nurse.Nombre} {nurse.ApellidoPaterno} {nurse.ApellidoMaterno}".Trim();
        var doctorName = DoctorOffboardingService.FullName(doctor);
        var request = new SolicitudBajaMedico { MedicoId = id, MedicoNombre = doctorName, EnfermeroId = nurse.Id, EnfermeroNombre = nurseName, Motivo = reason };
        context.SolicitudesBajaMedico.Add(request);
        context.MovimientosPersonal.Add(new MovimientoPersonal
        {
            ActorRol = "Enfermero", ActorId = nurse.Id, ActorUsuario = nurse.NombreUsuario, ActorNombre = nurseName, Accion = "Solicitud de baja de médico",
            DestinoTipo = "Médico", DestinoId = doctor.Id, DestinoNombre = doctorName, Detalle = reason, FechaUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);
        return Created($"api/maestra/panel", new { request.Id, request.Estado, request.MedicoNombre, request.Motivo });
    }
}
