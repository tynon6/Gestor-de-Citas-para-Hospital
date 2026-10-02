using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Contracts;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes")]
public class PacientesController(AppDbContext context) : ControllerBase
{
    [HttpPut("me")]
    public async Task<ActionResult<PatientResponse>> UpdateMe(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombres) || string.IsNullOrWhiteSpace(request.ApellidoPaterno) || request.Edad is < 0 or > 120)
            return BadRequest(new { message = "Revisa los datos del perfil." });

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        var paciente = await context.Pacientes.SingleOrDefaultAsync(p => p.Id == id);
        if (paciente is null) return Unauthorized();
        paciente.Nombres = request.Nombres.Trim();
        paciente.ApellidoPaterno = request.ApellidoPaterno.Trim();
        paciente.ApellidoMaterno = request.ApellidoMaterno?.Trim() ?? string.Empty;
        paciente.Edad = request.Edad;
        await context.SaveChangesAsync();
        return Ok(new PatientResponse(paciente.Id, paciente.Nombres, paciente.ApellidoPaterno, paciente.ApellidoMaterno, paciente.Edad, paciente.Correo));
    }
}
