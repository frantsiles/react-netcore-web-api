using Purchasing.Application.Common.Dtos;
using Purchasing.Domain.PurchaseOrders;

namespace Purchasing.Application.Common.Mappings;

public static class PurchasingMappingExtensions
{
    public static PurchaseOrderLineDto ToDto(this PurchaseOrderLine line) =>
        new(line.Id, line.CatalogItemId, line.Sku, line.ItemName,
            line.QuantityOrdered, line.QuantityReceived,
            line.UnitCost.Amount, line.UnitCost.CurrencyCode,
            line.LineTotal.Amount, line.Notes);

    public static PurchaseOrderDto ToDto(this PurchaseOrder po) =>
        new(po.Id, po.TenantId, po.PoNumber, po.SupplierId, po.Status,
            po.ExpectedDeliveryDate, po.CurrencyCode, po.CountryCode, po.Notes,
            po.Subtotal.Amount, po.Total.Amount,
            po.Lines.Select(l => l.ToDto()).ToList(),
            po.CreatedAt, po.UpdatedAt);
}
