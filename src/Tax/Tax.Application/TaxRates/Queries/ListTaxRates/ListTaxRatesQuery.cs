using MediatR;
using Tax.Application.Common.Dtos;
using Tax.Application.Common.Mappings;
using Tax.Domain.Repositories;
using Tax.Domain.TaxRates;

namespace Tax.Application.TaxRates.Queries.ListTaxRates;

public record ListTaxRatesQuery(
    TaxRateStatus? Status,
    TaxApplicability? Applicability) : IRequest<List<TaxRateDto>>;

public class ListTaxRatesHandler(ITaxRateRepository repo)
    : IRequestHandler<ListTaxRatesQuery, List<TaxRateDto>>
{
    public async Task<List<TaxRateDto>> Handle(ListTaxRatesQuery query, CancellationToken ct)
    {
        var rates = await repo.ListAsync(query.Status, query.Applicability, ct);
        return rates.Select(r => r.ToDto()).ToList();
    }
}
