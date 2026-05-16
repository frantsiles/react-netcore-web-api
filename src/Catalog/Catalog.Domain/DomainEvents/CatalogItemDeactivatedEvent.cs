using Api.Domain.Common;

namespace Catalog.Domain.DomainEvents;

public record CatalogItemDeactivatedEvent(
    Guid CatalogItemId,
    string SKU,
    DateTime DeactivatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
