using MediatR;

namespace BFF.Application.Sales;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record QuoteLineBffDto(
    Guid Id,
    Guid CatalogItemId,
    string SKU,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    decimal LineTotal,
    string? Notes);

public record QuoteBffDto(
    Guid Id,
    string QuoteNumber,
    Guid CustomerId,
    string Status,           // "Draft" | "Sent" | "Accepted" | "Rejected" | "Expired" | "ConvertedToOrder"
    DateOnly ValidUntil,
    string CurrencyCode,
    string CountryCode,
    IReadOnlyList<QuoteLineBffDto> Lines,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SalesOrderLineBffDto(
    Guid Id,
    Guid CatalogItemId,
    string SKU,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    decimal LineTotal,
    decimal FulfilledQuantity,
    string? Notes);

public record SalesOrderBffDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,           // "Draft" | "Confirmed" | "PartiallyFulfilled" | "Fulfilled" | "Invoiced" | "Cancelled"
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    string CurrencyCode,
    string CountryCode,
    Guid? OriginQuoteId,
    IReadOnlyList<SalesOrderLineBffDto> Lines,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ── Quote Queries ─────────────────────────────────────────────────────────────

public record SearchQuotesBffQuery(
    string BearerToken,
    Guid? CustomerId,
    string? Status,
    int Skip,
    int Take) : IRequest<IReadOnlyList<QuoteBffDto>>;

// ── Quote Commands ────────────────────────────────────────────────────────────

public record CreateQuoteBffCommand(
    string BearerToken,
    Guid CustomerId,
    DateOnly ValidUntil,
    string CurrencyCode,
    string CountryCode,
    string? Notes) : IRequest<QuoteBffDto>;

public record AddQuoteLineBffCommand(
    string BearerToken,
    Guid QuoteId,
    Guid CatalogItemId,
    string SKU,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    string? Notes) : IRequest<QuoteBffDto>;

public record SendQuoteBffCommand(
    string BearerToken,
    Guid QuoteId) : IRequest<QuoteBffDto>;

public record AcceptQuoteBffCommand(
    string BearerToken,
    Guid QuoteId) : IRequest<QuoteBffDto>;

public record RejectQuoteBffCommand(
    string BearerToken,
    Guid QuoteId) : IRequest<QuoteBffDto>;

public record ConvertQuoteToOrderBffCommand(
    string BearerToken,
    Guid QuoteId,
    DateOnly? RequestedDeliveryDate,
    string? Notes) : IRequest<SalesOrderBffDto>;

// ── Order Queries ─────────────────────────────────────────────────────────────

public record SearchSalesOrdersBffQuery(
    string BearerToken,
    Guid? CustomerId,
    string? Status,
    int Skip,
    int Take) : IRequest<IReadOnlyList<SalesOrderBffDto>>;

// ── Order Commands ────────────────────────────────────────────────────────────

public record ConfirmSalesOrderBffCommand(
    string BearerToken,
    Guid OrderId) : IRequest<SalesOrderBffDto>;

public record CancelSalesOrderBffCommand(
    string BearerToken,
    Guid OrderId,
    string Reason) : IRequest<SalesOrderBffDto>;
