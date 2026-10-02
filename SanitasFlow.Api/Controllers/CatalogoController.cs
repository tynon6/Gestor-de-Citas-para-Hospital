using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public class CatalogoController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var especialidades = await context.Especialidades.AsNoTracking()
            .Include(e => e.Procedimientos)
            .OrderBy(e => e.Nombre)
            .Select(e => new
            {
                e.Id, e.Nombre, e.Descripcion,
                procedimientos = e.Procedimientos.OrderBy(p => p.Nombre).Select(p => new { p.Id, p.Nombre, p.Descripcion, p.DuracionMin, p.Precio })
            })
            .ToListAsync(cancellationToken);
        return Ok(especialidades);
    }
}
