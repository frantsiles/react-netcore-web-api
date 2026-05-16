using Microsoft.EntityFrameworkCore;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.Infrastructure.Persistence.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly SalesDbContext _db;

    public QuoteRepository(SalesDbContext db) => _db = db;

    public async Task<Quote?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Quotes
            .Include("_lines")
            .FirstOrDefaultAsync(q => q.Id == id, ct);

    public async Task<bool> ExistsByNumberAsync(string quoteNumber, CancellationToken ct = default) =>
        await _db.Quotes.AnyAsync(q => q.QuoteNumber == quoteNumber.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Quote>> SearchAsync(Guid? customerId, QuoteStatus? status,
        DateOnly? validFrom, DateOnly? validTo, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.Quotes.Include("_lines").AsQueryable();

        if (customerId.HasValue)
            query = query.Where(q => q.CustomerId == customerId.Value);
        if (status.HasValue)
            query = query.Where(q => q.Status == status.Value);
        if (validFrom.HasValue)
            query = query.Where(q => q.ValidUntil >= validFrom.Value);
        if (validTo.HasValue)
            query = query.Where(q => q.ValidUntil <= validTo.Value);

        return await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Quote quote, CancellationToken ct = default)
    {
        await _db.Quotes.AddAsync(quote, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Quote quote, CancellationToken ct = default)
    {
        _db.Quotes.Update(quote);
        await _db.SaveChangesAsync(ct);
    }
}
