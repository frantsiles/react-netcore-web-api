using Api.Domain.Common;

namespace ControlPlane.Domain.DomainEvents;

public record TenantCreatedEvent(
    Guid TenantId,
    string Name,
    string Slug,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
