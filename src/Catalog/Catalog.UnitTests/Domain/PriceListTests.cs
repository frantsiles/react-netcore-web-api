using Api.Domain.Common;
using Catalog.Domain.Catalog;
using FluentAssertions;

namespace Catalog.UnitTests.Domain;

public class PriceListTests
{
    private static PriceList AList(bool isDefault = false) =>
        PriceList.Create("Standard", "USD",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), null, isDefault);

    private static PriceListEntry AnEntry(Guid? itemId = null) =>
        PriceListEntry.Create(itemId ?? Guid.NewGuid(), Money.Of(10m, "USD"), 1m);

    [Fact]
    public void Create_WithValidData_CreatesActiveList()
    {
        var pl = AList();

        pl.Name.Should().Be("Standard");
        pl.CurrencyCode.Should().Be("USD");
        pl.IsDefault.Should().BeFalse();
        pl.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValidToBeforeValidFrom_ThrowsDomainException()
    {
        var act = () => PriceList.Create("Bad", "USD",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), false);

        act.Should().Throw<DomainException>().WithMessage("*ValidTo*after*ValidFrom*");
    }

    [Fact]
    public void AddEntry_WithValidEntry_AddsIt()
    {
        var pl = AList();
        var entry = AnEntry();

        pl.AddEntry(entry);

        pl.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void AddEntry_WithDifferentCurrency_ThrowsDomainException()
    {
        var pl = AList();
        var entry = PriceListEntry.Create(Guid.NewGuid(), Money.Of(10m, "EUR"), 1m);

        var act = () => pl.AddEntry(entry);

        act.Should().Throw<DomainException>().WithMessage("*currency*");
    }

    [Fact]
    public void AddEntry_DuplicateItemAndMinQuantity_ThrowsDomainException()
    {
        var pl = AList();
        var itemId = Guid.NewGuid();
        pl.AddEntry(PriceListEntry.Create(itemId, Money.Of(10m, "USD"), 1m));

        var act = () => pl.AddEntry(PriceListEntry.Create(itemId, Money.Of(12m, "USD"), 1m));

        act.Should().Throw<DomainException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveEntry_ExistingEntry_RemovesIt()
    {
        var pl = AList();
        var entry = AnEntry();
        pl.AddEntry(entry);

        pl.RemoveEntry(entry.Id);

        pl.Entries.Should().BeEmpty();
    }

    [Fact]
    public void RemoveEntry_NonExistent_ThrowsDomainException()
    {
        var pl = AList();

        var act = () => pl.RemoveEntry(Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*not found*");
    }

    [Fact]
    public void IsActiveOn_WithinValidRange_ReturnsTrue()
    {
        var pl = PriceList.Create("P", "USD",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), false);

        pl.IsActiveOn(DateOnly.FromDateTime(DateTime.UtcNow)).Should().BeTrue();
    }

    [Fact]
    public void IsActiveOn_PastExpiry_ReturnsFalse()
    {
        var pl = PriceList.Create("P", "USD",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), false);

        pl.IsActiveOn(DateOnly.FromDateTime(DateTime.UtcNow)).Should().BeFalse();
    }

    [Fact]
    public void SetAsDefault_SetsFlag()
    {
        var pl = AList(isDefault: false);
        pl.SetAsDefault();
        pl.IsDefault.Should().BeTrue();
    }
}
