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
            var doctors = await _context.Medicos.AsNoTracking()
                .Where(m => m.EspecialidadId == especialidadId).Select(m => m.Id).ToListAsync(cancellationToken);
            if (doctors.Count == 0) return [];
            var occupied = await ObtenerCitasOcupadasAsync(especialidadId, fecha, cancellationToken);
            var possibleSlots = CalcularHorarios(fecha, procedimiento.DuracionMin, []);
            return possibleSlots.Where(start => doctors.Any(doctorId => occupied
                .Where(c => c.MedicoId == doctorId)
                .All(c => !SeTraslapan(start, start + procedimiento.DuracionMin, c.InicioMin, c.FinMin))))
                .ToList();
        }

        public async Task<Cita> CrearCitaAsync(int pacienteId, string especialidadId, string procedimientoId, DateOnly fecha, int inicioMin, string motivo, CancellationToken cancellationToken)
        {
            var fechaMinima = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
            if (fecha < fechaMinima)
            {
                throw new InvalidOperationException("Solo se pueden agendar citas a partir de mañana.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var procedimiento = await ObtenerProcedimientoAsync(especialidadId, procedimientoId, cancellationToken);
            var finMin = inicioMin + procedimiento.DuracionMin;

            var doctors = await _context.Medicos
                .Where(m => m.EspecialidadId == especialidadId).ToListAsync(cancellationToken);
            var occupied = await ObtenerCitasOcupadasAsync(especialidadId, fecha, cancellationToken);
            var availableDoctors = doctors.Where(doctor => occupied.Where(c => c.MedicoId == doctor.Id)
                .All(c => !SeTraslapan(inicioMin, finMin, c.InicioMin, c.FinMin))).ToList();
            if (inicioMin < 0 || !CalcularHorarios(fecha, procedimiento.DuracionMin, []).Contains(inicioMin) || availableDoctors.Count == 0)
            {
                throw new InvalidOperationException("El horario solicitado ya no está disponible o queda fuera del horario de atención.");
            }

            var assignedDoctor = availableDoctors[Random.Shared.Next(availableDoctors.Count)];

            var cita = new Cita
            {
                PacienteId = pacienteId,
                MedicoId = assignedDoctor.Id,
                EspecialidadId = especialidadId,
                ProcedimientoId = procedimientoId,
                Fecha = fecha,
                InicioMin = inicioMin,
                FinMin = finMin,
                Motivo = motivo.Trim(),
                Precio = procedimiento.Precio,
                Estado = "Pendiente"
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
                .Where(c => c.Estado != "Rechazada")
                .ToListAsync(cancellationToken);
        }

        private static bool SeTraslapan(int inicioUno, int finUno, int inicioDos, int finDos) => inicioUno < finDos && finUno > inicioDos;

        private static List<int> CalcularHorarios(DateOnly fecha, int duracionMin, List<Cita> ocupadas)
        {
            var fechaMinima = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
            if (fecha < fechaMinima || !Horarios.TryGetValue(fecha.DayOfWeek, out var ventanas))
            {
                return [];
            }

            var resultado = new List<int>();
            foreach (var (inicio, fin) in ventanas)
            {
                for (var hora = inicio; hora + duracionMin <= fin; hora += 15)
                {
                    if (ocupadas.Any(c => hora < c.FinMin && hora + duracionMin > c.InicioMin)) continue;
                    resultado.Add(hora);
                }
            }
            return resultado;
        }
    }
}
