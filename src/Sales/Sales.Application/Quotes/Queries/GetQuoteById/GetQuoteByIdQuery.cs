using Api.Domain.Common;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Queries.GetQuoteById;

public record GetQuoteByIdQuery(Guid QuoteId) : IRequest<QuoteDto>;

public class GetQuoteByIdQueryHandler : IRequestHandler<GetQuoteByIdQuery, QuoteDto>
{
    private readonly IQuoteRepository _repo;

    public GetQuoteByIdQueryHandler(IQuoteRepository repo) => _repo = repo;

    public async Task<QuoteDto> Handle(GetQuoteByIdQuery query, CancellationToken ct)
    {
        var quote = await _repo.GetByIdAsync(query.QuoteId, ct)
            ?? throw new DomainException($"Quote '{query.QuoteId}' not found.");
        return quote.ToDto();
    }
}
