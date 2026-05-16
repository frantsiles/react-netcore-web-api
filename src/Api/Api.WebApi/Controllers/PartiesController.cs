using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parties.Application.Commands.ActivateRole;
using Parties.Application.Commands.AddAddress;
using Parties.Application.Commands.AddContactPoint;
using Parties.Application.Commands.DeactivateParty;
using Parties.Application.Commands.DeactivateRole;
using Parties.Application.Commands.RegisterParty;
using Parties.Application.Commands.RemoveAddress;
using Parties.Application.Commands.RemoveContactPoint;
using Parties.Application.Commands.UpdatePartyProfile;
using Parties.Application.Queries.GetPartyById;
using Parties.Application.Queries.SearchParties;
using Parties.Domain.Parties;
using Parties.Domain.ValueObjects;

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

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? legalName,
        [FromQuery] PartyRoleType? roleType,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchPartiesQuery(legalName, roleType, isActive, skip, take), ct);
        return Ok(result);
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

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> AddAddress(
        Guid id, [FromBody] AddAddressRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new AddAddressCommand(id, request.Line1, request.Line2, request.City,
                request.StateOrRegion, request.PostalCode, request.CountryCode), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/addresses/{index:int}")]
    public async Task<IActionResult> RemoveAddress(Guid id, int index, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveAddressCommand(id, index), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/contact-points")]
    public async Task<IActionResult> AddContactPoint(
        Guid id, [FromBody] AddContactPointRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new AddContactPointCommand(id, request.Type, request.Value, request.IsPrimary), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/contact-points/{index:int}")]
    public async Task<IActionResult> RemoveContactPoint(Guid id, int index, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveContactPointCommand(id, index), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/roles/{roleType}/activate")]
    public async Task<IActionResult> ActivateRole(
        Guid id, PartyRoleType roleType,
        [FromBody] ActivateRoleRequest? request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ActivateRoleCommand(id, roleType,
                request?.CreditLimit, request?.CreditLimitCurrency,
                request?.PaymentTermsDays, request?.EmployeeNumber), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/roles/{roleType}/deactivate")]
    public async Task<IActionResult> DeactivateRole(
        Guid id, PartyRoleType roleType, CancellationToken ct)
    {
        var result = await mediator.Send(new DeactivateRoleCommand(id, roleType), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivatePartyCommand(id), ct);
        return NoContent();
    }
}

public record UpdatePartyProfileRequest(string LegalName, string? TradeName, string? TaxId);
public record AddAddressRequest(string Line1, string? Line2, string City,
    string StateOrRegion, string PostalCode, string CountryCode);
public record AddContactPointRequest(ContactPointType Type, string Value, bool IsPrimary = false);
public record ActivateRoleRequest(decimal? CreditLimit, string? CreditLimitCurrency,
    int? PaymentTermsDays, string? EmployeeNumber);
