using Tax.Domain.TaxRates;

namespace Tax.Domain.Repositories;

public interface ITaxRateRepository
{
    Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaxRate?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<TaxRate>> ListAsync(TaxRateStatus? status, TaxApplicability? applicability, CancellationToken ct = default);
    Task AddAsync(TaxRate taxRate, CancellationToken ct = default);
    Task UpdateAsync(TaxRate taxRate, CancellationToken ct = default);
}
