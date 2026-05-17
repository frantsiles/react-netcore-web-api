using BFF.Application.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/admin/tenants")]
[Authorize]
public class TenantsAdminController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? name,
        [FromQuery] string? status,
        [FromQuery] string? plan,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new ListTenantsBffQuery(GetToken(), name, status, plan), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTenantByIdBffQuery(GetToken(), id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateTenantBffCommand(GetToken(), req.Name, req.Slug, req.CountryCode, req.CurrencyCode, req.Plan), ct);
        return Created(string.Empty, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest req, CancellationToken ct)
        => Ok(await mediator.Send(
            new UpdateTenantBffCommand(GetToken(), id, req.Name, req.CountryCode, req.CurrencyCode, req.Plan), ct));

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new SuspendTenantBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ReactivateTenantBffCommand(GetToken(), id), ct));

    [HttpPut("{id:guid}/settings/{key}")]
    public async Task<IActionResult> SetSetting(Guid id, string key, [FromBody] SetSettingRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new SetTenantSettingBffCommand(GetToken(), id, key, req.Value), ct));

    [HttpDelete("{id:guid}/settings/{key}")]
    public async Task<IActionResult> RemoveSetting(Guid id, string key, CancellationToken ct)
        => Ok(await mediator.Send(new RemoveTenantSettingBffCommand(GetToken(), id, key), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreateTenantRequest(string Name, string Slug, string CountryCode, string CurrencyCode, string? Plan);
public record UpdateTenantRequest(string Name, string CountryCode, string CurrencyCode, string Plan);
public record SetSettingRequest(string Value);
