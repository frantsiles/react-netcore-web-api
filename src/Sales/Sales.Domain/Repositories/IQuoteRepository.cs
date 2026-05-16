using Sales.Domain.Quotes;

namespace Sales.Domain.Repositories;

public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNumberAsync(string quoteNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Quote>> SearchAsync(Guid? customerId, QuoteStatus? status,
        DateOnly? validFrom, DateOnly? validTo, int skip, int take, CancellationToken ct = default);
    Task AddAsync(Quote quote, CancellationToken ct = default);
    Task UpdateAsync(Quote quote, CancellationToken ct = default);
}
