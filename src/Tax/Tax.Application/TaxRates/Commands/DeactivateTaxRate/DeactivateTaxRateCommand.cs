using Api.Domain.Common;
using MediatR;
using Tax.Application.Common.Dtos;
using Tax.Application.Common.Mappings;
using Tax.Domain.Repositories;

namespace Tax.Application.TaxRates.Commands.DeactivateTaxRate;

public record DeactivateTaxRateCommand(Guid Id) : IRequest<TaxRateDto>;

public class DeactivateTaxRateHandler(ITaxRateRepository repo)
    : IRequestHandler<DeactivateTaxRateCommand, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(DeactivateTaxRateCommand cmd, CancellationToken ct)
    {
        var taxRate = await repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new DomainException($"Tax rate {cmd.Id} not found.");
        taxRate.Deactivate();
        await repo.UpdateAsync(taxRate, ct);
        return taxRate.ToDto();
    }
}
