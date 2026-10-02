using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MedicosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicos()
        {
            var medicos = await _context.Medicos.AsNoTracking().ToListAsync();
            return Ok(medicos);
        }
    }
}
