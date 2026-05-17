namespace BFF.Application.Approvals;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ApprovalWorkflowBffDto(
    Guid Id, string EntityType, string Name, decimal? AmountThreshold,
    bool IsActive, List<Guid> ApproverUserIds, DateTime CreatedAt, DateTime UpdatedAt);

public record ApprovalRequestBffDto(
    Guid Id, Guid WorkflowId, string EntityType, Guid EntityId,
    string EntityReference, decimal? Amount, Guid RequestedByUserId, string? Notes,
    string Status, Guid? DecidedByUserId, DateTime? DecidedAt, string? DecisionNotes,
    DateTime CreatedAt, DateTime UpdatedAt);

// ── Queries ───────────────────────────────────────────────────────────────────

public record ListApprovalRequestsBffQuery(
    string Token, string? EntityType, string? Status, int Skip, int Take)
    : MediatR.IRequest<IReadOnlyList<ApprovalRequestBffDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record ApproveRequestBffCommand(
    string Token, Guid RequestId, Guid ApproverUserId, string? Notes)
    : MediatR.IRequest<ApprovalRequestBffDto>;

public record RejectRequestBffCommand(
    string Token, Guid RequestId, Guid ApproverUserId, string? Notes)
    : MediatR.IRequest<ApprovalRequestBffDto>;
