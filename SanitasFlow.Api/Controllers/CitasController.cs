using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Contracts;
using SanitasFlow.Api.Models;
using SanitasFlow.Api.Services;

namespace SanitasFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/citas")]
public class CitasController(AppDbContext context, CitaService citaService) : ControllerBase
{
    [HttpGet("disponibilidad")]
    public async Task<ActionResult<object>> Disponibilidad([FromQuery] string especialidadId, [FromQuery] string procedimientoId, [FromQuery] string fecha, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(fecha, out var fechaCita)) return BadRequest(new { message = "La fecha no es válida." });
        try
        {
            var horas = await citaService.ObtenerHorariosDisponiblesAsync(especialidadId, procedimientoId, fechaCita, cancellationToken);
            return Ok(new { horas });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mias")]
    public async Task<ActionResult<List<AppointmentResponse>>> Mias(CancellationToken cancellationToken)
    {
        if (!TryGetPatientId(out var pacienteId)) return Unauthorized();
        var citas = await context.Citas.AsNoTracking()
            .Include(c => c.Especialidad).Include(c => c.Procedimiento)
            .Where(c => c.PacienteId == pacienteId)
            .OrderBy(c => c.Fecha).ThenBy(c => c.InicioMin)
            .ToListAsync(cancellationToken);
        return Ok(citas.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        if (!TryGetPatientId(out var pacienteId)) return Unauthorized();
        var cita = await context.Citas.AsNoTracking()
            .Include(c => c.Especialidad).Include(c => c.Procedimiento)
            .SingleOrDefaultAsync(c => c.Id == id && c.PacienteId == pacienteId, cancellationToken);
        return cita is null ? NotFound() : Ok(ToResponse(cita));
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetPatientId(out var pacienteId)) return Unauthorized();
        if (!DateOnly.TryParse(request.Fecha, out var fecha)) return BadRequest(new { message = "La fecha no es válida." });
        if (request.Motivo?.Length > 1000) return BadRequest(new { message = "El motivo no puede superar 1000 caracteres." });

        try
        {
            var cita = await citaService.CrearCitaAsync(pacienteId, request.EspecialidadId, request.ProcedimientoId, fecha, request.InicioMin, request.Motivo ?? string.Empty, cancellationToken);
            var response = await context.Citas.AsNoTracking().Include(c => c.Especialidad).Include(c => c.Procedimiento)
                .SingleAsync(c => c.Id == cita.Id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = cita.Id }, ToResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private bool TryGetPatientId(out int patientId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out patientId);

    private static AppointmentResponse ToResponse(Cita cita) => new(cita.Id, cita.Especialidad.Nombre, cita.Procedimiento.Nombre,
        cita.Fecha.ToString("yyyy-MM-dd"), cita.InicioMin, cita.FinMin, cita.Precio, cita.Motivo, cita.Estado);
}
