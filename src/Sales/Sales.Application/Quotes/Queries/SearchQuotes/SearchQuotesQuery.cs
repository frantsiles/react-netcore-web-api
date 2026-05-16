using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Queries.SearchQuotes;

public record SearchQuotesQuery(
    Guid? CustomerId,
    QuoteStatus? Status,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int Skip = 0,
    int Take = 20) : IRequest<IReadOnlyList<QuoteDto>>;

public class SearchQuotesQueryHandler : IRequestHandler<SearchQuotesQuery, IReadOnlyList<QuoteDto>>
{
    private readonly IQuoteRepository _repo;

    public SearchQuotesQueryHandler(IQuoteRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<QuoteDto>> Handle(SearchQuotesQuery query, CancellationToken ct)
    {
        var results = await _repo.SearchAsync(
            query.CustomerId, query.Status,
            query.ValidFrom, query.ValidTo,
            query.Skip, query.Take, ct);
        return results.Select(q => q.ToDto()).ToList().AsReadOnly();
    }
}
