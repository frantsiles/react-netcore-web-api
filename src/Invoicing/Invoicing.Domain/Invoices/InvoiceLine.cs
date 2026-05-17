using Api.Domain.Common;

namespace Invoicing.Domain.Invoices;

public class InvoiceLine : Entity
{
    public Guid? OriginSalesOrderLineId { get; private set; }
    public Guid? CatalogItemId { get; private set; }
    public string SKU { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public decimal DiscountPercent { get; private set; }
    public Money LineTotal { get; private set; } = null!;

    private InvoiceLine() : base() { }

    public static InvoiceLine Create(
        string description, decimal quantity, Money unitPrice,
        decimal discountPercent = 0,
        Guid? catalogItemId = null,
        string sku = "",
        Guid? originSalesOrderLineId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Invoice line description cannot be empty.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (discountPercent < 0 || discountPercent > 100)
            throw new DomainException("Discount percent must be between 0 and 100.");

        var line = new InvoiceLine
        {
            Description             = description.Trim(),
            Quantity                = quantity,
            UnitPrice               = unitPrice,
            DiscountPercent         = discountPercent,
            CatalogItemId           = catalogItemId,
            SKU                     = sku.ToUpperInvariant(),
            OriginSalesOrderLineId  = originSalesOrderLineId,
        };
        line.LineTotal = line.ComputeTotal();
        return line;
    }

    private Money ComputeTotal() =>
        Money.Of(Math.Round(Quantity * UnitPrice.Amount * (1m - DiscountPercent / 100m), 2),
            UnitPrice.CurrencyCode);
}
