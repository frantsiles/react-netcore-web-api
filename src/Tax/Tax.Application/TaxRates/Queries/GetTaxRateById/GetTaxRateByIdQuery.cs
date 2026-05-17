using Api.Domain.Common;
using MediatR;
using Tax.Application.Common.Dtos;
using Tax.Application.Common.Mappings;
using Tax.Domain.Repositories;

namespace Tax.Application.TaxRates.Queries.GetTaxRateById;

public record GetTaxRateByIdQuery(Guid Id) : IRequest<TaxRateDto>;

public class GetTaxRateByIdHandler(ITaxRateRepository repo)
    : IRequestHandler<GetTaxRateByIdQuery, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(GetTaxRateByIdQuery query, CancellationToken ct)
    {
        var taxRate = await repo.GetByIdAsync(query.Id, ct)
            ?? throw new DomainException($"Tax rate {query.Id} not found.");
        return taxRate.ToDto();
    }
}
