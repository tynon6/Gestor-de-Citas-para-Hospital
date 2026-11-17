using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Services;

public class DoctorOffboardingService(AppDbContext context, AgendaMedicaContext agenda, AgendaCancelacionesContext cancelledAgenda)
{
    public async Task ProcessAsync(Medico doctor, string reason, string requestedBy, string approvedBy, int? requestId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        doctor = await context.Medicos.SingleAsync(m => m.Id == doctor.Id, ct);
        var oldName = FullName(doctor);
        var mexicoToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, GetMexicoTimeZone()).DateTime);
        var appointments = await context.Citas.Include(c => c.Paciente).Include(c => c.Especialidad).Include(c => c.Procedimiento).Include(c => c.Pagos)
            .Where(c => c.MedicoId == doctor.Id && c.Fecha >= mexicoToday && (c.Estado == "Pendiente" || c.Estado == "Confirmada"))
            .OrderBy(c => c.Fecha).ThenBy(c => c.InicioMin).ToListAsync(ct);

        foreach (var cita in appointments)
        {
            var oldAgenda = await agenda.CitasConfirmadas.SingleOrDefaultAsync(item => item.CitaId == cita.Id, ct);
            var today = cita.Fecha == mexicoToday;
            var rejectedIds = await context.RechazosCita.Where(r => r.CitaId == cita.Id).Select(r => r.MedicoId).ToListAsync(ct);
            rejectedIds.Add(doctor.Id);
            var occupied = await context.Citas.Where(other => other.Id != cita.Id && other.EspecialidadId == cita.EspecialidadId && other.Fecha == cita.Fecha && (other.Estado == "Pendiente" || other.Estado == "Confirmada"))
                .Select(other => new { other.MedicoId, other.InicioMin, other.FinMin }).ToListAsync(ct);
            var candidates = today ? [] : await context.Medicos.Where(m => m.Activo && m.Id != doctor.Id && m.EspecialidadId == cita.EspecialidadId && !rejectedIds.Contains(m.Id)).ToListAsync(ct);
            var available = candidates.Where(candidate => occupied.Where(slot => slot.MedicoId == candidate.Id)
                .All(slot => cita.InicioMin >= slot.FinMin || cita.FinMin <= slot.InicioMin)).ToList();

            if (available.Count > 0)
            {
                var replacement = available[Random.Shared.Next(available.Count)];
                cita.MedicoId = replacement.Id;
                cita.Estado = "Pendiente";
                cita.ReasignacionUrgente = true;
                cita.AvisoReasignacion = $"Reasignación de cita por la baja de {oldName}";
                if (oldAgenda is not null) agenda.CitasConfirmadas.Remove(oldAgenda);
                context.MovimientosPersonal.Add(new MovimientoPersonal
                {
                    ActorRol = "Maestra", ActorNombre = approvedBy, ActorUsuario = "maestra", Accion = "Reasignación por baja",
                    DestinoTipo = "Cita", DestinoId = cita.Id, DestinoNombre = $"Cita #{cita.Id}",
                    Detalle = $"Se reasignó de {oldName} a {FullName(replacement)} por baja aprobada.", FechaUtc = DateTime.UtcNow
                });
                context.MedicosBajaCitas.Add(ToArchive(cita, oldName, FullName(replacement), "Reasignada", 0, 0m));
            }
            else
            {
                cita.Estado = "Cancelada";
                cita.MotivoCancelacion = $"Cita cancelada por baja de {oldName}; no hubo médico disponible para reasignarla.";
                cita.CanceladaEnUtc = DateTime.UtcNow;
                cita.PorcentajeReembolso = 100;
                cita.MontoReembolso = cita.Precio;
                cita.ReasignacionUrgente = false;
                cita.AvisoReasignacion = null;
                foreach (var payment in cita.Pagos.Where(p => p.Estado.StartsWith("Aprobado", StringComparison.OrdinalIgnoreCase)))
                    payment.Estado = $"Reembolso 100%: ${cita.Precio:0.00} MXN";
                if (oldAgenda is not null) agenda.CitasConfirmadas.Remove(oldAgenda);
                context.MedicosBajaCitas.Add(ToArchive(cita, oldName, null, "Cancelada", 100, cita.Precio));
                cancelledAgenda.CitasCanceladas.Add(new CitaCancelada
                {
                    CitaId = cita.Id, Estado = "Cancelada", MedicoId = doctor.Id, Medico = oldName,
                    Especialidad = cita.Especialidad.Nombre,
                    Paciente = $"{cita.Paciente.Nombres} {cita.Paciente.ApellidoPaterno} {cita.Paciente.ApellidoMaterno}".Trim(),
                    Procedimiento = cita.Procedimiento.Nombre, Fecha = cita.Fecha, InicioMin = cita.InicioMin, FinMin = cita.FinMin,
                    MotivoCancelacion = cita.MotivoCancelacion, CanceladaEnUtc = cita.CanceladaEnUtc.Value,
                    PorcentajeReembolso = 100, MontoReembolso = cita.Precio
                });
            }
        }

        context.MedicosBaja.Add(new MedicoBaja
        {
            MedicoOriginalId = doctor.Id, Nombre = oldName, NombreUsuario = doctor.NombreUsuario, Correo = doctor.Correo,
            EspecialidadId = doctor.EspecialidadId, Edad = doctor.Edad, Sexo = doctor.Sexo,
            Motivo = reason, SolicitadoPor = requestedBy, AprobadoPor = approvedBy, DadoDeBajaEnUtc = DateTime.UtcNow
        });
        if (requestId.HasValue)
        {
            var request = await context.SolicitudesBajaMedico.SingleAsync(item => item.Id == requestId.Value, ct);
            request.Estado = "Aprobada";
            request.RevisadaPorCuentaId = 1;
            request.RevisadaEnUtc = DateTime.UtcNow;
        }
        context.MovimientosPersonal.Add(new MovimientoPersonal
        {
            ActorRol = "Maestra", ActorNombre = approvedBy, ActorUsuario = "maestra", Accion = "Baja de médico",
            DestinoTipo = "Médico", DestinoId = doctor.Id, DestinoNombre = oldName,
            Detalle = reason, FechaUtc = DateTime.UtcNow
        });
        context.Medicos.Remove(doctor);
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await agenda.SaveChangesAsync(ct);
        await cancelledAgenda.SaveChangesAsync(ct);
    }

    private static MedicoBajaCita ToArchive(Cita cita, string oldName, string? replacement, string outcome, int refundPercent, decimal refund) => new()
    {
        CitaId = cita.Id, Paciente = $"{cita.Paciente.Nombres} {cita.Paciente.ApellidoPaterno} {cita.Paciente.ApellidoMaterno}".Trim(),
        Especialidad = cita.Especialidad.Nombre, Procedimiento = cita.Procedimiento.Nombre, Fecha = cita.Fecha,
        InicioMin = cita.InicioMin, FinMin = cita.FinMin, MedicoAnterior = oldName, MedicoDestino = replacement,
        Resultado = outcome, PorcentajeReembolso = refundPercent, MontoReembolso = refund, ProcesadaEnUtc = DateTime.UtcNow
    };
    public static string FullName(Medico doctor) => $"{(doctor.Sexo == "Mujer" ? "Dra." : "Dr.")} {doctor.Nombre} {doctor.ApellidoPaterno} {doctor.ApellidoMaterno}".Trim();
    private static TimeZoneInfo GetMexicoTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)"); }
    }
}
