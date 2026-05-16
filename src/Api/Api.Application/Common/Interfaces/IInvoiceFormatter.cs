namespace Api.Application.Common.Interfaces;

public interface IInvoiceFormatter
{
    string CountryCode { get; }

    /// <summary>
    /// Returns the invoice document as a byte array (PDF or XML depending on country).
    /// USA → standard PDF invoice, MX → CFDI XML/PDF, CR → MH XML/PDF.
    /// </summary>
    Task<byte[]> FormatAsync(InvoiceFormatterRequest request, CancellationToken ct = default);
}

public sealed record InvoiceFormatterRequest(
    Guid InvoiceId,
    string SerialNumber,
    DateOnly IssueDate,
    string CustomerLegalName,
    string CustomerTaxId,
    IReadOnlyList<InvoiceLineItem> Lines,
    string CurrencyCode);

public sealed record InvoiceLineItem(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    string TaxCategory);
