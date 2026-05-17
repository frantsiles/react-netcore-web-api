using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tax.Application.TaxRates.Commands.CreateTaxRate;
using Tax.Application.TaxRates.Commands.DeactivateTaxRate;
using Tax.Application.TaxRates.Commands.UpdateTaxRate;
using Tax.Application.TaxRates.Queries.CalculateTax;
using Tax.Application.TaxRates.Queries.GetTaxRateById;
using Tax.Application.TaxRates.Queries.ListTaxRates;
using Tax.Domain.TaxRates;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/tax/rates")]
[Authorize]
public class TaxRatesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaxRateCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetTaxRateByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] TaxRateStatus? status,
        [FromQuery] TaxApplicability? applicability,
        CancellationToken ct)
        => Ok(await sender.Send(new ListTaxRatesQuery(status, applicability), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxRateRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateTaxRateCommand(id, req.Name, req.Rate, req.Applicability, req.Description), ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new DeactivateTaxRateCommand(id), ct));

    [HttpGet("calculate")]
    public async Task<IActionResult> Calculate(
        [FromQuery] string taxCode,
        [FromQuery] decimal baseAmount,
        CancellationToken ct)
        => Ok(await sender.Send(new CalculateTaxQuery(taxCode, baseAmount), ct));
}

public record UpdateTaxRateRequest(string Name, decimal Rate, TaxApplicability Applicability, string? Description);
