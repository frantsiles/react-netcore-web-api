using Api.Domain.Common;
using Parties.Domain.Parties;

namespace Parties.Domain.DomainEvents;

public record PartyRoleActivatedEvent(
    Guid PartyId,
    PartyRoleType RoleType,
    DateTime ActivatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
