using Api.Domain.Common;
using Catalog.Domain.DomainEvents;

namespace Catalog.Domain.Catalog;

public class PriceList : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = "";
    public string CurrencyCode { get; private set; } = "";
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsDefault { get; private set; }

    private readonly List<PriceListEntry> _entries = [];
    public IReadOnlyList<PriceListEntry> Entries => _entries.AsReadOnly();

    private PriceList() : base() { }

    private PriceList(string name, string currencyCode, DateOnly validFrom,
        DateOnly? validTo, bool isDefault) : base()
    {
        Name = name;
        CurrencyCode = currencyCode.ToUpperInvariant();
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsDefault = isDefault;
    }

    public static PriceList Create(string name, string currencyCode,
        DateOnly validFrom, DateOnly? validTo, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Price list name cannot be empty.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new DomainException("Currency code must be a 3-letter ISO 4217 code.");
        if (validTo.HasValue && validTo.Value <= validFrom)
            throw new DomainException("ValidTo must be after ValidFrom.");

        var list = new PriceList(name.Trim(), currencyCode, validFrom, validTo, isDefault);

        if (validFrom <= DateOnly.FromDateTime(DateTime.UtcNow))
            list.RaiseDomainEvent(new PriceListPublishedEvent(list.Id, list.Name, list.ValidFrom, DateTime.UtcNow));

        return list;
    }

    public void Update(string name, DateOnly validFrom, DateOnly? validTo)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Price list name cannot be empty.");
        if (validTo.HasValue && validTo.Value <= validFrom)
            throw new DomainException("ValidTo must be after ValidFrom.");

        Name = name.Trim();
        ValidFrom = validFrom;
        ValidTo = validTo;
        SetUpdatedAt();
    }

    public void SetAsDefault() { IsDefault = true; SetUpdatedAt(); }
    public void UnsetDefault() { IsDefault = false; SetUpdatedAt(); }

    public void AddEntry(PriceListEntry entry)
    {
        if (entry.UnitPrice.CurrencyCode != CurrencyCode)
            throw new DomainException($"Entry currency '{entry.UnitPrice.CurrencyCode}' must match price list currency '{CurrencyCode}'.");

        if (_entries.Any(e => e.CatalogItemId == entry.CatalogItemId && e.MinQuantity == entry.MinQuantity))
            throw new DomainException("A price entry for this item and minimum quantity already exists.");

        _entries.Add(entry);
        SetUpdatedAt();
    }

    public void UpdateEntry(Guid entryId, Money unitPrice, decimal minQuantity)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new DomainException($"Price entry '{entryId}' not found.");

        if (unitPrice.CurrencyCode != CurrencyCode)
            throw new DomainException($"Entry currency must match price list currency '{CurrencyCode}'.");

        var duplicate = _entries.Any(e => e.Id != entryId
            && e.CatalogItemId == entry.CatalogItemId
            && e.MinQuantity == minQuantity);
        if (duplicate) throw new DomainException("A price entry for this item and minimum quantity already exists.");

        entry.UpdatePrice(unitPrice);
        entry.UpdateMinQuantity(minQuantity);
        SetUpdatedAt();
    }

    public void RemoveEntry(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new DomainException($"Price entry '{entryId}' not found.");
        _entries.Remove(entry);
        SetUpdatedAt();
    }

    public bool IsActiveOn(DateOnly date) =>
        ValidFrom <= date && (ValidTo == null || ValidTo.Value >= date);
}
