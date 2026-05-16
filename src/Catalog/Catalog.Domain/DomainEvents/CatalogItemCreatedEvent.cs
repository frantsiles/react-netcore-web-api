using Api.Domain.Common;

namespace Catalog.Domain.DomainEvents;

public record CatalogItemCreatedEvent(
    Guid CatalogItemId,
    string SKU,
    string Name,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
