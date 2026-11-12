using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Services;

public class AgendaMedicaService(AppDbContext context, AgendaMedicaContext agenda, AgendaCancelacionesContext cancelledAgenda)
{
    public async Task<Cita> AceptarAsync(int citaId, int medicoId, CancellationToken cancellationToken)
    {
        var cita = await context.Citas.Include(c => c.Medico).Include(c => c.Paciente).Include(c => c.Especialidad)
            .SingleOrDefaultAsync(c => c.Id == citaId && c.MedicoId == medicoId, cancellationToken)
            ?? throw new InvalidOperationException("No se encontró una cita asignada a tu cuenta.");
        if (cita.Estado is not ("Pendiente" or "Confirmada")) throw new InvalidOperationException("La cita ya no está pendiente.");

        if (cita.Estado == "Pendiente") cita.Estado = "Confirmada";
        cita.Medico = await context.Medicos.SingleAsync(m => m.Id == medicoId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var registro = await agenda.CitasConfirmadas.SingleOrDefaultAsync(c => c.CitaId == cita.Id, cancellationToken);
        if (registro is null)
        {
            agenda.CitasConfirmadas.Add(new CitaConfirmada
            {
                CitaId = cita.Id,
                Medico = $"{cita.Medico.Nombre} {cita.Medico.ApellidoPaterno} {cita.Medico.ApellidoMaterno}".Trim(),
                Especialidad = cita.Especialidad.Nombre,
                Paciente = $"{cita.Paciente.Nombres} {cita.Paciente.ApellidoPaterno} {cita.Paciente.ApellidoMaterno}".Trim(),
                Fecha = cita.Fecha, InicioMin = cita.InicioMin, FinMin = cita.FinMin
            });
        }
        await agenda.SaveChangesAsync(cancellationToken);
        return cita;
    }

    public async Task<(Cita Cita, bool Reasignada)> RechazarAsync(int citaId, int medicoId, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var cita = await context.Citas.Include(c => c.Pagos).Include(c => c.Medico)
            .Include(c => c.Paciente).Include(c => c.Especialidad).Include(c => c.Procedimiento)
            .SingleOrDefaultAsync(c => c.Id == citaId && c.MedicoId == medicoId, cancellationToken)
            ?? throw new InvalidOperationException("No se encontró una cita asignada a tu cuenta.");
        if (cita.Estado != "Pendiente") throw new InvalidOperationException("La cita ya no está pendiente.");

        context.RechazosCita.Add(new RechazoCita { CitaId = cita.Id, MedicoId = medicoId });
        await context.SaveChangesAsync(cancellationToken);

        var rejectedIds = await context.RechazosCita.Where(r => r.CitaId == cita.Id).Select(r => r.MedicoId).ToListAsync(cancellationToken);
        var doctors = await context.Medicos.Where(m => m.EspecialidadId == cita.EspecialidadId && !rejectedIds.Contains(m.Id)).ToListAsync(cancellationToken);
        var otherAppointments = await context.Citas.Where(c => c.Id != cita.Id && c.EspecialidadId == cita.EspecialidadId && c.Fecha == cita.Fecha && c.Estado != "Cancelada" && c.Estado != "Rechazada").ToListAsync(cancellationToken);
        var available = doctors.Where(m => otherAppointments.Where(c => c.MedicoId == m.Id)
            .All(c => cita.InicioMin >= c.FinMin || cita.FinMin <= c.InicioMin)).ToList();

        if (available.Count > 0)
        {
            cita.MedicoId = available[Random.Shared.Next(available.Count)].Id;
            cita.Estado = "Pendiente";
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (cita, true);
        }

        cita.Estado = "Rechazada";
        cita.CanceladaEnUtc = DateTime.UtcNow;
        cita.PorcentajeReembolso = 100;
        cita.MontoReembolso = cita.Precio;
        foreach (var payment in cita.Pagos.Where(p => p.Estado == "Aprobado"))
            payment.Estado = $"Reembolso 100%: ${cita.Precio:0.00} MXN";

        await context.SaveChangesAsync(cancellationToken);
        var archivedAppointment = await cancelledAgenda.CitasCanceladas
            .SingleOrDefaultAsync(record => record.CitaId == cita.Id, cancellationToken);
        if (archivedAppointment is null)
        {
            cancelledAgenda.CitasCanceladas.Add(new CitaCancelada
            {
                CitaId = cita.Id,
                Estado = "Rechazada",
                MedicoId = medicoId,
                Medico = cita.Medico is null ? string.Empty : $"{cita.Medico.Nombre} {cita.Medico.ApellidoPaterno} {cita.Medico.ApellidoMaterno}".Trim(),
                Especialidad = cita.Especialidad.Nombre,
                Paciente = $"{cita.Paciente.Nombres} {cita.Paciente.ApellidoPaterno} {cita.Paciente.ApellidoMaterno}".Trim(),
                Procedimiento = cita.Procedimiento.Nombre,
                Fecha = cita.Fecha,
                InicioMin = cita.InicioMin,
                FinMin = cita.FinMin,
                MotivoCancelacion = "Ningún médico aceptó la cita.",
                CanceladaEnUtc = cita.CanceladaEnUtc.Value,
                PorcentajeReembolso = 100,
                MontoReembolso = cita.Precio
            });
        }
        await cancelledAgenda.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (cita, false);
    }
}
