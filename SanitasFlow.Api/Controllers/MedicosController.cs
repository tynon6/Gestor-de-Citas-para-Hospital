using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;
using SanitasFlow.Api.Contracts;
using SanitasFlow.Api.Services;

namespace SanitasFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "StaffOnly")]
    public class MedicosController(AppDbContext context, PasswordService passwords) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetMedicos()
        {
            var medicos = await context.Medicos.AsNoTracking()
                .Select(m => new { m.Id, m.Nombre, m.ApellidoPaterno, m.ApellidoMaterno, m.Edad, m.Sexo, m.EspecialidadId, m.Correo, m.NombreUsuario })
                .ToListAsync();
            return Ok(medicos);
        }

        [HttpPost]
        [Authorize(Policy = "NurseOnly")]
        public async Task<IActionResult> Create(AltaMedicoRequest request, CancellationToken cancellationToken)
        {
            var username = request.NombreUsuario.Trim().ToLowerInvariant();
            var email = request.Correo.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.ApellidoPaterno) ||
                request.Edad is < 26 or > 60 || string.IsNullOrWhiteSpace(request.Sexo) || username.Length is < 3 or > 40 ||
                !username.All(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.') || request.Password.Length < 8)
                return BadRequest(new { message = "Revisa los datos; edad 26 a 60, usuario válido y contraseña de al menos 8 caracteres." });
            if (!await context.Especialidades.AnyAsync(e => e.Id == request.EspecialidadId, cancellationToken))
                return BadRequest(new { message = "La especialidad no existe." });
            if (await context.Pacientes.AnyAsync(p => p.NombreUsuario == username || p.Correo == email, cancellationToken) ||
                await context.Medicos.AnyAsync(m => m.NombreUsuario == username || m.Correo == email, cancellationToken) ||
                await context.Enfermeros.AnyAsync(n => n.NombreUsuario == username || n.Correo == email, cancellationToken))
                return Conflict(new { message = "El correo o nombre de usuario ya está registrado." });

            var doctor = new Medico
            {
                Nombre = request.Nombre.Trim(), ApellidoPaterno = request.ApellidoPaterno.Trim(),
                ApellidoMaterno = request.ApellidoMaterno.Trim(), Edad = request.Edad, Sexo = request.Sexo.Trim(),
                EspecialidadId = request.EspecialidadId, Correo = email, NombreUsuario = username,
                PasswordHash = passwords.Hash(request.Password)
            };
            context.Medicos.Add(doctor);
            await context.SaveChangesAsync(cancellationToken);
            return Created("api/medicos", new { doctor.Id, doctor.Nombre, doctor.ApellidoPaterno, doctor.ApellidoMaterno, doctor.Edad, doctor.Sexo, doctor.EspecialidadId, doctor.Correo, doctor.NombreUsuario });
        }
    }
}
