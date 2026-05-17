using BFF.Application.Tax;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/tax/rates")]
[Authorize]
public class TaxController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] string? applicability, CancellationToken ct)
        => Ok(await mediator.Send(new ListTaxRatesBffQuery(GetToken(), status, applicability), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaxRateBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTaxRateBffCommand(
            GetToken(), req.Code, req.Name, req.Rate, req.Applicability, req.Description), ct);
        return Created(string.Empty, result);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new DeactivateTaxRateBffCommand(GetToken(), id), ct));

    [HttpGet("calculate")]
    public async Task<IActionResult> Calculate(
        [FromQuery] string taxCode, [FromQuery] decimal baseAmount, CancellationToken ct)
        => Ok(await mediator.Send(new CalculateTaxBffQuery(GetToken(), taxCode, baseAmount), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreateTaxRateBffRequest(
    string Code, string Name, decimal Rate, string Applicability, string? Description);
