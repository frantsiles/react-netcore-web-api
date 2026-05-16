using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentAssertions;
using Inventory.Application.Inventory.Commands.ReceiveStock;
using Inventory.Domain.Inventory;
using Inventory.Domain.Repositories;
using Inventory.Domain.Warehouses;
using Moq;

namespace Inventory.UnitTests.Application;

public class ReceiveStockCommandHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _itemRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly ReceiveStockCommandHandler _handler;

    public ReceiveStockCommandHandlerTests()
        => _handler = new ReceiveStockCommandHandler(
            _itemRepo.Object, _warehouseRepo.Object, _events.Object);

    private static ReceiveStockCommand ValidCommand(Guid warehouseId, Guid catalogItemId) => new(
        catalogItemId, warehouseId, "SKU-001", 50, "PO-001", null, null);

    [Fact]
    public async Task Handle_NewItem_CreatesItemAndReceivesStock()
    {
        var warehouseId = Guid.NewGuid();
        var catalogItemId = Guid.NewGuid();
        var warehouse = Warehouse.Create("WH-01", "Main");

        _warehouseRepo.Setup(r => r.GetByIdAsync(warehouseId, default)).ReturnsAsync(warehouse);
        _itemRepo.Setup(r => r.GetByCatalogItemAndWarehouseAsync(catalogItemId, warehouseId, default))
            .ReturnsAsync((InventoryItem?)null);

        var dto = await _handler.Handle(ValidCommand(warehouseId, catalogItemId), default);

        dto.QuantityOnHand.Should().Be(50);
        dto.SKU.Should().Be("SKU-001");
        _itemRepo.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), default), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<object>(), default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ExistingItem_AccumulatesStock()
    {
        var warehouseId = Guid.NewGuid();
        var catalogItemId = Guid.NewGuid();
        var warehouse = Warehouse.Create("WH-01", "Main");
        var existing = InventoryItem.Create(catalogItemId, warehouseId, "SKU-001");
        existing.Receive(20);
        existing.ClearDomainEvents();

        _warehouseRepo.Setup(r => r.GetByIdAsync(warehouseId, default)).ReturnsAsync(warehouse);
        _itemRepo.Setup(r => r.GetByCatalogItemAndWarehouseAsync(catalogItemId, warehouseId, default))
            .ReturnsAsync(existing);

        var dto = await _handler.Handle(ValidCommand(warehouseId, catalogItemId), default);

        dto.QuantityOnHand.Should().Be(70); // 20 + 50
        _itemRepo.Verify(r => r.UpdateAsync(It.IsAny<InventoryItem>(), default), Times.Once);
        _itemRepo.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_WarehouseNotFound_ThrowsDomainException()
    {
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Warehouse?)null);

        var act = async () => await _handler.Handle(
            ValidCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not found*");
    }
}
