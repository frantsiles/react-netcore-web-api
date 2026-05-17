namespace Invoicing.Domain.Invoices;

public enum InvoiceStatus
{
    Draft,
    Issued,
    PartiallyPaid,
    Paid,
    Cancelled,
    Voided
}
