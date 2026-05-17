namespace BFF.Application.Tax;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record TaxRateBffDto(
    Guid Id, string Code, string Name, decimal Rate,
    string Applicability, string Status, string? Description,
    DateTime CreatedAt, DateTime UpdatedAt);

public record TaxCalculationBffDto(
    string TaxCode, decimal BaseAmount, decimal TaxRate,
    decimal TaxAmount, decimal TotalAmount);

// ── Queries ───────────────────────────────────────────────────────────────────

public record ListTaxRatesBffQuery(string Token, string? Status, string? Applicability)
    : MediatR.IRequest<IReadOnlyList<TaxRateBffDto>>;

public record CalculateTaxBffQuery(string Token, string TaxCode, decimal BaseAmount)
    : MediatR.IRequest<TaxCalculationBffDto>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateTaxRateBffCommand(
    string Token, string Code, string Name, decimal Rate,
    string Applicability, string? Description)
    : MediatR.IRequest<TaxRateBffDto>;

public record UpdateTaxRateBffCommand(
    string Token, Guid Id, string Name, decimal Rate,
    string Applicability, string? Description)
    : MediatR.IRequest<TaxRateBffDto>;

public record DeactivateTaxRateBffCommand(string Token, Guid Id)
    : MediatR.IRequest<TaxRateBffDto>;
