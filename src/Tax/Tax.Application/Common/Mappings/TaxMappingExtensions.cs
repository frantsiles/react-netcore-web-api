using Tax.Application.Common.Dtos;
using Tax.Domain.TaxRates;

namespace Tax.Application.Common.Mappings;

public static class TaxMappingExtensions
{
    public static TaxRateDto ToDto(this TaxRate r) =>
        new(r.Id, r.Code, r.Name, r.Rate, r.Applicability, r.Status, r.Description, r.CreatedAt, r.UpdatedAt);
}
