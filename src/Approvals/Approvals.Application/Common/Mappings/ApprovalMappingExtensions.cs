using Approvals.Application.Common.Dtos;
using Approvals.Domain.ApprovalRequests;
using Approvals.Domain.ApprovalWorkflows;

namespace Approvals.Application.Common.Mappings;

public static class ApprovalMappingExtensions
{
    public static ApprovalWorkflowDto ToDto(this ApprovalWorkflow w) =>
        new(w.Id, w.EntityType, w.Name, w.AmountThreshold, w.IsActive,
            w.ApproverUserIds.ToList(), w.CreatedAt, w.UpdatedAt);

    public static ApprovalRequestDto ToDto(this ApprovalRequest r) =>
        new(r.Id, r.WorkflowId, r.EntityType, r.EntityId, r.EntityReference,
            r.Amount, r.RequestedByUserId, r.Notes, r.Status,
            r.DecidedByUserId, r.DecidedAt, r.DecisionNotes, r.CreatedAt, r.UpdatedAt);
}
