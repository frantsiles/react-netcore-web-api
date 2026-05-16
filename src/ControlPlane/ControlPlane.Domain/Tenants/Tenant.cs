using Api.Domain.Common;
using ControlPlane.Domain.DomainEvents;

namespace ControlPlane.Domain.Tenants;

public class Tenant : AggregateRoot
{
    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public string CountryCode { get; private set; } = "";
    public string CurrencyCode { get; private set; } = "";
    public TenantPlan Plan { get; private set; }
    public TenantStatus Status { get; private set; }

    private readonly List<TenantSetting> _settings = [];
    public IReadOnlyList<TenantSetting> Settings => _settings.AsReadOnly();

    private Tenant() : base() { }

    private Tenant(Guid id) : base(id) { }

    public static Tenant Create(string name, string slug, string countryCode,
        string currencyCode, TenantPlan plan = TenantPlan.Free)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tenant name cannot be empty.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Tenant slug cannot be empty.");
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new DomainException("CountryCode must be a 2-letter ISO code.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new DomainException("CurrencyCode must be a 3-letter ISO code.");

        var tenant = new Tenant
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            CountryCode = countryCode.ToUpperInvariant(),
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Plan = plan,
            Status = TenantStatus.Active
        };
        tenant.RaiseDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.Name,
            tenant.Slug, DateTimeOffset.UtcNow));
        return tenant;
    }

    public static Tenant CreateWithId(Guid id, string name, string slug,
        string countryCode, string currencyCode, TenantPlan plan = TenantPlan.Free)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tenant name cannot be empty.");

        return new Tenant(id)
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            CountryCode = countryCode.ToUpperInvariant(),
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Plan = plan,
            Status = TenantStatus.Active
        };
    }

    public void Update(string name, string countryCode, string currencyCode, TenantPlan plan)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tenant name cannot be empty.");

        Name = name.Trim();
        CountryCode = countryCode.ToUpperInvariant();
        CurrencyCode = currencyCode.ToUpperInvariant();
        Plan = plan;
        SetUpdatedAt();
    }

    public void Suspend()
    {
        if (Status == TenantStatus.Suspended)
            throw new DomainException("Tenant is already suspended.");
        Status = TenantStatus.Suspended;
        SetUpdatedAt();
    }

    public void Reactivate()
    {
        if (Status == TenantStatus.Active)
            throw new DomainException("Tenant is already active.");
        Status = TenantStatus.Active;
        SetUpdatedAt();
    }

    public void SetSetting(string key, string value)
    {
        var existing = _settings.FirstOrDefault(s => s.Key == key.Trim());
        if (existing is not null)
            existing.UpdateValue(value);
        else
            _settings.Add(TenantSetting.Create(Id, key, value));
        SetUpdatedAt();
    }

    public void RemoveSetting(string key)
    {
        var setting = _settings.FirstOrDefault(s => s.Key == key.Trim())
            ?? throw new DomainException($"Setting '{key}' not found.");
        _settings.Remove(setting);
        SetUpdatedAt();
    }

    public string? GetSetting(string key) =>
        _settings.FirstOrDefault(s => s.Key == key.Trim())?.Value;
}
