using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Contracts;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Services;

/// <summary>Aplica las reglas académicas de cancelación y calcula el reembolso simulado.</summary>
public class CancellationPolicyService(AppDbContext context)
{
    public CancellationQuoteResponse GetQuote(Cita cita, DateTimeOffset? now = null)
    {
        var current = (now ?? DateTimeOffset.Now).LocalDateTime;
        var appointmentAt = DateTime.SpecifyKind(
            cita.Fecha.ToDateTime(TimeOnly.MinValue).AddMinutes(cita.InicioMin),
            DateTimeKind.Local);
        var remaining = appointmentAt - current;
        var sameBookingDay = cita.CreadaEnUtc.ToLocalTime().Date == current.Date;
        var canCancel = cita.Estado != "Cancelada" && remaining > TimeSpan.Zero;

        var percentage = sameBookingDay ? 100 : remaining <= TimeSpan.FromHours(48) ? 0 : 80;
        var refund = Math.Round(cita.Precio * percentage / 100m, 2, MidpointRounding.AwayFromZero);
        var message = !canCancel
            ? cita.Estado == "Cancelada" ? "Esta cita ya fue cancelada." : "Esta cita ya pasó y no se puede cancelar."
            : sameBookingDay
                ? "Si cancelas hoy, el sistema registrará un reembolso simulado del 100% del pago."
                : percentage == 0
                    ? "Ya estás dentro del periodo de 48 horas. Puedes cancelar, pero no se te regresará el dinero."
                    : "Al cancelar con más de 48 horas de anticipación, el sistema registrará un reembolso simulado del 80%.";

        return new CancellationQuoteResponse(canCancel, percentage, refund, cita.Precio, sameBookingDay, message);
    }

    public async Task<CancellationQuoteResponse> CancelAsync(Cita cita, string motivoCancelacion, bool deseaReagendar, CancellationToken cancellationToken)
    {
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
                ? "Cancelado; sin reembolso simulado"
                : "Reembolso simulado " + quote.PorcentajeReembolso + "%: $" + quote.MontoReembolso.ToString("0.00") + " MXN";
        }

        await context.SaveChangesAsync(cancellationToken);
        return quote;
    }
}
