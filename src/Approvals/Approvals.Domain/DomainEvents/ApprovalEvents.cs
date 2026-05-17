using Api.Domain.Common;

namespace Approvals.Domain.DomainEvents;

public record ApprovalRequestCreatedEvent(
    Guid RequestId,
    Guid TenantId,
    string EntityType,
    Guid EntityId,
    Guid WorkflowId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record ApprovalRequestDecidedEvent(
    Guid RequestId,
    Guid TenantId,
    string EntityType,
    Guid EntityId,
    bool Approved) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
