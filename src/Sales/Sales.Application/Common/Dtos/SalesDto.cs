using Sales.Domain.Orders;
using Sales.Domain.Quotes;

namespace Sales.Application.Common.Dtos;

public record QuoteLineDto(
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

public record QuoteDto(
    Guid Id,
    string QuoteNumber,
    Guid CustomerId,
    QuoteStatus Status,
    DateOnly ValidUntil,
    string CurrencyCode,
    string CountryCode,
    IReadOnlyList<QuoteLineDto> Lines,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SalesOrderLineDto(
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

public record SalesOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    SalesOrderStatus Status,
    DateOnly OrderDate,
    DateOnly? RequestedDeliveryDate,
    string CurrencyCode,
    string CountryCode,
    Guid? OriginQuoteId,
    IReadOnlyList<SalesOrderLineDto> Lines,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
