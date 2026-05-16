using Purchasing.Domain.PurchaseOrders;

namespace Purchasing.Application.Common.Dtos;

public record PurchaseOrderLineDto(
    Guid Id,
    Guid CatalogItemId,
    string Sku,
    string ItemName,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal UnitCostAmount,
    string CurrencyCode,
    decimal LineTotal,
    string? Notes);

public record PurchaseOrderDto(
    Guid Id,
    Guid TenantId,
    string PoNumber,
    Guid SupplierId,
    PurchaseOrderStatus Status,
    DateTime? ExpectedDeliveryDate,
    string CurrencyCode,
    string CountryCode,
    string? Notes,
    decimal Subtotal,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt);
