using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Approvals.Application.Common.Dtos;
using Approvals.Application.Common.Mappings;
using Approvals.Domain.ApprovalRequests;
using Approvals.Domain.Repositories;
using MediatR;

namespace Approvals.Application.ApprovalRequests.Commands.CreateApprovalRequest;

public record CreateApprovalRequestCommand(
    string EntityType,
    Guid EntityId,
    string EntityReference,
    Guid RequestedByUserId,
    decimal? Amount,
    string? Notes) : IRequest<ApprovalRequestDto>;

public class CreateApprovalRequestHandler(
    IApprovalWorkflowRepository workflowRepo,
    IApprovalRequestRepository requestRepo,
    ITenantContext tenant)
    : IRequestHandler<CreateApprovalRequestCommand, ApprovalRequestDto>
{
    public async Task<ApprovalRequestDto> Handle(CreateApprovalRequestCommand cmd, CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByEntityTypeAsync(cmd.EntityType, ct)
            ?? throw new DomainException($"No active approval workflow for entity type '{cmd.EntityType}'.");

        if (!workflow.RequiresApproval(cmd.Amount))
            throw new DomainException("Entity does not meet the approval threshold for this workflow.");

        var request = ApprovalRequest.Create(
            tenant.TenantId, workflow.Id, cmd.EntityType, cmd.EntityId,
            cmd.EntityReference, cmd.RequestedByUserId, cmd.Amount, cmd.Notes);
        await requestRepo.AddAsync(request, ct);
        return request.ToDto();
    }
}
