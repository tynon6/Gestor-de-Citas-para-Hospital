using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;
using SanitasFlow.Api.Services;

namespace SanitasFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/agenda-medica")]
public class AgendaMedicaController(AppDbContext context, AgendaMedicaContext agenda, AgendaCancelacionesContext cancelledAgenda, AgendaMedicaService service) : ControllerBase
{
    [HttpGet("citas")]
    public async Task<IActionResult> GetAppointments(CancellationToken cancellationToken)
    {
        int? doctorId = null;
        if (User.IsInRole("Medico"))
        {
            if (!TryGetDoctorId(out var id)) return Unauthorized();
            doctorId = id;
        }
        var nurseView = User.IsInRole("Enfermero");
        var pendingQuery = context.Citas.AsNoTracking().Include(c => c.Paciente).Include(c => c.Especialidad).Include(c => c.Procedimiento)
            .Include(c => c.Medico)
            .Where(c => c.Estado == "Pendiente");
        if (doctorId.HasValue) pendingQuery = pendingQuery.Where(c => c.MedicoId == doctorId.Value);
        var pendingAppointments = await pendingQuery
            .OrderBy(c => c.Fecha).ThenBy(c => c.InicioMin)
            .ToListAsync(cancellationToken);
        var pending = new List<object>();
        if (nurseView && pendingAppointments.Count > 0)
        {
            var pendingIds = pendingAppointments.Select(c => c.Id).ToList();
            var rejectionHistory = await context.RechazosCita.AsNoTracking()
                .Where(rejection => pendingIds.Contains(rejection.CitaId))
                .Join(context.Medicos.AsNoTracking(), rejection => rejection.MedicoId, doctor => doctor.Id,
                    (rejection, doctor) => new
                    {
                        rejection.CitaId,
                        rejection.RechazadaEnUtc,
                        Medico = $"{(doctor.Sexo == "Mujer" ? "Dra." : "Dr.")} {doctor.Nombre} {doctor.ApellidoPaterno} {doctor.ApellidoMaterno}".Trim()
                    })
                .OrderBy(rejection => rejection.RechazadaEnUtc)
                .ToListAsync(cancellationToken);

            foreach (var appointment in pendingAppointments)
            {
                var history = rejectionHistory.Where(item => item.CitaId == appointment.Id).ToList();
                var assignedDoctor = appointment.Medico is null
                    ? "médico pendiente de asignación"
                    : $"{(appointment.Medico.Sexo == "Mujer" ? "Dra." : "Dr.")} {appointment.Medico.Nombre} {appointment.Medico.ApellidoPaterno} {appointment.Medico.ApellidoMaterno}".Trim();
                var assignmentSummary = history.Count == 0
                    ? $"Pendiente de confirmación por {assignedDoctor}."
                    : string.Join(" ", history.Select((item, index) =>
                        $"{item.Medico} rechazó la solicitud; se pasó a {(index + 1 < history.Count ? history[index + 1].Medico : assignedDoctor)}."))
                        + $" Pendiente de confirmación por {assignedDoctor}.";

                pending.Add(new
                {
                    appointment.Id,
                    appointment.Estado,
                    appointment.Fecha,
                    appointment.InicioMin,
                    appointment.FinMin,
                    appointment.Motivo,
                    Paciente = $"{appointment.Paciente.Nombres} {appointment.Paciente.ApellidoPaterno}".Trim(),
                    Especialidad = appointment.Especialidad.Nombre,
                    Procedimiento = appointment.Procedimiento.Nombre,
                    MedicoAsignado = assignedDoctor,
                    ResumenAsignacion = assignmentSummary
                });
            }
        }
        else
        {
            pending.AddRange(pendingAppointments.Select(appointment => (object)new
            {
                appointment.Id,
                appointment.Estado,
                appointment.Fecha,
                appointment.InicioMin,
                appointment.FinMin,
                appointment.Motivo,
                Paciente = $"{appointment.Paciente.Nombres} {appointment.Paciente.ApellidoPaterno}".Trim(),
                Especialidad = appointment.Especialidad.Nombre,
                Procedimiento = appointment.Procedimiento.Nombre
            }));
        }

        var cancelledQuery = cancelledAgenda.CitasCanceladas.AsNoTracking().AsQueryable();
        if (doctorId.HasValue) cancelledQuery = cancelledQuery.Where(c => c.MedicoId == doctorId.Value);
        var cancelled = await cancelledQuery
            .OrderByDescending(c => c.CanceladaEnUtc)
            .Select(c => new { c.CitaId, c.Estado, c.Fecha, c.InicioMin, c.FinMin, c.MotivoCancelacion, c.Paciente, c.Especialidad, c.Procedimiento })
            .ToListAsync(cancellationToken);

        var confirmedAppointmentIds = await context.Citas.AsNoTracking()
            .Where(c => c.Estado == "Confirmada")
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        var confirmedQuery = agenda.CitasConfirmadas.AsNoTracking().AsQueryable();
        confirmedQuery = confirmedQuery.Where(c => confirmedAppointmentIds.Contains(c.CitaId));
        if (doctorId.HasValue)
        {
            var doctor = await context.Medicos.AsNoTracking().SingleAsync(m => m.Id == doctorId.Value, cancellationToken);
            var doctorName = $"{doctor.Nombre} {doctor.ApellidoPaterno} {doctor.ApellidoMaterno}".Trim();
            confirmedQuery = confirmedQuery.Where(c => c.Medico == doctorName);
        }
        var confirmed = await confirmedQuery
            .OrderBy(c => c.Fecha).ThenBy(c => c.InicioMin)
            .Select(c => new { c.CitaId, Estado = "Confirmada", c.Fecha, c.InicioMin, c.FinMin, Motivo = "", c.Paciente, c.Especialidad, Procedimiento = "", Medico = c.Medico })
            .ToListAsync(cancellationToken);
        return Ok(new { pendientes = pending, confirmadas = confirmed, canceladas = cancelled });
    }

    [HttpPost("citas/{id:int}/aceptar")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Accept(int id, CancellationToken cancellationToken)
    {
        if (!TryGetDoctorId(out var doctorId)) return Unauthorized();
        try
        {
            var appointment = await service.AceptarAsync(id, doctorId, cancellationToken);
            return Ok(new { id = appointment.Id, estado = appointment.Estado });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("citas/{id:int}/rechazar")]
    [Authorize(Policy = "DoctorOnly")]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        if (!TryGetDoctorId(out var doctorId)) return Unauthorized();
        try
        {
            var (appointment, reassigned) = await service.RechazarAsync(id, doctorId, cancellationToken);
            return Ok(new { id = appointment.Id, estado = appointment.Estado, reasignada = reassigned, montoReembolso = appointment.MontoReembolso });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    private bool TryGetDoctorId(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
