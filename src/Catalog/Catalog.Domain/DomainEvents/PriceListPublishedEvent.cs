using Api.Domain.Common;

namespace Catalog.Domain.DomainEvents;

public record PriceListPublishedEvent(
    Guid PriceListId,
    string Name,
    DateOnly ValidFrom,
    DateTime PublishedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
