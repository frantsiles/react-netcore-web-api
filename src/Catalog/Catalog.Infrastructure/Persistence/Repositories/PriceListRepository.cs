using Catalog.Domain.Catalog;
using Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public class PriceListRepository(CatalogDbContext db) : IPriceListRepository
{
    public Task<PriceList?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.PriceLists.Include("_entries").FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PriceList?> GetDefaultAsync(CancellationToken ct = default)
        => db.PriceLists.Include("_entries").FirstOrDefaultAsync(x => x.IsDefault, ct);

    public async Task<IReadOnlyList<PriceList>> ListAllAsync(CancellationToken ct = default)
        => await db.PriceLists.ToListAsync(ct);

    public async Task<IReadOnlyList<PriceList>> GetActiveOnDateAsync(DateOnly date, CancellationToken ct = default)
        => await db.PriceLists.Include("_entries")
            .Where(x => x.ValidFrom <= date && (x.ValidTo == null || x.ValidTo.Value >= date))
            .ToListAsync(ct);

    public async Task AddAsync(PriceList priceList, CancellationToken ct = default)
    {
        await db.PriceLists.AddAsync(priceList, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PriceList priceList, CancellationToken ct = default)
    {
        db.PriceLists.Update(priceList);
        await db.SaveChangesAsync(ct);
    }
}
