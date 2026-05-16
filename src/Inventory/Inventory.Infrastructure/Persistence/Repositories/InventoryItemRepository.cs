using Inventory.Domain.Inventory;
using Inventory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class InventoryItemRepository : IInventoryItemRepository
{
    private readonly InventoryDbContext _db;

    public InventoryItemRepository(InventoryDbContext db) => _db = db;

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.InventoryItems
            .Include("_movements")
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<InventoryItem?> GetByCatalogItemAndWarehouseAsync(
        Guid catalogItemId, Guid warehouseId, CancellationToken ct = default) =>
        await _db.InventoryItems
            .Include("_movements")
            .FirstOrDefaultAsync(
                i => i.CatalogItemId == catalogItemId && i.WarehouseId == warehouseId, ct);

    public async Task<bool> ExistsByCatalogItemAndWarehouseAsync(
        Guid catalogItemId, Guid warehouseId, CancellationToken ct = default) =>
        await _db.InventoryItems.AnyAsync(
            i => i.CatalogItemId == catalogItemId && i.WarehouseId == warehouseId, ct);

    public async Task<IReadOnlyList<InventoryItem>> SearchAsync(
        Guid? warehouseId, string? sku, bool? belowReorderPoint,
        int skip, int take, CancellationToken ct = default)
    {
        var query = _db.InventoryItems.Include("_movements").AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(sku))
            query = query.Where(i => i.SKU.Contains(sku.ToUpperInvariant()));
        if (belowReorderPoint == true)
            query = query.Where(i => i.ReorderPoint.HasValue &&
                                     (i.QuantityOnHand - i.QuantityReserved) <= i.ReorderPoint.Value);

        return await query
            .OrderBy(i => i.SKU)
            .Skip(skip).Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(InventoryItem item, CancellationToken ct = default)
    {
        await _db.InventoryItems.AddAsync(item, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(InventoryItem item, CancellationToken ct = default)
    {
        _db.InventoryItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }
}
