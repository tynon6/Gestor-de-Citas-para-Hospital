using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Contracts;
using SanitasFlow.Api.Models;
using SanitasFlow.Api.Services;

namespace SanitasFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext context, PasswordService passwords) : ControllerBase
{
    private const string PatientCookie = "PatientCookie";
    private const string StaffCookie = "StaffCookie";

    [HttpPost("register")]
    public async Task<ActionResult<PatientResponse>> Register(RegisterRequest request)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        var nombreUsuario = request.NombreUsuario.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.Nombres) || string.IsNullOrWhiteSpace(request.ApellidoPaterno) ||
            request.Edad is < 0 or > 120 || string.IsNullOrWhiteSpace(correo) || nombreUsuario.Length is < 3 or > 40 ||
            !nombreUsuario.All(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.') || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Revisa los datos: la contraseña debe tener al menos 8 caracteres." });
        }
        if (await context.Pacientes.AnyAsync(p => p.Correo == correo || p.NombreUsuario == nombreUsuario) ||
            await context.Medicos.AnyAsync(m => m.Correo == correo || m.NombreUsuario == nombreUsuario) ||
            await context.Enfermeros.AnyAsync(n => n.Correo == correo || n.NombreUsuario == nombreUsuario))
        {
            return Conflict(new { message = "El correo o nombre de usuario ya está registrado." });
        }

        var paciente = new Paciente
        {
            Nombres = request.Nombres.Trim(), ApellidoPaterno = request.ApellidoPaterno.Trim(),
            ApellidoMaterno = request.ApellidoMaterno?.Trim() ?? string.Empty, Edad = request.Edad,
            Correo = correo, NombreUsuario = nombreUsuario, PasswordHash = passwords.Hash(request.Password)
        };
        context.Pacientes.Add(paciente);
        await context.SaveChangesAsync();
        await SignInAsync(paciente);
        return Created("api/auth/me", ToResponse(paciente));
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login(LoginRequest request, [FromQuery] string area = "patient")
    {
        var identificador = request.Identificador.Trim().ToLowerInvariant();
        if (area.Equals("staff", StringComparison.OrdinalIgnoreCase))
        {
            var medico = await context.Medicos.SingleOrDefaultAsync(m => m.Correo == identificador || m.NombreUsuario == identificador);
            if (medico is not null && passwords.Verify(request.Password, medico.PasswordHash))
            {
                await SignInAsync(medico);
                return Ok(new StaffResponse(medico.Id, FullDoctorName(medico), medico.NombreUsuario, medico.Correo, "Medico"));
            }
            var nurse = await context.Enfermeros.SingleOrDefaultAsync(n => n.Correo == identificador || n.NombreUsuario == identificador);
            if (nurse is null || !passwords.Verify(request.Password, nurse.PasswordHash))
                return Unauthorized(new { message = "Usuario/correo o contraseña incorrectos." });
            await SignInAsync(nurse);
            return Ok(new StaffResponse(nurse.Id, FullNurseName(nurse), nurse.NombreUsuario, nurse.Correo, "Enfermero"));
        }

        if (!area.Equals("patient", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El área de inicio de sesión no es válida." });

        var paciente = await context.Pacientes.SingleOrDefaultAsync(p => p.Correo == identificador || p.NombreUsuario == identificador);
        if (paciente is null || !passwords.Verify(request.Password, paciente.PasswordHash))
            return Unauthorized(new { message = "Usuario/correo o contraseña incorrectos." });
        await SignInAsync(paciente);
        return Ok(ToResponse(paciente));
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me([FromQuery] string area = "patient")
    {
        var isStaffArea = area.Equals("staff", StringComparison.OrdinalIgnoreCase);
        if (!isStaffArea && !area.Equals("patient", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El área de sesión no es válida." });

        var scheme = isStaffArea ? StaffCookie : PatientCookie;
        var session = await HttpContext.AuthenticateAsync(scheme);
        var principal = session.Principal;
        if (!session.Succeeded || principal is null) return Unauthorized();

        var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(rawId, out var id)) return Unauthorized();

        if (isStaffArea && principal.IsInRole("Medico"))
        {
            var doctor = await context.Medicos.FindAsync(id);
            return doctor is null ? Unauthorized() : Ok(new StaffResponse(doctor.Id, FullDoctorName(doctor), doctor.NombreUsuario, doctor.Correo, "Medico"));
        }
        if (isStaffArea && principal.IsInRole("Enfermero"))
        {
            var nurse = await context.Enfermeros.FindAsync(id);
            return nurse is null ? Unauthorized() : Ok(new StaffResponse(nurse.Id, FullNurseName(nurse), nurse.NombreUsuario, nurse.Correo, "Enfermero"));
        }
        if (isStaffArea) return Unauthorized();

        var paciente = await context.Pacientes.FindAsync(id);
        return paciente is null ? Unauthorized() : Ok(ToResponse(paciente));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] string area = "patient")
    {
        if (area.Equals("staff", StringComparison.OrdinalIgnoreCase))
            await HttpContext.SignOutAsync(StaffCookie);
        else if (area.Equals("patient", StringComparison.OrdinalIgnoreCase))
            await HttpContext.SignOutAsync(PatientCookie);
        else
            return BadRequest(new { message = "El área de sesión no es válida." });
        return NoContent();
    }

    private async Task<Paciente?> CurrentPatientAsync()
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawId, out var id) ? await context.Pacientes.FindAsync(id) : null;
    }

    private async Task SignInAsync(Paciente paciente)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, paciente.Id.ToString()), new Claim(ClaimTypes.Email, paciente.Correo), new Claim(ClaimTypes.Name, paciente.Nombres), new Claim(ClaimTypes.Role, "Paciente")
        };
        var identity = new ClaimsIdentity(claims, PatientCookie);
        await HttpContext.SignInAsync(PatientCookie, new ClaimsPrincipal(identity));
    }

    private async Task SignInAsync(Medico medico)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, medico.Id.ToString()), new Claim(ClaimTypes.Email, medico.Correo),
            new Claim(ClaimTypes.Name, medico.Nombre), new Claim(ClaimTypes.Role, "Medico")
        };
        var identity = new ClaimsIdentity(claims, StaffCookie);
        await HttpContext.SignInAsync(StaffCookie, new ClaimsPrincipal(identity));
    }

    private async Task SignInAsync(Enfermero nurse)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, nurse.Id.ToString()), new Claim(ClaimTypes.Email, nurse.Correo),
            new Claim(ClaimTypes.Name, nurse.Nombre), new Claim(ClaimTypes.Role, "Enfermero")
        };
        var identity = new ClaimsIdentity(claims, StaffCookie);
        await HttpContext.SignInAsync(StaffCookie, new ClaimsPrincipal(identity));
    }

    private static string FullDoctorName(Medico doctor) => $"{doctor.Nombre} {doctor.ApellidoPaterno} {doctor.ApellidoMaterno}".Trim();
    private static string FullNurseName(Enfermero nurse) => $"{nurse.Nombre} {nurse.ApellidoPaterno} {nurse.ApellidoMaterno}".Trim();
    private static PatientResponse ToResponse(Paciente p) => new(p.Id, p.Nombres, p.ApellidoPaterno, p.ApellidoMaterno, p.Edad, p.Correo, p.NombreUsuario);
}
