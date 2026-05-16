using ControlPlane.Application.Tenants.Commands.CreateTenant;
using ControlPlane.Application.Tenants.Commands.ReactivateTenant;
using ControlPlane.Application.Tenants.Commands.RemoveTenantSetting;
using ControlPlane.Application.Tenants.Commands.SetTenantSetting;
using ControlPlane.Application.Tenants.Commands.SuspendTenant;
using ControlPlane.Application.Tenants.Commands.UpdateTenant;
using ControlPlane.Application.Tenants.Queries.GetTenantById;
using ControlPlane.Application.Tenants.Queries.ListTenants;
using ControlPlane.Domain.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[Authorize]
public class TenantsAdminController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? name,
        [FromQuery] TenantStatus? status,
        [FromQuery] TenantPlan? plan,
        CancellationToken ct)
        => Ok(await sender.Send(new ListTenantsQuery(name, status, plan), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetTenantByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateTenantCommand(id, req.Name, req.CountryCode, req.CurrencyCode, req.Plan), ct));

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new SuspendTenantCommand(id), ct));

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new ReactivateTenantCommand(id), ct));

    [HttpPut("{id:guid}/settings/{key}")]
    public async Task<IActionResult> SetSetting(Guid id, string key, [FromBody] SetSettingRequest req, CancellationToken ct)
        => Ok(await sender.Send(new SetTenantSettingCommand(id, key, req.Value), ct));

    [HttpDelete("{id:guid}/settings/{key}")]
    public async Task<IActionResult> RemoveSetting(Guid id, string key, CancellationToken ct)
        => Ok(await sender.Send(new RemoveTenantSettingCommand(id, key), ct));
}

public record UpdateTenantRequest(string Name, string CountryCode, string CurrencyCode, TenantPlan Plan);
public record SetSettingRequest(string Value);
