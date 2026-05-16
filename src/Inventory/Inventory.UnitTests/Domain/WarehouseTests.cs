using Api.Domain.Common;
using FluentAssertions;
using Inventory.Domain.DomainEvents;
using Inventory.Domain.Warehouses;

namespace Inventory.UnitTests.Domain;

public class WarehouseTests
{
    [Fact]
    public void Create_WithValidData_CreatesActiveWarehouse()
    {
        var wh = Warehouse.Create("WH-01", "Main Warehouse", "123 Main St");

        wh.Code.Should().Be("WH-01");
        wh.Name.Should().Be("Main Warehouse");
        wh.Address.Should().Be("123 Main St");
        wh.Status.Should().Be(WarehouseStatus.Active);
    }

    [Fact]
    public void Create_NormalizesCodeToUpperCase()
    {
        var wh = Warehouse.Create("wh-central", "Central");
        wh.Code.Should().Be("WH-CENTRAL");
    }

    [Fact]
    public void Create_RaisesWarehouseCreatedEvent()
    {
        var wh = Warehouse.Create("WH-01", "Main");
        wh.DomainEvents[0].Should().BeOfType<WarehouseCreatedEvent>();
    }

    [Fact]
    public void Create_WithEmptyCode_ThrowsDomainException()
    {
        var act = () => Warehouse.Create("", "Main");
        act.Should().Throw<DomainException>().WithMessage("*code*");
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsDomainException()
    {
        var act = () => Warehouse.Create("WH-01", "");
        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void Deactivate_ActiveWarehouse_SetsStatusInactive()
    {
        var wh = Warehouse.Create("WH-01", "Main");
        wh.Deactivate();
        wh.Status.Should().Be(WarehouseStatus.Inactive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsDomainException()
    {
        var wh = Warehouse.Create("WH-01", "Main");
        wh.Deactivate();

        var act = () => wh.Deactivate();
        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }
}
