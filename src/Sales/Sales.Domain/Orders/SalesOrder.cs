using Api.Domain.Common;
using Sales.Domain.DomainEvents;

namespace Sales.Domain.Orders;

public class SalesOrder : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; private set; } = "";
    public Guid CustomerId { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public DateOnly? RequestedDeliveryDate { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public string CountryCode { get; private set; } = "";
    public Guid? OriginQuoteId { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<SalesOrderLine> _lines = [];
    public IReadOnlyList<SalesOrderLine> Lines => _lines.AsReadOnly();

    public Money Subtotal { get; private set; } = null!;
    public Money TaxAmount { get; private set; } = null!;
    public Money Total { get; private set; } = null!;

    private SalesOrder() : base() { }

    public static SalesOrder Create(string orderNumber, Guid customerId, DateOnly orderDate,
        string currencyCode, string countryCode,
        DateOnly? requestedDeliveryDate = null, Guid? originQuoteId = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Order number cannot be empty.");
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new DomainException("CurrencyCode must be a 3-letter ISO code.");
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new DomainException("CountryCode must be a 2-letter ISO code.");
        if (requestedDeliveryDate.HasValue && requestedDeliveryDate.Value < orderDate)
            throw new DomainException("RequestedDeliveryDate cannot be before OrderDate.");

        var order = new SalesOrder
        {
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            Status = SalesOrderStatus.Draft,
            OrderDate = orderDate,
            RequestedDeliveryDate = requestedDeliveryDate,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            CountryCode = countryCode.ToUpperInvariant(),
            OriginQuoteId = originQuoteId,
            Notes = notes?.Trim()
        };
        order.RecalculateTotals();
        order.RaiseDomainEvent(new SalesOrderCreatedEvent(order.Id, order.OrderNumber,
            order.CustomerId, order.OriginQuoteId, DateTimeOffset.UtcNow));
        return order;
    }

    public void AddLine(SalesOrderLine line)
    {
        GuardEditable();
        if (line.UnitPrice.CurrencyCode != CurrencyCode)
            throw new DomainException($"Line currency '{line.UnitPrice.CurrencyCode}' does not match order currency '{CurrencyCode}'.");
        if (_lines.Any(l => l.CatalogItemId == line.CatalogItemId))
            throw new DomainException($"Item '{line.SKU}' is already on this order. Update the existing line instead.");
        _lines.Add(line);
        RecalculateTotals();
        SetUpdatedAt();
    }

    public void UpdateLine(Guid lineId, decimal quantity, Money unitPrice,
        decimal discountPercent, string? notes)
    {
        GuardEditable();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Line '{lineId}' not found.");
        if (unitPrice.CurrencyCode != CurrencyCode)
            throw new DomainException($"Line currency must match order currency '{CurrencyCode}'.");
        line.Update(quantity, unitPrice, discountPercent, notes);
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

    public void Confirm()
    {
        if (Status != SalesOrderStatus.Draft)
            throw new DomainException($"Cannot confirm an order in '{Status}' status. Must be Draft.");
        if (_lines.Count == 0)
            throw new DomainException("Cannot confirm an order with no lines.");
        Status = SalesOrderStatus.Confirmed;
        SetUpdatedAt();
        RaiseDomainEvent(new SalesOrderConfirmedEvent(Id, OrderNumber, CustomerId, DateTimeOffset.UtcNow));
    }

    public void Cancel(string reason)
    {
        if (Status == SalesOrderStatus.Fulfilled || Status == SalesOrderStatus.Invoiced)
            throw new DomainException($"Cannot cancel an order in '{Status}' status.");
        Status = SalesOrderStatus.Cancelled;
        SetUpdatedAt();
        RaiseDomainEvent(new SalesOrderCancelledEvent(Id, OrderNumber, CustomerId, reason, DateTimeOffset.UtcNow));
    }

    private void GuardEditable()
    {
        if (Status != SalesOrderStatus.Draft)
            throw new DomainException($"Order lines cannot be modified in '{Status}' status.");
    }

    private void RecalculateTotals()
    {
        Subtotal = _lines.Count == 0
            ? Money.Zero(CurrencyCode)
            : _lines.Aggregate(Money.Zero(CurrencyCode), (sum, l) => sum.Add(l.LineTotal));
        TaxAmount = Money.Zero(CurrencyCode);
        Total = Subtotal.Add(TaxAmount);
    }
}
