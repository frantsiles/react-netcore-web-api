using Api.Domain.Common;
using Tax.Domain.DomainEvents;

namespace Tax.Domain.TaxRates;

public class TaxRate : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal Rate { get; private set; }
    public TaxApplicability Applicability { get; private set; }
    public TaxRateStatus Status { get; private set; }
    public string? Description { get; private set; }

    private TaxRate() { }

    public static TaxRate Create(
        Guid tenantId,
        string code,
        string name,
        decimal rate,
        TaxApplicability applicability,
        string? description = null)
    {
        if (rate < 0 || rate > 100)
            throw new DomainException("Tax rate must be between 0 and 100.");

        var taxRate = new TaxRate
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            Rate = rate,
            Applicability = applicability,
            Status = TaxRateStatus.Active,
            Description = description
        };

        taxRate.RaiseDomainEvent(new TaxRateCreatedEvent(taxRate.Id, tenantId, code, rate));
        return taxRate;
    }

    public void Update(string name, decimal rate, TaxApplicability applicability, string? description)
    {
        if (rate < 0 || rate > 100)
            throw new DomainException("Tax rate must be between 0 and 100.");

        Name = name;
        Rate = rate;
        Applicability = applicability;
        Description = description;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        if (Status == TaxRateStatus.Inactive)
            throw new DomainException("Tax rate is already inactive.");
        Status = TaxRateStatus.Inactive;
        SetUpdatedAt();
    }

    public void Activate()
    {
        if (Status == TaxRateStatus.Active)
            throw new DomainException("Tax rate is already active.");
        Status = TaxRateStatus.Active;
        SetUpdatedAt();
    }

    public decimal Calculate(decimal baseAmount) =>
        Math.Round(baseAmount * Rate / 100, 2, MidpointRounding.AwayFromZero);
}
