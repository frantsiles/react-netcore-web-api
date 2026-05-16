using Sales.Application.Common.Dtos;
using Sales.Domain.Orders;
using Sales.Domain.Quotes;

namespace Sales.Application.Common.Mappings;

public static class SalesMappingExtensions
{
    public static QuoteDto ToDto(this Quote q) => new(
        q.Id, q.QuoteNumber, q.CustomerId, q.Status, q.ValidUntil,
        q.CurrencyCode, q.CountryCode,
        q.Lines.Select(l => l.ToDto()).ToList().AsReadOnly(),
        q.Subtotal.Amount, q.TaxAmount.Amount, q.Total.Amount,
        q.Notes, q.CreatedAt, q.UpdatedAt);

    public static QuoteLineDto ToDto(this QuoteLine l) => new(
        l.Id, l.CatalogItemId, l.SKU, l.ItemName, l.Quantity,
        l.UnitPrice.Amount, l.UnitPrice.CurrencyCode,
        l.DiscountPercent, l.LineTotal.Amount, l.Notes);

    public static SalesOrderDto ToDto(this SalesOrder o) => new(
        o.Id, o.OrderNumber, o.CustomerId, o.Status, o.OrderDate,
        o.RequestedDeliveryDate, o.CurrencyCode, o.CountryCode, o.OriginQuoteId,
        o.Lines.Select(l => l.ToDto()).ToList().AsReadOnly(),
        o.Subtotal.Amount, o.TaxAmount.Amount, o.Total.Amount,
        o.Notes, o.CreatedAt, o.UpdatedAt);

    public static SalesOrderLineDto ToDto(this SalesOrderLine l) => new(
        l.Id, l.CatalogItemId, l.SKU, l.ItemName, l.Quantity,
        l.UnitPrice.Amount, l.UnitPrice.CurrencyCode,
        l.DiscountPercent, l.LineTotal.Amount, l.FulfilledQuantity, l.Notes);
}
