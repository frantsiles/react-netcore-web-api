using Catalog.Domain.Catalog;

namespace Catalog.Domain.Repositories;

public interface ICatalogItemRepository
{
    Task<CatalogItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CatalogItem?> GetBySKUAsync(string sku, CancellationToken ct = default);
    Task<bool> ExistsBySKUAsync(string sku, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogItem>> SearchAsync(string? name, string? sku,
        ItemType? itemType, bool? isActive, int skip, int take, CancellationToken ct = default);
    Task AddAsync(CatalogItem item, CancellationToken ct = default);
    Task UpdateAsync(CatalogItem item, CancellationToken ct = default);
}
