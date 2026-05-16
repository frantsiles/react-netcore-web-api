using Api.Domain.Common;

namespace Purchasing.Domain.PurchaseOrders;

public class PurchaseOrderLine : Entity
{
    public Guid CatalogItemId { get; private set; }
    public string Sku { get; private set; } = "";
    public string ItemName { get; private set; } = "";
    public decimal QuantityOrdered { get; private set; }
    public decimal QuantityReceived { get; private set; }
    public Money UnitCost { get; private set; } = null!;
    public Money LineTotal { get; private set; } = null!;
    public string? Notes { get; private set; }

    private PurchaseOrderLine() : base() { }

    public static PurchaseOrderLine Create(
        Guid catalogItemId,
        string sku,
        string itemName,
        decimal quantityOrdered,
        Money unitCost,
        string? notes = null)
    {
        if (quantityOrdered <= 0)
            throw new DomainException("Quantity ordered must be positive.");

        var lineTotal = Money.Of(Math.Round(quantityOrdered * unitCost.Amount, 2), unitCost.CurrencyCode);

        return new PurchaseOrderLine
        {
            CatalogItemId = catalogItemId,
            Sku = sku.Trim().ToUpperInvariant(),
            ItemName = itemName.Trim(),
            QuantityOrdered = quantityOrdered,
            QuantityReceived = 0,
            UnitCost = unitCost,
            LineTotal = lineTotal,
            Notes = notes?.Trim()
        };
    }

    internal void Update(decimal quantityOrdered, Money unitCost, string? notes)
    {
        if (quantityOrdered <= 0)
            throw new DomainException("Quantity ordered must be positive.");

        QuantityOrdered = quantityOrdered;
        UnitCost = unitCost;
        LineTotal = Money.Of(Math.Round(quantityOrdered * unitCost.Amount, 2), unitCost.CurrencyCode);
        Notes = notes?.Trim();
        SetUpdatedAt();
    }

    internal void AddReceived(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Received quantity must be positive.");
        if (QuantityReceived + quantity > QuantityOrdered)
            throw new DomainException($"Cannot receive more than ordered ({QuantityOrdered}) for '{Sku}'.");

        QuantityReceived += quantity;
        SetUpdatedAt();
    }
}
