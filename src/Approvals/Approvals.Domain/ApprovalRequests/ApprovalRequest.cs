using Api.Domain.Common;
using Approvals.Domain.DomainEvents;

namespace Approvals.Domain.ApprovalRequests;

public class ApprovalRequest : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid WorkflowId { get; private set; }
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string EntityReference { get; private set; } = default!;
    public decimal? Amount { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public ApprovalRequestStatus Status { get; private set; }
    public Guid? DecidedByUserId { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionNotes { get; private set; }

    private ApprovalRequest() { }

    public static ApprovalRequest Create(
        Guid tenantId,
        Guid workflowId,
        string entityType,
        Guid entityId,
        string entityReference,
        Guid requestedByUserId,
        decimal? amount = null,
        string? notes = null)
    {
        var req = new ApprovalRequest
        {
            TenantId = tenantId,
            WorkflowId = workflowId,
            EntityType = entityType,
            EntityId = entityId,
            EntityReference = entityReference,
            RequestedByUserId = requestedByUserId,
            Amount = amount,
            Notes = notes,
            Status = ApprovalRequestStatus.Pending
        };
        req.RaiseDomainEvent(new ApprovalRequestCreatedEvent(req.Id, tenantId, entityType, entityId, workflowId));
        return req;
    }

    public void Approve(Guid approverUserId, string? decisionNotes = null)
    {
        if (Status != ApprovalRequestStatus.Pending)
            throw new DomainException($"Cannot approve a request in status '{Status}'.");

        Status = ApprovalRequestStatus.Approved;
        DecidedByUserId = approverUserId;
        DecidedAt = DateTime.UtcNow;
        DecisionNotes = decisionNotes;
        SetUpdatedAt();
        RaiseDomainEvent(new ApprovalRequestDecidedEvent(Id, TenantId, EntityType, EntityId, true));
    }

    public void Reject(Guid approverUserId, string? decisionNotes = null)
    {
        if (Status != ApprovalRequestStatus.Pending)
            throw new DomainException($"Cannot reject a request in status '{Status}'.");

        Status = ApprovalRequestStatus.Rejected;
        DecidedByUserId = approverUserId;
        DecidedAt = DateTime.UtcNow;
        DecisionNotes = decisionNotes;
        SetUpdatedAt();
        RaiseDomainEvent(new ApprovalRequestDecidedEvent(Id, TenantId, EntityType, EntityId, false));
    }

    public void Cancel()
    {
        if (Status != ApprovalRequestStatus.Pending)
            throw new DomainException($"Cannot cancel a request in status '{Status}'.");
        Status = ApprovalRequestStatus.Cancelled;
        SetUpdatedAt();
    }
}
