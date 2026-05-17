using System;

namespace Invoicing.Domain.Invoices;

public class InvoiceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? OriginSalesOrderLineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount => UnitPrice * Quantity;
}
