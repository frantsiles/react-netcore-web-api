using Api.Domain.Common;
using Inventory.Domain.DomainEvents;

namespace Inventory.Domain.Inventory;

public class InventoryItem : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CatalogItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string SKU { get; private set; } = "";
    public decimal QuantityOnHand { get; private set; }
    public decimal QuantityReserved { get; private set; }
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
    public decimal? ReorderPoint { get; private set; }

    private readonly List<StockMovement> _movements = [];
    public IReadOnlyList<StockMovement> Movements => _movements.AsReadOnly();

    private InventoryItem() : base() { }

    public static InventoryItem Create(Guid catalogItemId, Guid warehouseId,
        string sku, decimal? reorderPoint = null)
    {
        if (catalogItemId == Guid.Empty)
            throw new DomainException("CatalogItemId cannot be empty.");
        if (warehouseId == Guid.Empty)
            throw new DomainException("WarehouseId cannot be empty.");
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU cannot be empty.");
        if (reorderPoint.HasValue && reorderPoint.Value < 0)
            throw new DomainException("ReorderPoint cannot be negative.");

        return new InventoryItem
        {
            CatalogItemId = catalogItemId,
            WarehouseId = warehouseId,
            SKU = sku.ToUpperInvariant(),
            QuantityOnHand = 0,
            QuantityReserved = 0,
            ReorderPoint = reorderPoint
        };
    }

    public void Receive(decimal quantity, string? referenceNumber = null, string? notes = null)
    {
        if (quantity <= 0)
            throw new DomainException("Receipt quantity must be greater than zero.");

        var movement = StockMovement.Create(MovementType.Receipt, quantity, referenceNumber,
            notes: notes);
        _movements.Add(movement);
        QuantityOnHand += quantity;
        SetUpdatedAt();

        RaiseDomainEvent(new StockReceivedEvent(Id, CatalogItemId, WarehouseId, SKU,
            quantity, QuantityOnHand, referenceNumber, DateTimeOffset.UtcNow));

        CheckReorderPoint();
    }

    public void Adjust(decimal delta, string reason, string? notes = null)
    {
        if (delta == 0)
            throw new DomainException("Adjustment delta cannot be zero.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Adjustment reason is required.");
        if (QuantityOnHand + delta < 0)
            throw new DomainException("Adjustment would result in negative stock.");

        var movement = StockMovement.Create(MovementType.Adjustment, delta,
            reason: reason, notes: notes);
        _movements.Add(movement);
        QuantityOnHand += delta;
        SetUpdatedAt();

        RaiseDomainEvent(new StockAdjustedEvent(Id, CatalogItemId, WarehouseId, SKU,
            delta, QuantityOnHand, reason, DateTimeOffset.UtcNow));

        CheckReorderPoint();
    }

    public void Reserve(decimal quantity, Guid salesOrderId, string? orderNumber = null)
    {
        if (quantity <= 0)
            throw new DomainException("Reservation quantity must be greater than zero.");
        if (quantity > QuantityAvailable)
            throw new DomainException(
                $"Insufficient stock. Available: {QuantityAvailable}, Requested: {quantity}.");

        var movement = StockMovement.Create(MovementType.SalesReservation, quantity,
            referenceNumber: orderNumber, referenceId: salesOrderId);
        _movements.Add(movement);
        QuantityReserved += quantity;
        SetUpdatedAt();

        RaiseDomainEvent(new StockReservedEvent(Id, CatalogItemId, WarehouseId, SKU,
            quantity, QuantityAvailable, salesOrderId, DateTimeOffset.UtcNow));
    }

    public void ReleaseReservation(decimal quantity, Guid salesOrderId)
    {
        if (quantity <= 0)
            throw new DomainException("Release quantity must be greater than zero.");
        if (quantity > QuantityReserved)
            throw new DomainException(
                $"Cannot release more than reserved. Reserved: {QuantityReserved}.");

        var movement = StockMovement.Create(MovementType.SalesRelease, quantity,
            referenceId: salesOrderId);
        _movements.Add(movement);
        QuantityReserved -= quantity;
        SetUpdatedAt();
    }

    public void WriteOff(decimal quantity, string reason, string? notes = null)
    {
        if (quantity <= 0)
            throw new DomainException("Write-off quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Write-off reason is required.");
        if (quantity > QuantityOnHand)
            throw new DomainException(
                $"Cannot write off more than on hand. On hand: {QuantityOnHand}.");

        var movement = StockMovement.Create(MovementType.WriteOff, -quantity,
            reason: reason, notes: notes);
        _movements.Add(movement);
        QuantityOnHand -= quantity;
        SetUpdatedAt();

        RaiseDomainEvent(new StockWrittenOffEvent(Id, CatalogItemId, WarehouseId, SKU,
            quantity, QuantityOnHand, reason, DateTimeOffset.UtcNow));

        CheckReorderPoint();
    }

    public void UpdateReorderPoint(decimal? reorderPoint)
    {
        if (reorderPoint.HasValue && reorderPoint.Value < 0)
            throw new DomainException("ReorderPoint cannot be negative.");
        ReorderPoint = reorderPoint;
        SetUpdatedAt();
    }

    private void CheckReorderPoint()
    {
        if (ReorderPoint.HasValue && QuantityAvailable <= ReorderPoint.Value)
            RaiseDomainEvent(new LowStockAlertEvent(Id, CatalogItemId, WarehouseId, SKU,
                QuantityAvailable, ReorderPoint.Value, DateTimeOffset.UtcNow));
    }
}
