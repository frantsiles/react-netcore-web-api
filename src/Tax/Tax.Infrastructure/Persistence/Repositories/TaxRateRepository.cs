using Microsoft.EntityFrameworkCore;
using Tax.Domain.Repositories;
using Tax.Domain.TaxRates;

namespace Tax.Infrastructure.Persistence.Repositories;

public class TaxRateRepository(TaxDbContext db) : ITaxRateRepository
{
    public async Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.TaxRates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TaxRate?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await db.TaxRates.FirstOrDefaultAsync(t => t.Code == code, ct);

    public async Task<List<TaxRate>> ListAsync(TaxRateStatus? status, TaxApplicability? applicability, CancellationToken ct = default)
    {
        var q = db.TaxRates.AsQueryable();
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (applicability.HasValue) q = q.Where(t =>
            t.Applicability == applicability.Value || t.Applicability == TaxApplicability.Both);
        return await q.OrderBy(t => t.Code).ToListAsync(ct);
    }

    public async Task AddAsync(TaxRate taxRate, CancellationToken ct = default)
    {
        await db.TaxRates.AddAsync(taxRate, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaxRate taxRate, CancellationToken ct = default)
    {
        db.TaxRates.Update(taxRate);
        await db.SaveChangesAsync(ct);
    }
}
