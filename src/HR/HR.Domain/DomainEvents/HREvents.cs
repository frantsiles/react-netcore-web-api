using Api.Domain.Common;

namespace HR.Domain.DomainEvents;

public record EmployeeHiredEvent(
    Guid EmployeeId,
    Guid TenantId,
    string EmployeeNumber,
    Guid DepartmentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public record EmployeeTerminatedEvent(
    Guid EmployeeId,
    Guid TenantId,
    string EmployeeNumber,
    DateOnly TerminationDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
