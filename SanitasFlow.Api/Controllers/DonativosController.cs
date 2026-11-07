using Microsoft.AspNetCore.Mvc;
using SanitasFlow.Api.Contracts;
using SanitasFlow.Api.Models;

namespace SanitasFlow.Api.Controllers;

[ApiController]
[Route("api/donativos")]
public class DonativosController(AppDbContext context) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DonationResponse>> Create(CreateDonationRequest request, CancellationToken cancellationToken)
    {
        if (request.Monto is < 10 or > 100000 || decimal.Round(request.Monto, 2) != request.Monto)
            return BadRequest(new { message = "El monto debe estar entre $10 y $100,000 MXN, con hasta dos decimales." });

        var donativo = new Donativo { Monto = request.Monto };
        context.Donativos.Add(donativo);
        await context.SaveChangesAsync(cancellationToken);

        var response = new DonationResponse(donativo.Id, donativo.Monto, donativo.Moneda, donativo.Estado, donativo.CreadoEnUtc);
        return Created($"api/donativos/{donativo.Id}", response);
    }
}
