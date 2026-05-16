using Api.Domain.Common;
using Sales.Domain.DomainEvents;

namespace Sales.Domain.Quotes;

public class Quote : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string QuoteNumber { get; private set; } = "";
    public Guid CustomerId { get; private set; }
    public QuoteStatus Status { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public string CountryCode { get; private set; } = "";
    public string? Notes { get; private set; }

    private readonly List<QuoteLine> _lines = [];
    public IReadOnlyList<QuoteLine> Lines => _lines.AsReadOnly();

    public Money Subtotal { get; private set; } = null!;
    public Money TaxAmount { get; private set; } = null!;
    public Money Total { get; private set; } = null!;

    private Quote() : base() { }

    public static Quote Create(string quoteNumber, Guid customerId, DateOnly validUntil,
        string currencyCode, string countryCode, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(quoteNumber))
            throw new DomainException("Quote number cannot be empty.");
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID cannot be empty.");
        if (validUntil < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("ValidUntil must be today or a future date.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new DomainException("CurrencyCode must be a 3-letter ISO code.");
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new DomainException("CountryCode must be a 2-letter ISO code.");

        var quote = new Quote
        {
            QuoteNumber = quoteNumber.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            Status = QuoteStatus.Draft,
            ValidUntil = validUntil,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            CountryCode = countryCode.ToUpperInvariant(),
            Notes = notes?.Trim()
        };
        quote.RecalculateTotals();
        quote.RaiseDomainEvent(new QuoteCreatedEvent(quote.Id, quote.QuoteNumber,
            quote.CustomerId, DateTimeOffset.UtcNow));
        return quote;
    }

    public void AddLine(QuoteLine line)
    {
        GuardEditable();
        if (line.UnitPrice.CurrencyCode != CurrencyCode)
            throw new DomainException($"Line currency '{line.UnitPrice.CurrencyCode}' does not match quote currency '{CurrencyCode}'.");
        if (_lines.Any(l => l.CatalogItemId == line.CatalogItemId))
            throw new DomainException($"Item '{line.SKU}' is already on this quote. Update the existing line instead.");
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
            throw new DomainException($"Line currency must match quote currency '{CurrencyCode}'.");
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

    public void Send()
    {
        if (Status != QuoteStatus.Draft)
            throw new DomainException($"Cannot send a quote in '{Status}' status. Must be Draft.");
        if (_lines.Count == 0)
            throw new DomainException("Cannot send a quote with no lines.");
        Status = QuoteStatus.Sent;
        SetUpdatedAt();
        RaiseDomainEvent(new QuoteSentEvent(Id, QuoteNumber, CustomerId, DateTimeOffset.UtcNow));
    }

    public void Accept()
    {
        if (Status != QuoteStatus.Sent)
            throw new DomainException($"Cannot accept a quote in '{Status}' status. Must be Sent.");
        Status = QuoteStatus.Accepted;
        SetUpdatedAt();
        RaiseDomainEvent(new QuoteAcceptedEvent(Id, QuoteNumber, CustomerId, DateTimeOffset.UtcNow));
    }

    public void Reject()
    {
        if (Status != QuoteStatus.Sent)
            throw new DomainException($"Cannot reject a quote in '{Status}' status. Must be Sent.");
        Status = QuoteStatus.Rejected;
        SetUpdatedAt();
    }

    public void Expire()
    {
        if (Status != QuoteStatus.Sent && Status != QuoteStatus.Draft)
            throw new DomainException($"Cannot expire a quote in '{Status}' status.");
        Status = QuoteStatus.Expired;
        SetUpdatedAt();
    }

    public void MarkConvertedToOrder()
    {
        if (Status != QuoteStatus.Accepted)
            throw new DomainException("Only an accepted quote can be converted to an order.");
        Status = QuoteStatus.ConvertedToOrder;
        SetUpdatedAt();
        RaiseDomainEvent(new QuoteConvertedToOrderEvent(Id, QuoteNumber, CustomerId, DateTimeOffset.UtcNow));
    }

    private void GuardEditable()
    {
        if (Status != QuoteStatus.Draft)
            throw new DomainException($"Quote lines cannot be modified in '{Status}' status.");
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
