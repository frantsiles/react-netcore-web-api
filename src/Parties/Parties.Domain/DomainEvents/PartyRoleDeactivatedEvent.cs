using Api.Domain.Common;
using Parties.Domain.Parties;

namespace Parties.Domain.DomainEvents;

public record PartyRoleDeactivatedEvent(
    Guid PartyId,
    PartyRoleType RoleType,
    DateTime DeactivatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
