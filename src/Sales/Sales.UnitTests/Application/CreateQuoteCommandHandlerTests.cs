using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentAssertions;
using Moq;
using Sales.Application.Quotes.Commands.CreateQuote;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.UnitTests.Application;

public class CreateQuoteCommandHandlerTests
{
    private readonly Mock<IQuoteRepository> _repo = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly CreateQuoteCommandHandler _handler;

    public CreateQuoteCommandHandlerTests()
        => _handler = new CreateQuoteCommandHandler(_repo.Object, _events.Object);

    private static CreateQuoteCommand ValidCommand() => new(
        Guid.NewGuid(),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
        "USD", "US", null);

    [Fact]
    public async Task Handle_WithValidData_CreatesQuoteAndPublishesEvent()
    {
        var dto = await _handler.Handle(ValidCommand(), default);

        dto.Status.Should().Be(QuoteStatus.Draft);
        dto.Lines.Should().BeEmpty();
        dto.Subtotal.Should().Be(0);
        _repo.Verify(r => r.AddAsync(It.IsAny<Quote>(), default), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<object>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_GeneratesUniqueQuoteNumbers()
    {
        var dto1 = await _handler.Handle(ValidCommand(), default);
        var dto2 = await _handler.Handle(ValidCommand(), default);

        dto1.QuoteNumber.Should().NotBe(dto2.QuoteNumber);
    }
}
