using Invoicing.Domain.Invoices;

namespace Invoicing.Application.DTOs;

public record InvoiceLineDto(
    Guid Id,
    Guid? OriginSalesOrderLineId,
    Guid? CatalogItemId,
    string SKU,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    decimal LineTotal);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    Guid? OriginSalesOrderId,
    string Status,
    DateOnly IssueDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    string? Notes,
    IReadOnlyList<InvoiceLineDto> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public static class InvoiceMappingExtensions
{
    public static InvoiceDto ToDto(this Invoice inv) => new(
        inv.Id,
        inv.InvoiceNumber,
        inv.CustomerId,
        inv.OriginSalesOrderId,
        inv.Status.ToString(),
        inv.IssueDate,
        inv.DueDate,
        inv.CurrencyCode,
        inv.Subtotal.Amount,
        inv.TaxAmount.Amount,
        inv.TotalAmount.Amount,
        inv.PaidAmount.Amount,
        inv.BalanceDue.Amount,
        inv.Notes,
        inv.Lines.Select(l => new InvoiceLineDto(
            l.Id, l.OriginSalesOrderLineId, l.CatalogItemId,
            l.SKU, l.Description, l.Quantity,
            l.UnitPrice.Amount, l.UnitPrice.CurrencyCode,
            l.DiscountPercent, l.LineTotal.Amount)).ToList(),
        inv.CreatedAt,
        inv.UpdatedAt);
}
