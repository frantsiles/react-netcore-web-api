using Api.Domain.Common;
using Catalog.Domain.Catalog;
using Catalog.Domain.DomainEvents;
using Catalog.Domain.ValueObjects;
using FluentAssertions;

namespace Catalog.UnitTests.Domain;

public class CatalogItemTests
{
    private static CatalogItem AnItem(ItemType type = ItemType.Product) =>
        CatalogItem.Create("SKU-001", "Widget", null, type,
            UnitOfMeasure.Each, "STANDARD", "USD", "US");

    [Fact]
    public void Create_WithValidData_CreatesActiveItem()
    {
        var item = AnItem();

        item.SKU.Should().Be("SKU-001");
        item.Name.Should().Be("Widget");
        item.IsActive.Should().BeTrue();
        item.ItemType.Should().Be(ItemType.Product);
        item.UnitOfMeasure.Code.Should().Be("EA");
    }

    [Fact]
    public void Create_NormalizesSKUToUpperCase()
    {
        var item = CatalogItem.Create("sku-abc", "Item", null,
            ItemType.Product, UnitOfMeasure.Each, "STANDARD", "USD", "US");

        item.SKU.Should().Be("SKU-ABC");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithEmptySKU_ThrowsDomainException(string sku)
    {
        var act = () => CatalogItem.Create(sku, "Item", null,
            ItemType.Product, UnitOfMeasure.Each, "STANDARD", "USD", "US");

        act.Should().Throw<DomainException>().WithMessage("*SKU*");
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsDomainException()
    {
        var act = () => CatalogItem.Create("SKU-1", "", null,
            ItemType.Product, UnitOfMeasure.Each, "STANDARD", "USD", "US");

        act.Should().Throw<DomainException>().WithMessage("*Name*");
    }

    [Fact]
    public void Create_ServiceWithTrackInventory_ThrowsDomainException()
    {
        var act = () => CatalogItem.Create("SVC-1", "Service", null,
            ItemType.Service, UnitOfMeasure.Hour, "STANDARD", "USD", "US",
            trackInventory: true);

        act.Should().Throw<DomainException>().WithMessage("*Services*inventory*");
    }

    [Fact]
    public void Create_RaisesCatalogItemCreatedEvent()
    {
        var item = AnItem();

        item.DomainEvents.Should().HaveCount(1);
        item.DomainEvents[0].Should().BeOfType<CatalogItemCreatedEvent>();
    }

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var item = AnItem();
        item.Update("New Name", "desc", UnitOfMeasure.Kilogram, "EXEMPT");

        item.Name.Should().Be("New Name");
        item.Description.Should().Be("desc");
        item.UnitOfMeasure.Code.Should().Be("KGM");
        item.TaxCategory.Should().Be("EXEMPT");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse_AndRaisesEvent()
    {
        var item = AnItem();
        item.ClearDomainEvents();

        item.Deactivate();

        item.IsActive.Should().BeFalse();
        item.DomainEvents.Should().HaveCount(1);
        item.DomainEvents[0].Should().BeOfType<CatalogItemDeactivatedEvent>();
    }
}
