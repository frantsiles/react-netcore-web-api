using Api.Domain.Common;
using Purchasing.Domain.DomainEvents;

namespace Purchasing.Domain.PurchaseOrders;

public class PurchaseOrder : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string PoNumber { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public string CountryCode { get; private set; } = "";
    public string? Notes { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = [];
    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public Money Subtotal { get; private set; } = Money.Zero("USD");
    public Money Total { get; private set; } = Money.Zero("USD");

    private PurchaseOrder() : base() { }

    public static PurchaseOrder Create(
        Guid tenantId,
        string poNumber,
        Guid supplierId,
        string currencyCode,
        string countryCode,
        DateTime? expectedDeliveryDate = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
            throw new DomainException("PO number cannot be empty.");
        if (supplierId == Guid.Empty)
            throw new DomainException("SupplierId is required.");

        var po = new PurchaseOrder
        {
            TenantId = tenantId,
            PoNumber = poNumber.Trim(),
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Draft,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            CountryCode = countryCode.ToUpperInvariant(),
            ExpectedDeliveryDate = expectedDeliveryDate,
            Notes = notes?.Trim()
        };
        po.Subtotal = Money.Zero(currencyCode);
        po.Total = Money.Zero(currencyCode);
        po.RaiseDomainEvent(new PurchaseOrderCreatedEvent(po.Id, tenantId, po.PoNumber, DateTimeOffset.UtcNow));
        return po;
    }

    private void GuardEditable()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException($"Purchase order cannot be edited in status '{Status}'.");
    }

    public void AddLine(Guid catalogItemId, string sku, string itemName,
        decimal quantityOrdered, Money unitCost, string? notes = null)
    {
        GuardEditable();
        if (unitCost.CurrencyCode != CurrencyCode)
            throw new DomainException($"Unit cost currency '{unitCost.CurrencyCode}' must match PO currency '{CurrencyCode}'.");
        if (_lines.Any(l => l.CatalogItemId == catalogItemId))
            throw new DomainException($"Item '{sku}' is already in the purchase order.");

        _lines.Add(PurchaseOrderLine.Create(catalogItemId, sku, itemName, quantityOrdered, unitCost, notes));
        RecalculateTotals();
        SetUpdatedAt();
    }

    public void UpdateLine(Guid lineId, decimal quantityOrdered, Money unitCost, string? notes)
    {
        GuardEditable();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Line '{lineId}' not found.");
        if (unitCost.CurrencyCode != CurrencyCode)
            throw new DomainException($"Unit cost currency must match PO currency '{CurrencyCode}'.");

        line.Update(quantityOrdered, unitCost, notes);
        RecalculateTotals();
        SetUpdatedAt();
    }

    public void RemoveLine(Guid lineId)
    {
        GuardEditable();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Line '{lineId}' not found.");

        _lines.Remove(line);
        RecalculateTotals();
        SetUpdatedAt();
    }

    public void Send()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("Only Draft purchase orders can be sent.");
        if (_lines.Count == 0)
            throw new DomainException("Cannot send a purchase order with no lines.");

        Status = PurchaseOrderStatus.Sent;
        SetUpdatedAt();
        RaiseDomainEvent(new PurchaseOrderSentEvent(Id, TenantId, PoNumber, DateTimeOffset.UtcNow));
    }

    public void Confirm()
    {
        if (Status != PurchaseOrderStatus.Sent)
            throw new DomainException("Only Sent purchase orders can be confirmed.");

        Status = PurchaseOrderStatus.Confirmed;
        SetUpdatedAt();
        RaiseDomainEvent(new PurchaseOrderConfirmedEvent(Id, TenantId, PoNumber, DateTimeOffset.UtcNow));
    }

    public void ReceiveLine(Guid lineId, decimal quantityReceived)
    {
        if (Status != PurchaseOrderStatus.Confirmed && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new DomainException("Purchase order must be Confirmed or PartiallyReceived to receive goods.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Line '{lineId}' not found.");

        line.AddReceived(quantityReceived);
        SetUpdatedAt();

        var allReceived = _lines.All(l => l.QuantityReceived >= l.QuantityOrdered);
        Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;

        RaiseDomainEvent(new PurchaseOrderLineReceivedEvent(
            Id, TenantId, PoNumber, lineId, line.CatalogItemId, line.Sku, quantityReceived, DateTimeOffset.UtcNow));
    }

    public void Cancel(string reason)
    {
        if (Status == PurchaseOrderStatus.Received)
            throw new DomainException("Cannot cancel a fully received purchase order.");
        if (Status == PurchaseOrderStatus.Cancelled)
            throw new DomainException("Purchase order is already cancelled.");

        Status = PurchaseOrderStatus.Cancelled;
        SetUpdatedAt();
        RaiseDomainEvent(new PurchaseOrderCancelledEvent(Id, TenantId, PoNumber, reason, DateTimeOffset.UtcNow));
    }

    private void RecalculateTotals()
    {
        var subtotal = _lines.Sum(l => l.LineTotal.Amount);
        Subtotal = Money.Of(Math.Round(subtotal, 2), CurrencyCode);
        Total = Subtotal;
    }
}
