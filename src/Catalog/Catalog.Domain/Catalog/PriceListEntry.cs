using Api.Domain.Common;

namespace Catalog.Domain.Catalog;

public class PriceListEntry : Entity
{
    public Guid CatalogItemId { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public decimal MinQuantity { get; private set; }

    private PriceListEntry() : base() { }

    private PriceListEntry(Guid catalogItemId, Money unitPrice, decimal minQuantity) : base()
    {
        CatalogItemId = catalogItemId;
        UnitPrice = unitPrice;
        MinQuantity = minQuantity;
    }

    public static PriceListEntry Create(Guid catalogItemId, Money unitPrice, decimal minQuantity = 1m)
    {
        if (catalogItemId == Guid.Empty) throw new DomainException("CatalogItemId cannot be empty.");
        if (minQuantity <= 0) throw new DomainException("Min quantity must be greater than zero.");

        return new PriceListEntry(catalogItemId, unitPrice, minQuantity);
    }

    internal void UpdatePrice(Money unitPrice) { UnitPrice = unitPrice; SetUpdatedAt(); }
    internal void UpdateMinQuantity(decimal minQuantity)
    {
        if (minQuantity <= 0) throw new DomainException("Min quantity must be greater than zero.");
        MinQuantity = minQuantity;
        SetUpdatedAt();
    }
}
