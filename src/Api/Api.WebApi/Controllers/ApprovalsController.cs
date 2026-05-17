using Approvals.Application.ApprovalRequests.Commands.ApproveRequest;
using Approvals.Application.ApprovalRequests.Commands.CreateApprovalRequest;
using Approvals.Application.ApprovalRequests.Commands.RejectRequest;
using Approvals.Application.ApprovalRequests.Queries.GetApprovalRequest;
using Approvals.Application.ApprovalRequests.Queries.ListApprovalRequests;
using Approvals.Application.ApprovalWorkflows.Commands.CreateWorkflow;
using Approvals.Application.ApprovalWorkflows.Commands.UpdateWorkflow;
using Approvals.Domain.ApprovalRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController(ISender sender) : ControllerBase
{
    // ── Workflows ──────────────────────────────────────────────────────────────

    [HttpPost("workflows")]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowCommand cmd, CancellationToken ct)
        => Ok(await sender.Send(cmd, ct));

    [HttpPut("workflows/{id:guid}")]
    public async Task<IActionResult> UpdateWorkflow(Guid id, [FromBody] UpdateWorkflowRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateWorkflowCommand(id, req.Name, req.ApproverUserIds, req.AmountThreshold), ct));

    // ── Requests ───────────────────────────────────────────────────────────────

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateApprovalRequestCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetRequest), new { id = dto.Id }, dto);
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetApprovalRequestQuery(id), ct));

    [HttpGet("requests")]
    public async Task<IActionResult> ListRequests(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] ApprovalRequestStatus? status,
        CancellationToken ct)
        => Ok(await sender.Send(new ListApprovalRequestsQuery(entityType, entityId, status), ct));

    [HttpPost("requests/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] DecisionRequest req, CancellationToken ct)
        => Ok(await sender.Send(new ApproveRequestCommand(id, req.ApproverUserId, req.Notes), ct));

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] DecisionRequest req, CancellationToken ct)
        => Ok(await sender.Send(new RejectRequestCommand(id, req.ApproverUserId, req.Notes), ct));
}

public record UpdateWorkflowRequest(string Name, List<Guid> ApproverUserIds, decimal? AmountThreshold);
public record DecisionRequest(Guid ApproverUserId, string? Notes);
