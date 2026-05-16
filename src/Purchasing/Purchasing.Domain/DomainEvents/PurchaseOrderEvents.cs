using Api.Domain.Common;

namespace Purchasing.Domain.DomainEvents;

public record PurchaseOrderCreatedEvent(Guid PurchaseOrderId, Guid TenantId, string PoNumber,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record PurchaseOrderSentEvent(Guid PurchaseOrderId, Guid TenantId, string PoNumber,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record PurchaseOrderConfirmedEvent(Guid PurchaseOrderId, Guid TenantId, string PoNumber,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record PurchaseOrderLineReceivedEvent(Guid PurchaseOrderId, Guid TenantId, string PoNumber,
    Guid LineId, Guid CatalogItemId, string Sku, decimal QuantityReceived,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record PurchaseOrderCancelledEvent(Guid PurchaseOrderId, Guid TenantId, string PoNumber,
    string Reason, DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
