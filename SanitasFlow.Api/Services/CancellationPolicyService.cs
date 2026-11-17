using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Contracts;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Services;

/// <summary>Aplica las reglas de cancelación y calcula el reembolso.</summary>
public class CancellationPolicyService(AppDbContext context, AgendaMedicaContext agenda, AgendaCancelacionesContext cancelledAgenda)
{
    public CancellationQuoteResponse GetQuote(Cita cita, DateTimeOffset? now = null)
    {
        var zone = GetMexicoTimeZone();
        var current = TimeZoneInfo.ConvertTime(now ?? DateTimeOffset.UtcNow, zone);
        var bookedAt = TimeZoneInfo.ConvertTime(new DateTimeOffset(DateTime.SpecifyKind(cita.CreadaEnUtc, DateTimeKind.Utc)), zone);
        var appointmentLocal = cita.Fecha.ToDateTime(TimeOnly.MinValue).AddMinutes(cita.InicioMin);
        var appointmentAt = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(appointmentLocal, DateTimeKind.Unspecified), zone));
        var remaining = appointmentAt - current;
        var sameBookingDay = bookedAt.Date == current.Date;
        var nextDayBooking = cita.Fecha == DateOnly.FromDateTime(bookedAt.Date.AddDays(1));
        var withinNextDayGrace = nextDayBooking && current >= bookedAt && current - bookedAt <= TimeSpan.FromHours(1);
        var canCancel = cita.Estado is not ("Cancelada" or "Rechazada") && remaining > TimeSpan.Zero;

        var percentage = nextDayBooking ? withinNextDayGrace ? 100 : 0
            : sameBookingDay ? 100 : remaining <= TimeSpan.FromHours(48) ? 0 : 80;
        var refund = Math.Round(cita.Precio * percentage / 100m, 2, MidpointRounding.AwayFromZero);
        var message = !canCancel
            ? cita.Estado is "Cancelada" or "Rechazada" ? "Esta cita ya no se puede cancelar." : "Esta cita ya pasó y no se puede cancelar."
            : nextDayBooking
                ? withinNextDayGrace
                    ? "Para una cita agendada para mañana, puedes cancelar durante la primera hora después de reservar y recibir el 100% de devolución. Después de esa hora, puedes cancelar sin devolución."
                    : "Esta cita fue agendada para mañana y ya terminó la primera hora desde la reserva. Puedes cancelar, pero ya no aplica devolución."
            : sameBookingDay
                ? "Si cancelas hoy, el sistema registrará un reembolso del 100% del pago."
                : percentage == 0
                    ? "Ya estás dentro del periodo de 48 horas. Puedes cancelar, pero no se te regresará el dinero."
                    : "Al cancelar con más de 48 horas de anticipación, el sistema registrará un reembolso del 80%.";

        return new CancellationQuoteResponse(canCancel, percentage, refund, cita.Precio, sameBookingDay || withinNextDayGrace, message);
    }

    private static TimeZoneInfo GetMexicoTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)"); }
    }

    public async Task<CancellationQuoteResponse> CancelAsync(Cita cita, string motivoCancelacion, bool deseaReagendar, CancellationToken cancellationToken)
    {
        var confirmedRecord = await agenda.CitasConfirmadas
            .SingleOrDefaultAsync(record => record.CitaId == cita.Id, cancellationToken);
        var procedureName = confirmedRecord is null
            ? string.Empty
            : await context.Procedimientos.AsNoTracking()
                .Where(procedure => procedure.Id == cita.ProcedimientoId)
                .Select(procedure => procedure.Nombre)
                .SingleAsync(cancellationToken);
        var quote = GetQuote(cita);
        if (!quote.PuedeCancelar)
            throw new InvalidOperationException(quote.Mensaje);

        cita.Estado = "Cancelada";
        cita.MotivoCancelacion = motivoCancelacion.Trim();
        cita.DeseaReagendar = deseaReagendar;
        cita.CanceladaEnUtc = DateTime.UtcNow;
        cita.PorcentajeReembolso = quote.PorcentajeReembolso;
        cita.MontoReembolso = quote.MontoReembolso;
        var payments = await context.Pagos
            .Where(p => p.CitaId == cita.Id && p.Estado == "Aprobado")
            .ToListAsync(cancellationToken);
        foreach (var payment in payments)
        {
            payment.Estado = quote.PorcentajeReembolso == 0
                ? "Cancelado; sin reembolso"
                : "Reembolso " + quote.PorcentajeReembolso + "%: $" + quote.MontoReembolso.ToString("0.00") + " MXN";
        }

        await context.SaveChangesAsync(cancellationToken);

        if (confirmedRecord is not null)
        {
            var cancelledRecord = await cancelledAgenda.CitasCanceladas
                .SingleOrDefaultAsync(record => record.CitaId == cita.Id, cancellationToken);
            if (cancelledRecord is null)
            {
                cancelledAgenda.CitasCanceladas.Add(new CitaCancelada
                {
                    CitaId = cita.Id,
                    Estado = "Cancelada",
                    MedicoId = cita.MedicoId ?? 0,
                    Medico = confirmedRecord.Medico,
                    Especialidad = confirmedRecord.Especialidad,
                    Paciente = confirmedRecord.Paciente,
                    Procedimiento = procedureName,
                    Fecha = confirmedRecord.Fecha,
                    InicioMin = confirmedRecord.InicioMin,
                    FinMin = confirmedRecord.FinMin,
                    MotivoCancelacion = cita.MotivoCancelacion,
                    CanceladaEnUtc = cita.CanceladaEnUtc ?? DateTime.UtcNow,
                    PorcentajeReembolso = quote.PorcentajeReembolso,
                    MontoReembolso = quote.MontoReembolso
                });
            }
            await cancelledAgenda.SaveChangesAsync(cancellationToken);

            agenda.CitasConfirmadas.Remove(confirmedRecord);
            await agenda.SaveChangesAsync(cancellationToken);
        }

        return quote;
    }
}
