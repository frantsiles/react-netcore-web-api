using Api.Domain.Common;

namespace Inventory.Domain.DomainEvents;

public record WarehouseCreatedEvent(
    Guid WarehouseId,
    string Code,
    string Name,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record StockReceivedEvent(
    Guid InventoryItemId,
    Guid CatalogItemId,
    Guid WarehouseId,
    string SKU,
    decimal Quantity,
    decimal NewQuantityOnHand,
    string? ReferenceNumber,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record StockAdjustedEvent(
    Guid InventoryItemId,
    Guid CatalogItemId,
    Guid WarehouseId,
    string SKU,
    decimal Delta,
    decimal NewQuantityOnHand,
    string Reason,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record StockReservedEvent(
    Guid InventoryItemId,
    Guid CatalogItemId,
    Guid WarehouseId,
    string SKU,
    decimal Quantity,
    decimal QuantityAvailableAfter,
    Guid SalesOrderId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record StockWrittenOffEvent(
    Guid InventoryItemId,
    Guid CatalogItemId,
    Guid WarehouseId,
    string SKU,
    decimal Quantity,
    decimal NewQuantityOnHand,
    string Reason,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record LowStockAlertEvent(
    Guid InventoryItemId,
    Guid CatalogItemId,
    Guid WarehouseId,
    string SKU,
    decimal QuantityAvailable,
    decimal ReorderPoint,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
