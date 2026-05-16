using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Catalog.Application.Commands.CreateCatalogItem;
using Catalog.Domain.Catalog;
using Catalog.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Catalog.UnitTests.Application;

public class CreateCatalogItemCommandHandlerTests
{
    private readonly Mock<ICatalogItemRepository> _repo = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly CreateCatalogItemCommandHandler _handler;

    public CreateCatalogItemCommandHandlerTests()
        => _handler = new CreateCatalogItemCommandHandler(_repo.Object, _events.Object);

    private static CreateCatalogItemCommand ValidCommand() => new(
        "SKU-001", "Widget", null, ItemType.Product, "EA", "STANDARD", "USD", "US");

    [Fact]
    public async Task Handle_WithValidData_CreatesItemAndPublishesEvent()
    {
        _repo.Setup(r => r.ExistsBySKUAsync("SKU-001", default)).ReturnsAsync(false);

        var dto = await _handler.Handle(ValidCommand(), default);

        dto.SKU.Should().Be("SKU-001");
        dto.IsActive.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<CatalogItem>(), default), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<object>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateSKU_ThrowsDomainException()
    {
        _repo.Setup(r => r.ExistsBySKUAsync("SKU-001", default)).ReturnsAsync(true);

        var act = async () => await _handler.Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*SKU*already exists*");
        _repo.Verify(r => r.AddAsync(It.IsAny<CatalogItem>(), default), Times.Never);
    }
}
