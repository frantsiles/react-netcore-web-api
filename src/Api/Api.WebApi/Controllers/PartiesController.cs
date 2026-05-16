using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parties.Application.Commands.RegisterParty;
using Parties.Application.Commands.UpdatePartyProfile;
using Parties.Application.Queries.GetPartyById;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/parties")]
[Authorize]
public class PartiesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPartyCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.PartyId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPartyByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(
        Guid id, [FromBody] UpdatePartyProfileRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdatePartyProfileCommand(id, request.LegalName, request.TradeName, request.TaxId), ct);
        return Ok(result);
    }
}

public record UpdatePartyProfileRequest(string LegalName, string? TradeName, string? TaxId);
