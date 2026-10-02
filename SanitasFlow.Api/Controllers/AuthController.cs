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
    [HttpPost("register")]
    public async Task<ActionResult<PatientResponse>> Register(RegisterRequest request)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.Nombres) || string.IsNullOrWhiteSpace(request.ApellidoPaterno) ||
            request.Edad is < 0 or > 120 || string.IsNullOrWhiteSpace(correo) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Revisa los datos: la contraseña debe tener al menos 8 caracteres." });
        }
        if (await context.Pacientes.AnyAsync(p => p.Correo == correo))
        {
            return Conflict(new { message = "Ya existe una cuenta con ese correo." });
        }

        var paciente = new Paciente
        {
            Nombres = request.Nombres.Trim(), ApellidoPaterno = request.ApellidoPaterno.Trim(),
            ApellidoMaterno = request.ApellidoMaterno?.Trim() ?? string.Empty, Edad = request.Edad,
            Correo = correo, PasswordHash = passwords.Hash(request.Password)
        };
        context.Pacientes.Add(paciente);
        await context.SaveChangesAsync();
        await SignInAsync(paciente);
        return Created("api/auth/me", ToResponse(paciente));
    }

    [HttpPost("login")]
    public async Task<ActionResult<PatientResponse>> Login(LoginRequest request)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        var paciente = await context.Pacientes.SingleOrDefaultAsync(p => p.Correo == correo);
        if (paciente is null || !passwords.Verify(request.Password, paciente.PasswordHash))
        {
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });
        }
        await SignInAsync(paciente);
        return Ok(ToResponse(paciente));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<PatientResponse>> Me()
    {
        var paciente = await CurrentPatientAsync();
        return paciente is null ? Unauthorized() : Ok(ToResponse(paciente));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
            new Claim(ClaimTypes.NameIdentifier, paciente.Id.ToString()), new Claim(ClaimTypes.Email, paciente.Correo), new Claim(ClaimTypes.Name, paciente.Nombres)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private static PatientResponse ToResponse(Paciente p) => new(p.Id, p.Nombres, p.ApellidoPaterno, p.ApellidoMaterno, p.Edad, p.Correo);
}
