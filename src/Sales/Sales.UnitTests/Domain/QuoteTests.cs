using Api.Domain.Common;
using FluentAssertions;
using Sales.Domain.DomainEvents;
using Sales.Domain.Quotes;

namespace Sales.UnitTests.Domain;

public class QuoteTests
{
    private static readonly DateOnly Tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static Quote AQuote() =>
        Quote.Create("Q-202506-AABBCC", Guid.NewGuid(), Tomorrow, "USD", "US");

    private static QuoteLine ALine(string sku = "SKU-001") =>
        QuoteLine.Create(Guid.NewGuid(), sku, "Widget", 2, Money.Of(10m, "USD"));

    [Fact]
    public void Create_WithValidData_CreatesDraftQuote()
    {
        var q = AQuote();

        q.Status.Should().Be(QuoteStatus.Draft);
        q.Lines.Should().BeEmpty();
        q.Subtotal.Amount.Should().Be(0);
        q.Total.Amount.Should().Be(0);
    }

    [Fact]
    public void Create_RaisesQuoteCreatedEvent()
    {
        var q = AQuote();
        q.DomainEvents.Should().HaveCount(1);
        q.DomainEvents[0].Should().BeOfType<QuoteCreatedEvent>();
    }

    [Fact]
    public void Create_WithEmptyQuoteNumber_ThrowsDomainException()
    {
        var act = () => Quote.Create("", Guid.NewGuid(), Tomorrow, "USD", "US");
        act.Should().Throw<DomainException>().WithMessage("*Quote number*");
    }

    [Fact]
    public void Create_WithPastValidUntil_ThrowsDomainException()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var act = () => Quote.Create("Q-001", Guid.NewGuid(), yesterday, "USD", "US");
        act.Should().Throw<DomainException>().WithMessage("*ValidUntil*");
    }

    [Fact]
    public void AddLine_UpdatesSubtotalAndTotal()
    {
        var q = AQuote();
        q.AddLine(ALine());

        q.Lines.Should().HaveCount(1);
        q.Subtotal.Amount.Should().Be(20m); // 2 * 10
        q.Total.Amount.Should().Be(20m);
    }

    [Fact]
    public void AddLine_DuplicateCatalogItem_ThrowsDomainException()
    {
        var q = AQuote();
        var catalogItemId = Guid.NewGuid();
        q.AddLine(QuoteLine.Create(catalogItemId, "SKU-001", "Widget", 1, Money.Of(10m, "USD")));

        var act = () => q.AddLine(QuoteLine.Create(catalogItemId, "SKU-001", "Widget", 2, Money.Of(10m, "USD")));
        act.Should().Throw<DomainException>().WithMessage("*already on this quote*");
    }

    [Fact]
    public void AddLine_WrongCurrency_ThrowsDomainException()
    {
        var q = AQuote();
        var eurLine = QuoteLine.Create(Guid.NewGuid(), "SKU-EUR", "Euro Item", 1, Money.Of(5m, "EUR"));
        var act = () => q.AddLine(eurLine);
        act.Should().Throw<DomainException>().WithMessage("*currency*");
    }

    [Fact]
    public void RemoveLine_ExistingLine_RecalculatesTotals()
    {
        var q = AQuote();
        var line = ALine();
        q.AddLine(line);

        q.RemoveLine(line.Id);

        q.Lines.Should().BeEmpty();
        q.Subtotal.Amount.Should().Be(0);
    }

    [Fact]
    public void AddLine_OnSentQuote_ThrowsDomainException()
    {
        var q = AQuote();
        q.AddLine(ALine());
        q.Send();

        var act = () => q.AddLine(QuoteLine.Create(Guid.NewGuid(), "SKU-002", "Other", 1, Money.Of(5m, "USD")));
        act.Should().Throw<DomainException>().WithMessage("*Sent*");
    }

    [Fact]
    public void Send_WithLines_TransitionsToSent_AndRaisesEvent()
    {
        var q = AQuote();
        q.AddLine(ALine());
        q.ClearDomainEvents();

        q.Send();

        q.Status.Should().Be(QuoteStatus.Sent);
        q.DomainEvents.Should().HaveCount(1);
        q.DomainEvents[0].Should().BeOfType<QuoteSentEvent>();
    }

    [Fact]
    public void Send_WithNoLines_ThrowsDomainException()
    {
        var q = AQuote();
        var act = () => q.Send();
        act.Should().Throw<DomainException>().WithMessage("*no lines*");
    }

    [Fact]
    public void Accept_SentQuote_TransitionsToAccepted()
    {
        var q = AQuote();
        q.AddLine(ALine());
        q.Send();
        q.ClearDomainEvents();

        q.Accept();

        q.Status.Should().Be(QuoteStatus.Accepted);
        q.DomainEvents[0].Should().BeOfType<QuoteAcceptedEvent>();
    }

    [Fact]
    public void Accept_DraftQuote_ThrowsDomainException()
    {
        var q = AQuote();
        var act = () => q.Accept();
        act.Should().Throw<DomainException>().WithMessage("*Draft*");
    }

    [Fact]
    public void Reject_SentQuote_TransitionsToRejected()
    {
        var q = AQuote();
        q.AddLine(ALine());
        q.Send();

        q.Reject();

        q.Status.Should().Be(QuoteStatus.Rejected);
    }

    [Fact]
    public void MarkConvertedToOrder_AcceptedQuote_TransitionsAndRaisesEvent()
    {
        var q = AQuote();
        q.AddLine(ALine());
        q.Send();
        q.Accept();
        q.ClearDomainEvents();

        q.MarkConvertedToOrder();

        q.Status.Should().Be(QuoteStatus.ConvertedToOrder);
        q.DomainEvents[0].Should().BeOfType<QuoteConvertedToOrderEvent>();
    }

    [Fact]
    public void MarkConvertedToOrder_NonAcceptedQuote_ThrowsDomainException()
    {
        var q = AQuote();
        q.AddLine(ALine());
        q.Send();

        var act = () => q.MarkConvertedToOrder();
        act.Should().Throw<DomainException>().WithMessage("*accepted*");
    }

    [Fact]
    public void UpdateLine_WithDiscount_RecalculatesLineTotal()
    {
        var q = AQuote();
        var line = ALine();
        q.AddLine(line);

        q.UpdateLine(line.Id, 4, Money.Of(10m, "USD"), 25, null);

        q.Lines[0].LineTotal.Amount.Should().Be(30m); // 4 * 10 * 0.75
        q.Subtotal.Amount.Should().Be(30m);
    }
}
