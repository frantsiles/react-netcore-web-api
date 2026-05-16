using Api.Domain.Common;
using Catalog.Domain.DomainEvents;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Catalog;

public class CatalogItem : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string CountryCode { get; private set; } = "";
    public ItemType ItemType { get; private set; }
    public string SKU { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; } = null!;
    public string TaxCategory { get; private set; } = "STANDARD";
    public string DefaultCurrency { get; private set; } = "";
    public bool IsActive { get; private set; }
    public bool TrackInventory { get; private set; }
    public decimal? ReorderPoint { get; private set; }

    private CatalogItem() : base() { }

    private CatalogItem(string sku, string name, string? description, ItemType itemType,
        UnitOfMeasure unitOfMeasure, string taxCategory, string defaultCurrency,
        string countryCode, bool trackInventory, decimal? reorderPoint) : base()
    {
        SKU = sku;
        Name = name;
        Description = description;
        ItemType = itemType;
        UnitOfMeasure = unitOfMeasure;
        TaxCategory = taxCategory;
        DefaultCurrency = defaultCurrency.ToUpperInvariant();
        CountryCode = countryCode.ToUpperInvariant();
        IsActive = true;
        TrackInventory = trackInventory;
        ReorderPoint = reorderPoint;
    }

    public static CatalogItem Create(string sku, string name, string? description,
        ItemType itemType, UnitOfMeasure unitOfMeasure, string taxCategory,
        string defaultCurrency, string countryCode,
        bool trackInventory = false, decimal? reorderPoint = null)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU cannot be empty.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name cannot be empty.");
        if (string.IsNullOrWhiteSpace(defaultCurrency) || defaultCurrency.Trim().Length != 3)
            throw new DomainException("Default currency must be a 3-letter ISO 4217 code.");
        if (trackInventory && itemType == ItemType.Service)
            throw new DomainException("Services cannot track inventory.");
        if (reorderPoint.HasValue && reorderPoint.Value < 0)
            throw new DomainException("Reorder point cannot be negative.");

        var item = new CatalogItem(sku.Trim().ToUpperInvariant(), name.Trim(), description?.Trim(),
            itemType, unitOfMeasure, taxCategory, defaultCurrency, countryCode,
            trackInventory, reorderPoint);

        item.RaiseDomainEvent(new CatalogItemCreatedEvent(item.Id, item.SKU, item.Name, DateTime.UtcNow));
        return item;
    }

    public void Update(string name, string? description, UnitOfMeasure unitOfMeasure, string taxCategory)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name cannot be empty.");

        Name = name.Trim();
        Description = description?.Trim();
        UnitOfMeasure = unitOfMeasure;
        TaxCategory = taxCategory;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
        RaiseDomainEvent(new CatalogItemDeactivatedEvent(Id, SKU, DateTime.UtcNow));
    }
}
