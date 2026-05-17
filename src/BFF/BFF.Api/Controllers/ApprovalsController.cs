using BFF.Application.Approvals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/approvals/requests")]
[Authorize]
public class ApprovalsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? entityType,
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new ListApprovalRequestsBffQuery(GetToken(), entityType, status, skip, take), ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid id, [FromBody] ApprovalDecisionBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new ApproveRequestBffCommand(
            GetToken(), id, req.ApproverUserId, req.Notes), ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] ApprovalDecisionBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new RejectRequestBffCommand(
            GetToken(), id, req.ApproverUserId, req.Notes), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record ApprovalDecisionBffRequest(Guid ApproverUserId, string? Notes);
