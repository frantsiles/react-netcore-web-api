using Api.Domain.Common;
using FluentAssertions;
using Inventory.Domain.DomainEvents;
using Inventory.Domain.Inventory;

namespace Inventory.UnitTests.Domain;

public class InventoryItemTests
{
    private static readonly Guid CatalogId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static InventoryItem AnItem(decimal? reorderPoint = null) =>
        InventoryItem.Create(CatalogId, WarehouseId, "SKU-001", reorderPoint);

    [Fact]
    public void Create_WithValidData_CreatesItemWithZeroStock()
    {
        var item = AnItem();

        item.QuantityOnHand.Should().Be(0);
        item.QuantityReserved.Should().Be(0);
        item.QuantityAvailable.Should().Be(0);
        item.SKU.Should().Be("SKU-001");
    }

    [Fact]
    public void Create_NormalizesSKUToUpperCase()
    {
        var item = InventoryItem.Create(CatalogId, WarehouseId, "sku-abc");
        item.SKU.Should().Be("SKU-ABC");
    }

    [Fact]
    public void Create_WithNegativeReorderPoint_ThrowsDomainException()
    {
        var act = () => InventoryItem.Create(CatalogId, WarehouseId, "SKU-001", -1);
        act.Should().Throw<DomainException>().WithMessage("*ReorderPoint*negative*");
    }

    [Fact]
    public void Receive_PositiveQuantity_UpdatesOnHand_AndRaisesEvent()
    {
        var item = AnItem();

        item.Receive(50, "PO-001");

        item.QuantityOnHand.Should().Be(50);
        item.QuantityAvailable.Should().Be(50);
        item.Movements.Should().HaveCount(1);
        item.Movements[0].Type.Should().Be(MovementType.Receipt);
        item.DomainEvents.Should().Contain(e => e is StockReceivedEvent);
    }

    [Fact]
    public void Receive_ZeroQuantity_ThrowsDomainException()
    {
        var item = AnItem();
        var act = () => item.Receive(0);
        act.Should().Throw<DomainException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Receive_AccumulatesMultipleReceipts()
    {
        var item = AnItem();
        item.Receive(30);
        item.Receive(20);

        item.QuantityOnHand.Should().Be(50);
        item.Movements.Should().HaveCount(2);
    }

    [Fact]
    public void Adjust_PositiveDelta_IncreasesStock()
    {
        var item = AnItem();
        item.Receive(100);

        item.Adjust(10, "Cycle count correction");

        item.QuantityOnHand.Should().Be(110);
        item.Movements.Last().Type.Should().Be(MovementType.Adjustment);
    }

    [Fact]
    public void Adjust_NegativeDelta_DecreasesStock()
    {
        var item = AnItem();
        item.Receive(100);

        item.Adjust(-15, "Shrinkage");

        item.QuantityOnHand.Should().Be(85);
    }

    [Fact]
    public void Adjust_WouldGoNegative_ThrowsDomainException()
    {
        var item = AnItem();
        item.Receive(10);

        var act = () => item.Adjust(-20, "Too much");
        act.Should().Throw<DomainException>().WithMessage("*negative stock*");
    }

    [Fact]
    public void Adjust_ZeroDelta_ThrowsDomainException()
    {
        var item = AnItem();
        var act = () => item.Adjust(0, "Nothing");
        act.Should().Throw<DomainException>().WithMessage("*cannot be zero*");
    }

    [Fact]
    public void Reserve_AvailableStock_UpdatesReservedAndAvailable()
    {
        var item = AnItem();
        item.Receive(100);
        var orderId = Guid.NewGuid();

        item.Reserve(30, orderId);

        item.QuantityOnHand.Should().Be(100);
        item.QuantityReserved.Should().Be(30);
        item.QuantityAvailable.Should().Be(70);
        item.DomainEvents.Should().Contain(e => e is StockReservedEvent);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_ThrowsDomainException()
    {
        var item = AnItem();
        item.Receive(10);

        var act = () => item.Reserve(15, Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*Insufficient stock*");
    }

    [Fact]
    public void ReleaseReservation_ReleasesCorrectly()
    {
        var item = AnItem();
        item.Receive(100);
        var orderId = Guid.NewGuid();
        item.Reserve(40, orderId);

        item.ReleaseReservation(40, orderId);

        item.QuantityReserved.Should().Be(0);
        item.QuantityAvailable.Should().Be(100);
    }

    [Fact]
    public void WriteOff_ValidQuantity_DecreasesOnHand_AndRaisesEvent()
    {
        var item = AnItem();
        item.Receive(50);
        item.ClearDomainEvents();

        item.WriteOff(5, "Damaged");

        item.QuantityOnHand.Should().Be(45);
        item.Movements.Last().Type.Should().Be(MovementType.WriteOff);
        item.DomainEvents.Should().Contain(e => e is StockWrittenOffEvent);
    }

    [Fact]
    public void WriteOff_MoreThanOnHand_ThrowsDomainException()
    {
        var item = AnItem();
        item.Receive(10);

        var act = () => item.WriteOff(20, "Too much");
        act.Should().Throw<DomainException>().WithMessage("*on hand*");
    }

    [Fact]
    public void Receive_BelowReorderPoint_RaisesLowStockAlert()
    {
        var item = AnItem(reorderPoint: 20);
        item.Receive(15);

        item.DomainEvents.Should().Contain(e => e is LowStockAlertEvent);
    }

    [Fact]
    public void WriteOff_DropsBelowReorderPoint_RaisesLowStockAlert()
    {
        var item = AnItem(reorderPoint: 10);
        item.Receive(20);
        item.ClearDomainEvents();

        item.WriteOff(12, "Expired");

        item.DomainEvents.Should().Contain(e => e is LowStockAlertEvent);
    }
}
