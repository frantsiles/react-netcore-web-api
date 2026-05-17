using Api.Domain.Common;
using Invoicing.Domain.DomainEvents;

namespace Invoicing.Domain.Invoices;

public class Invoice : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; private set; } = "";
    public Guid CustomerId { get; private set; }
    public Guid? OriginSalesOrderId { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public string? Notes { get; private set; }

    private readonly List<InvoiceLine> _lines = [];
    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();

    public Money Subtotal { get; private set; } = null!;
    public Money TaxAmount { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    public Money PaidAmount { get; private set; } = null!;
    public Money BalanceDue { get; private set; } = null!;

    private Invoice() : base() { }

    public static Invoice Create(
        string invoiceNumber,
        Guid customerId,
        string currencyCode,
        DateOnly issueDate,
        DateOnly dueDate,
        Guid? originSalesOrderId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new DomainException("Invoice number cannot be empty.");
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new DomainException("CurrencyCode must be a 3-letter ISO code.");
        if (dueDate < issueDate)
            throw new DomainException("DueDate cannot be before IssueDate.");

        var invoice = new Invoice
        {
            InvoiceNumber       = invoiceNumber.Trim().ToUpperInvariant(),
            CustomerId          = customerId,
            CurrencyCode        = currencyCode.ToUpperInvariant(),
            IssueDate           = issueDate,
            DueDate             = dueDate,
            Status              = InvoiceStatus.Draft,
            OriginSalesOrderId  = originSalesOrderId,
            Notes               = notes?.Trim(),
            PaidAmount          = Money.Zero(currencyCode.ToUpperInvariant()),
        };
        invoice.RecalculateTotals();
        invoice.RaiseDomainEvent(new InvoiceCreatedEvent(invoice.Id, invoice.InvoiceNumber,
            invoice.CustomerId, invoice.OriginSalesOrderId, DateTimeOffset.UtcNow));
        return invoice;
    }

    public void AddLine(InvoiceLine line)
    {
        GuardEditable();
        if (line.UnitPrice.CurrencyCode != CurrencyCode)
            throw new DomainException($"Line currency '{line.UnitPrice.CurrencyCode}' does not match invoice currency '{CurrencyCode}'.");
        _lines.Add(line);
        RecalculateTotals();
        SetUpdatedAt();
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException($"Cannot issue an invoice in '{Status}' status. Must be Draft.");
        if (_lines.Count == 0)
            throw new DomainException("Cannot issue an invoice with no lines.");
        Status = InvoiceStatus.Issued;
        SetUpdatedAt();
        RaiseDomainEvent(new InvoiceIssuedEvent(Id, InvoiceNumber, CustomerId, TotalAmount, DueDate, DateTimeOffset.UtcNow));
    }

    public void RecordPayment(Money amount, DateOnly paidAt, string? reference = null)
    {
        if (Status != InvoiceStatus.Issued && Status != InvoiceStatus.PartiallyPaid)
            throw new DomainException($"Cannot record payment for invoice in '{Status}' status.");
        if (amount.CurrencyCode != CurrencyCode)
            throw new DomainException($"Payment currency '{amount.CurrencyCode}' does not match invoice currency '{CurrencyCode}'.");
        if (amount.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");
        if (amount.Amount > BalanceDue.Amount)
            throw new DomainException($"Payment amount {amount.Amount} exceeds balance due {BalanceDue.Amount}.");

        PaidAmount = PaidAmount.Add(amount);
        BalanceDue = TotalAmount.Subtract(PaidAmount);
        Status = BalanceDue.Amount == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        SetUpdatedAt();
        RaiseDomainEvent(new InvoicePaymentRecordedEvent(Id, InvoiceNumber, amount, PaidAmount, BalanceDue, paidAt, reference, DateTimeOffset.UtcNow));
    }

    public void Cancel(string reason)
    {
        if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.Voided)
            throw new DomainException($"Cannot cancel an invoice in '{Status}' status.");
        Status = InvoiceStatus.Cancelled;
        SetUpdatedAt();
        RaiseDomainEvent(new InvoiceCancelledEvent(Id, InvoiceNumber, reason, DateTimeOffset.UtcNow));
    }

    public void Void(string reason)
    {
        if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.Voided)
            throw new DomainException($"Cannot void an invoice in '{Status}' status.");
        Status = InvoiceStatus.Voided;
        SetUpdatedAt();
        RaiseDomainEvent(new InvoiceVoidedEvent(Id, InvoiceNumber, reason, DateTimeOffset.UtcNow));
    }

    private void GuardEditable()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException($"Invoice lines cannot be modified in '{Status}' status.");
    }

    private void RecalculateTotals()
    {
        Subtotal    = _lines.Count == 0
            ? Money.Zero(CurrencyCode)
            : _lines.Aggregate(Money.Zero(CurrencyCode), (sum, l) => sum.Add(l.LineTotal));
        TaxAmount   = Money.Zero(CurrencyCode);
        TotalAmount = Subtotal.Add(TaxAmount);
        BalanceDue  = TotalAmount.Subtract(PaidAmount ?? Money.Zero(CurrencyCode));
    }
}
