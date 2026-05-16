using Api.Domain.Common;
using Parties.Domain.Parties;

namespace Parties.Domain.DomainEvents;

public record PartyRegisteredEvent(
    Guid PartyId,
    string LegalName,
    DateTime RegisteredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
