using Api.Domain.Common;
using MediatR;
using Tax.Application.Common.Dtos;
using Tax.Application.Common.Mappings;
using Tax.Domain.Repositories;
using Tax.Domain.TaxRates;

namespace Tax.Application.TaxRates.Commands.UpdateTaxRate;

public record UpdateTaxRateCommand(
    Guid Id,
    string Name,
    decimal Rate,
    TaxApplicability Applicability,
    string? Description) : IRequest<TaxRateDto>;

public class UpdateTaxRateHandler(ITaxRateRepository repo)
    : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand cmd, CancellationToken ct)
    {
        var taxRate = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new DomainException($"Tax rate {cmd.Id} not found.");
        taxRate.Update(cmd.Name, cmd.Rate, cmd.Applicability, cmd.Description);
        await repo.UpdateAsync(taxRate, ct);
        return taxRate.ToDto();
    }
}
