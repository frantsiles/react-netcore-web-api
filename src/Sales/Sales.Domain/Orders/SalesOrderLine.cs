using Api.Domain.Common;

namespace Sales.Domain.Orders;

public class SalesOrderLine : Entity
{
    public Guid CatalogItemId { get; private set; }
    public string SKU { get; private set; } = "";
    public string ItemName { get; private set; } = "";
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public decimal DiscountPercent { get; private set; }
    public Money LineTotal { get; private set; } = null!;
    public decimal FulfilledQuantity { get; private set; }
    public string? Notes { get; private set; }

    private SalesOrderLine() : base() { }

    public static SalesOrderLine Create(Guid catalogItemId, string sku, string itemName,
        decimal quantity, Money unitPrice, decimal discountPercent = 0, string? notes = null)
    {
        if (catalogItemId == Guid.Empty)
            throw new DomainException("CatalogItemId cannot be empty.");
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU cannot be empty.");
        if (string.IsNullOrWhiteSpace(itemName))
            throw new DomainException("Item name cannot be empty.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new DomainException("Discount percent must be between 0 and 100.");

        var line = new SalesOrderLine
        {
            CatalogItemId = catalogItemId,
            SKU = sku.ToUpperInvariant(),
            ItemName = itemName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercent = discountPercent,
            FulfilledQuantity = 0,
            Notes = notes?.Trim()
        };
        line.LineTotal = line.ComputeTotal();
        return line;
    }

    internal void Update(decimal quantity, Money unitPrice, decimal discountPercent, string? notes)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new DomainException("Discount percent must be between 0 and 100.");

        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
        Notes = notes?.Trim();
        LineTotal = ComputeTotal();
    }

    private Money ComputeTotal() =>
        Money.Of(Math.Round(Quantity * UnitPrice.Amount * (1m - DiscountPercent / 100m), 2),
            UnitPrice.CurrencyCode);
}
