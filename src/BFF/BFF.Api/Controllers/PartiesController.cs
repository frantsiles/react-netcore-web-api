using BFF.Application.Parties;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/[controller]")]
[Authorize]
public class PartiesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? legalName,
        [FromQuery] string? roleType,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchPartiesBffQuery(GetToken(), legalName, roleType, isActive, skip, take), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPartyByIdBffQuery(GetToken(), id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPartyBffRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterPartyBffCommand(
            GetToken(),
            request.LegalName, request.TradeName,
            request.PartyType, request.CountryCode,
            request.FirstRoleType, request.TaxId,
            request.CreditLimit, request.CreditLimitCurrency,
            request.PaymentTermsDays, request.EmployeeNumber), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.PartyId }, result);
    }

    [HttpPatch("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(
        Guid id, [FromBody] UpdatePartyProfileBffRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdatePartyProfileBffCommand(GetToken(), id, request.LegalName, request.TradeName, request.TaxId), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivatePartyBffCommand(GetToken(), id), ct);
        return NoContent();
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record RegisterPartyBffRequest(
    string LegalName,
    string? TradeName,
    string PartyType,
    string CountryCode,
    string FirstRoleType,
    string? TaxId,
    decimal? CreditLimit,
    string? CreditLimitCurrency,
    int? PaymentTermsDays,
    string? EmployeeNumber);

public record UpdatePartyProfileBffRequest(
    string LegalName,
    string? TradeName,
    string? TaxId);
