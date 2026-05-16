using Catalog.Domain.Catalog;
using Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public class CatalogItemRepository(CatalogDbContext db) : ICatalogItemRepository
{
    public Task<CatalogItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.CatalogItems.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<CatalogItem?> GetBySKUAsync(string sku, CancellationToken ct = default)
        => db.CatalogItems.FirstOrDefaultAsync(x => x.SKU == sku, ct);

    public Task<bool> ExistsBySKUAsync(string sku, CancellationToken ct = default)
        => db.CatalogItems.AnyAsync(x => x.SKU == sku, ct);

    public async Task<IReadOnlyList<CatalogItem>> SearchAsync(
        string? name, string? sku, ItemType? itemType, bool? isActive,
        int skip, int take, CancellationToken ct = default)
    {
        var q = db.CatalogItems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(x => x.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(sku)) q = q.Where(x => x.SKU.Contains(sku));
        if (itemType.HasValue) q = q.Where(x => x.ItemType == itemType.Value);
        if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive.Value);
        return await q.Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task AddAsync(CatalogItem item, CancellationToken ct = default)
    {
        await db.CatalogItems.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CatalogItem item, CancellationToken ct = default)
    {
        db.CatalogItems.Update(item);
        await db.SaveChangesAsync(ct);
    }
}
