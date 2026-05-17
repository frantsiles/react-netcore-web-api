using Api.Domain.Common;
using FluentAssertions;
using Tax.Domain.DomainEvents;
using Tax.Domain.TaxRates;

namespace Tax.UnitTests.Domain;

public class TaxRateTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static TaxRate NewRate(decimal rate = 10m) =>
        TaxRate.Create(TenantId, "VAT-10", "VAT 10%", rate, TaxApplicability.Both);

    [Fact]
    public void Create_ValidArgs_SetsActiveWithCorrectRate()
    {
        var rate = NewRate();

        rate.Code.Should().Be("VAT-10");
        rate.Rate.Should().Be(10m);
        rate.Status.Should().Be(TaxRateStatus.Active);
        rate.Applicability.Should().Be(TaxApplicability.Both);
    }

    [Fact]
    public void Create_RaisesTaxRateCreatedEvent()
    {
        var rate = NewRate();

        rate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TaxRateCreatedEvent>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_InvalidRate_ThrowsDomainException(decimal rate)
    {
        var act = () => TaxRate.Create(TenantId, "X", "X", rate, TaxApplicability.Sales);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Calculate_ReturnsCorrectTaxAmount()
    {
        var rate = NewRate(21m);

        var tax = rate.Calculate(100m);

        tax.Should().Be(21m);
    }

    [Fact]
    public void Calculate_RoundsHalfUp()
    {
        var rate = NewRate(10m);

        var tax = rate.Calculate(33.33m);

        tax.Should().Be(3.33m);
    }

    [Fact]
    public void Deactivate_ActiveRate_SetsInactive()
    {
        var rate = NewRate();

        rate.Deactivate();

        rate.Status.Should().Be(TaxRateStatus.Inactive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsDomainException()
    {
        var rate = NewRate();
        rate.Deactivate();

        var act = () => rate.Deactivate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_InactiveRate_SetsActive()
    {
        var rate = NewRate();
        rate.Deactivate();

        rate.Activate();

        rate.Status.Should().Be(TaxRateStatus.Active);
    }

    [Fact]
    public void Update_ChangesRateAndApplicability()
    {
        var rate = NewRate(10m);

        rate.Update("VAT 15%", 15m, TaxApplicability.Sales, "Sales only");

        rate.Rate.Should().Be(15m);
        rate.Name.Should().Be("VAT 15%");
        rate.Applicability.Should().Be(TaxApplicability.Sales);
        rate.Description.Should().Be("Sales only");
    }
}
