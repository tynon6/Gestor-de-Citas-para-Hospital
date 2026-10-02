using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;
using System.Data;

namespace SanitasFlow.Api.Services
{
    public class CitaService
    {
        private readonly AppDbContext _context;

        public CitaService(AppDbContext context)
        {
            _context = context;
        }

        private static readonly IReadOnlyDictionary<DayOfWeek, (int Inicio, int Fin)[]> Horarios =
            new Dictionary<DayOfWeek, (int, int)[]>
            {
                [DayOfWeek.Monday] = [(540, 840), (900, 1080)],
                [DayOfWeek.Tuesday] = [(540, 840), (900, 1080)],
                [DayOfWeek.Wednesday] = [(540, 840), (900, 1080)],
                [DayOfWeek.Thursday] = [(540, 840), (900, 1080)],
                [DayOfWeek.Friday] = [(540, 840), (900, 1080)],
                [DayOfWeek.Saturday] = [(540, 780)],
                [DayOfWeek.Sunday] = []
            };

        public async Task<List<int>> ObtenerHorariosDisponiblesAsync(string especialidadId, string procedimientoId, DateOnly fecha, CancellationToken cancellationToken)
        {
            var procedimiento = await ObtenerProcedimientoAsync(especialidadId, procedimientoId, cancellationToken);
            var ocupadas = await ObtenerCitasOcupadasAsync(especialidadId, fecha, cancellationToken);
            return CalcularHorarios(fecha, procedimiento.DuracionMin, ocupadas);
        }

        public async Task<Cita> CrearCitaAsync(int pacienteId, string especialidadId, string procedimientoId, DateOnly fecha, int inicioMin, string motivo, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var procedimiento = await ObtenerProcedimientoAsync(especialidadId, procedimientoId, cancellationToken);
            var ocupadas = await ObtenerCitasOcupadasAsync(especialidadId, fecha, cancellationToken);
            var finMin = inicioMin + procedimiento.DuracionMin;

            if (!CalcularHorarios(fecha, procedimiento.DuracionMin, ocupadas).Contains(inicioMin))
            {
                throw new InvalidOperationException("El horario solicitado ya no está disponible o queda fuera del horario de atención.");
            }

            var cita = new Cita
            {
                PacienteId = pacienteId,
                EspecialidadId = especialidadId,
                ProcedimientoId = procedimientoId,
                Fecha = fecha,
                InicioMin = inicioMin,
                FinMin = finMin,
                Motivo = motivo.Trim(),
                Precio = procedimiento.Precio,
                Estado = "Confirmada"
            };
            _context.Citas.Add(cita);
            _context.Pagos.Add(new Pago
            {
                Cita = cita,
                Monto = procedimiento.Precio,
                ReferenciaExterna = $"simulado-{Guid.NewGuid():N}"
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return cita;
        }

        private async Task<Procedimiento> ObtenerProcedimientoAsync(string especialidadId, string procedimientoId, CancellationToken cancellationToken)
        {
            return await _context.Procedimientos.AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == procedimientoId && p.EspecialidadId == especialidadId, cancellationToken)
                ?? throw new InvalidOperationException("El procedimiento solicitado no existe para esa especialidad.");
        }

        private async Task<List<Cita>> ObtenerCitasOcupadasAsync(string especialidadId, DateOnly fecha, CancellationToken cancellationToken)
        {
            return await _context.Citas
                .Where(c => c.EspecialidadId == especialidadId && c.Fecha == fecha && c.Estado != "Cancelada")
                .ToListAsync(cancellationToken);
        }

        private static List<int> CalcularHorarios(DateOnly fecha, int duracionMin, List<Cita> ocupadas)
        {
            if (fecha < DateOnly.FromDateTime(DateTime.Today) || !Horarios.TryGetValue(fecha.DayOfWeek, out var ventanas))
            {
                return [];
            }

            var hoy = fecha == DateOnly.FromDateTime(DateTime.Today);
            var ahoraMin = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            var resultado = new List<int>();
            foreach (var (inicio, fin) in ventanas)
            {
                for (var hora = inicio; hora + duracionMin <= fin; hora += 15)
                {
                    if (hoy && hora <= ahoraMin) continue;
                    if (ocupadas.Any(c => hora < c.FinMin && hora + duracionMin > c.InicioMin)) continue;
                    resultado.Add(hora);
                }
            }
            return resultado;
        }
    }
}
