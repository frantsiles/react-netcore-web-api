using Inventory.Application.Common.Dtos;
using Inventory.Domain.Inventory;
using Inventory.Domain.Warehouses;

namespace Inventory.Application.Common.Mappings;

public static class InventoryMappingExtensions
{
    public static WarehouseDto ToDto(this Warehouse w) => new(
        w.Id, w.Code, w.Name, w.Address, w.Status, w.CreatedAt);

    public static InventoryItemDto ToDto(this InventoryItem item) => new(
        item.Id, item.CatalogItemId, item.WarehouseId, item.SKU,
        item.QuantityOnHand, item.QuantityReserved, item.QuantityAvailable,
        item.ReorderPoint,
        item.ReorderPoint.HasValue && item.QuantityAvailable <= item.ReorderPoint.Value,
        item.CreatedAt, item.UpdatedAt);

    public static StockMovementDto ToDto(this StockMovement m) => new(
        m.Id, m.Type, m.Quantity, m.ReferenceNumber, m.ReferenceId,
        m.Reason, m.Notes, m.OccurredAt);
}
