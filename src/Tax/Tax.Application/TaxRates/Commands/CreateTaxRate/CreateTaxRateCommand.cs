using Api.Application.Common.Interfaces;
using MediatR;
using Tax.Application.Common.Dtos;
using Tax.Application.Common.Mappings;
using Tax.Domain.Repositories;
using Tax.Domain.TaxRates;

namespace Tax.Application.TaxRates.Commands.CreateTaxRate;

public record CreateTaxRateCommand(
    string Code,
    string Name,
    decimal Rate,
    TaxApplicability Applicability,
    string? Description) : IRequest<TaxRateDto>;

public class CreateTaxRateHandler(ITaxRateRepository repo, ITenantContext tenant)
    : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(CreateTaxRateCommand cmd, CancellationToken ct)
    {
        var existing = await repo.GetByCodeAsync(cmd.Code, ct);
        if (existing != null)
            throw new InvalidOperationException($"Tax rate code '{cmd.Code}' already exists.");

        var taxRate = TaxRate.Create(tenant.TenantId, cmd.Code, cmd.Name, cmd.Rate, cmd.Applicability, cmd.Description);
        await repo.AddAsync(taxRate, ct);
        return taxRate.ToDto();
    }
}
