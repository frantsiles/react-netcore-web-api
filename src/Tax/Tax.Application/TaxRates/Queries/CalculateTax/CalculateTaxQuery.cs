using Api.Domain.Common;
using MediatR;
using Tax.Application.Common.Dtos;
using Tax.Domain.Repositories;

namespace Tax.Application.TaxRates.Queries.CalculateTax;

public record CalculateTaxQuery(string TaxCode, decimal BaseAmount) : IRequest<TaxCalculationDto>;

public class CalculateTaxHandler(ITaxRateRepository repo)
    : IRequestHandler<CalculateTaxQuery, TaxCalculationDto>
{
    public async Task<TaxCalculationDto> Handle(CalculateTaxQuery query, CancellationToken ct)
    {
        var taxRate = await repo.GetByCodeAsync(query.TaxCode, ct)
            ?? throw new DomainException($"Tax rate code '{query.TaxCode}' not found.");

        var taxAmount = taxRate.Calculate(query.BaseAmount);
        return new TaxCalculationDto(
            query.TaxCode,
            query.BaseAmount,
            taxRate.Rate,
            taxAmount,
            query.BaseAmount + taxAmount);
    }
}
