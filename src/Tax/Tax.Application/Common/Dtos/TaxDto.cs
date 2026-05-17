using Tax.Domain.TaxRates;

namespace Tax.Application.Common.Dtos;

public record TaxRateDto(
    Guid Id,
    string Code,
    string Name,
    decimal Rate,
    TaxApplicability Applicability,
    TaxRateStatus Status,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TaxCalculationDto(
    string TaxCode,
    decimal BaseAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalAmount);
