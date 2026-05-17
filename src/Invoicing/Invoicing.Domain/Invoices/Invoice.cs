using System;
using System.Collections.Generic;
using System.Linq;

namespace Invoicing.Domain.Invoices;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Draft"; // Draft, Issued, PartiallyPaid, Paid, Cancelled
    public List<InvoiceLine> Lines { get; set; } = new();

    public decimal Total => Lines.Sum(l => l.Amount);
}
