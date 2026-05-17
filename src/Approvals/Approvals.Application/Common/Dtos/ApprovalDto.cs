using Approvals.Domain.ApprovalRequests;

namespace Approvals.Application.Common.Dtos;

public record ApprovalWorkflowDto(
    Guid Id,
    string EntityType,
    string Name,
    decimal? AmountThreshold,
    bool IsActive,
    List<Guid> ApproverUserIds,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ApprovalRequestDto(
    Guid Id,
    Guid WorkflowId,
    string EntityType,
    Guid EntityId,
    string EntityReference,
    decimal? Amount,
    Guid RequestedByUserId,
    string? Notes,
    ApprovalRequestStatus Status,
    Guid? DecidedByUserId,
    DateTime? DecidedAt,
    string? DecisionNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
