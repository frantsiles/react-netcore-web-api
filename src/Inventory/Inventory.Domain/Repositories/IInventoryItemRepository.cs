using Inventory.Domain.Inventory;

namespace Inventory.Domain.Repositories;

public interface IInventoryItemRepository
{
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryItem?> GetByCatalogItemAndWarehouseAsync(Guid catalogItemId, Guid warehouseId,
        CancellationToken ct = default);
    Task<bool> ExistsByCatalogItemAndWarehouseAsync(Guid catalogItemId, Guid warehouseId,
        CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItem>> SearchAsync(Guid? warehouseId, string? sku,
        bool? belowReorderPoint, int skip, int take, CancellationToken ct = default);
    Task AddAsync(InventoryItem item, CancellationToken ct = default);
    Task UpdateAsync(InventoryItem item, CancellationToken ct = default);
}
