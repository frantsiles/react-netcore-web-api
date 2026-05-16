using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentAssertions;
using Moq;
using Sales.Application.Quotes.Commands.ConvertQuoteToOrder;
using Sales.Domain.Orders;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.UnitTests.Application;

public class ConvertQuoteToOrderCommandHandlerTests
{
    private readonly Mock<IQuoteRepository> _quoteRepo = new();
    private readonly Mock<ISalesOrderRepository> _orderRepo = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly ConvertQuoteToOrderCommandHandler _handler;

    public ConvertQuoteToOrderCommandHandlerTests()
        => _handler = new ConvertQuoteToOrderCommandHandler(
            _quoteRepo.Object, _orderRepo.Object, _events.Object);

    private static Quote AnAcceptedQuoteWithLines()
    {
        var q = Quote.Create("Q-202506-XYZ001", Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), "USD", "US");
        q.AddLine(QuoteLine.Create(Guid.NewGuid(), "SKU-001", "Widget", 2, Money.Of(10m, "USD")));
        q.AddLine(QuoteLine.Create(Guid.NewGuid(), "SKU-002", "Gadget", 1, Money.Of(25m, "USD")));
        q.Send();
        q.Accept();
        return q;
    }

    [Fact]
    public async Task Handle_AcceptedQuote_CreatesOrderWithSameLines()
    {
        var quote = AnAcceptedQuoteWithLines();
        _quoteRepo.Setup(r => r.GetByIdAsync(quote.Id, default)).ReturnsAsync(quote);

        var dto = await _handler.Handle(new ConvertQuoteToOrderCommand(quote.Id, null, null), default);

        dto.OriginQuoteId.Should().Be(quote.Id);
        dto.Lines.Should().HaveCount(2);
        dto.Subtotal.Should().Be(45m); // 2*10 + 1*25
        dto.CurrencyCode.Should().Be("USD");
        dto.Status.Should().Be(SalesOrderStatus.Draft);

        _orderRepo.Verify(r => r.AddAsync(It.IsAny<SalesOrder>(), default), Times.Once);
        _quoteRepo.Verify(r => r.UpdateAsync(It.IsAny<Quote>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_AcceptedQuote_MarksQuoteAsConvertedToOrder()
    {
        var quote = AnAcceptedQuoteWithLines();
        _quoteRepo.Setup(r => r.GetByIdAsync(quote.Id, default)).ReturnsAsync(quote);

        await _handler.Handle(new ConvertQuoteToOrderCommand(quote.Id, null, null), default);

        quote.Status.Should().Be(QuoteStatus.ConvertedToOrder);
    }

    [Fact]
    public async Task Handle_NotFoundQuote_ThrowsDomainException()
    {
        _quoteRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Quote?)null);

        var act = async () => await _handler.Handle(
            new ConvertQuoteToOrderCommand(Guid.NewGuid(), null, null), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_NonAcceptedQuote_ThrowsDomainException()
    {
        var quote = Quote.Create("Q-202506-DRAFT01", Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "USD", "US");
        quote.AddLine(QuoteLine.Create(Guid.NewGuid(), "SKU-001", "Item", 1, Money.Of(10m, "USD")));
        quote.Send();
        _quoteRepo.Setup(r => r.GetByIdAsync(quote.Id, default)).ReturnsAsync(quote);

        var act = async () => await _handler.Handle(
            new ConvertQuoteToOrderCommand(quote.Id, null, null), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*accepted*");
    }
}
