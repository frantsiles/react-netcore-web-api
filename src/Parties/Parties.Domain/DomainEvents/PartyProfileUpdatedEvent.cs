using Api.Domain.Common;

namespace Parties.Domain.DomainEvents;

public record PartyProfileUpdatedEvent(
    Guid PartyId,
    string LegalName,
    DateTime UpdatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
