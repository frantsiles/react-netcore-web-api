namespace BFF.Application.Invoicing;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record InvoiceLineBffDto(
    Guid Id, Guid? OriginSalesOrderLineId, Guid? CatalogItemId,
    string SKU, string Description, decimal Quantity,
    decimal UnitPrice, string CurrencyCode, decimal DiscountPercent, decimal LineTotal);

public record InvoiceBffDto(
    Guid Id, string InvoiceNumber, Guid CustomerId, Guid? OriginSalesOrderId,
    string Status, string IssueDate, string DueDate, string CurrencyCode,
    decimal Subtotal, decimal TaxAmount, decimal TotalAmount,
    decimal PaidAmount, decimal BalanceDue, string? Notes,
    IReadOnlyList<InvoiceLineBffDto> Lines, DateTime CreatedAt, DateTime UpdatedAt)
{
    public string CustomerName { get; init; } = "—";
}

// ── Queries ───────────────────────────────────────────────────────────────────

public record SearchInvoicesBffQuery(
    string Token, Guid? CustomerId, string? Status, Guid? OriginSalesOrderId,
    int Skip = 0, int Take = 50)
    : MediatR.IRequest<IReadOnlyList<InvoiceBffDto>>;

public record GetInvoiceByIdBffQuery(string Token, Guid InvoiceId)
    : MediatR.IRequest<InvoiceBffDto?>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record ConvertOrderToInvoiceBffCommand(
    string Token, Guid SalesOrderId, string DueDate, string? Notes)
    : MediatR.IRequest<InvoiceBffDto>;

public record IssueInvoiceBffCommand(string Token, Guid InvoiceId)
    : MediatR.IRequest<InvoiceBffDto>;

public record RecordInvoicePaymentBffCommand(
    string Token, Guid InvoiceId, decimal Amount, string CurrencyCode,
    string PaidAt, string? Reference)
    : MediatR.IRequest<InvoiceBffDto>;

public record CancelInvoiceBffCommand(string Token, Guid InvoiceId, string Reason)
    : MediatR.IRequest<InvoiceBffDto>;
