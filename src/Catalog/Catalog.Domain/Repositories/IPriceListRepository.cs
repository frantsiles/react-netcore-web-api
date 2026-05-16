using Catalog.Domain.Catalog;

namespace Catalog.Domain.Repositories;

public interface IPriceListRepository
{
    Task<PriceList?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PriceList?> GetDefaultAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PriceList>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PriceList>> GetActiveOnDateAsync(DateOnly date, CancellationToken ct = default);
    Task AddAsync(PriceList priceList, CancellationToken ct = default);
    Task UpdateAsync(PriceList priceList, CancellationToken ct = default);
}
