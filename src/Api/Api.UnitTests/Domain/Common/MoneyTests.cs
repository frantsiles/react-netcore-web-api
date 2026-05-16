using Api.Domain.Common;
using FluentAssertions;

namespace Api.UnitTests.Domain.Common;

public class MoneyTests
{
    [Fact]
    public void Of_WithValidAmountAndCurrency_ShouldCreate()
    {
        var money = Money.Of(100m, "USD");

        money.Amount.Should().Be(100m);
        money.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Of_NormalizesLowercaseCurrencyCode()
    {
        var money = Money.Of(50m, "usd");
        money.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Of_WithNegativeAmount_ShouldThrow()
    {
        var act = () => Money.Of(-1m, "USD");
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData(null)]
    public void Of_WithInvalidCurrencyCode_ShouldThrow(string? code)
    {
        var act = () => Money.Of(10m, code!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Zero_ShouldCreateZeroAmount()
    {
        var zero = Money.Zero("MXN");
        zero.Amount.Should().Be(0m);
        zero.CurrencyCode.Should().Be("MXN");
    }

    [Fact]
    public void Add_SameCurrency_ShouldSumAmounts()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(50m, "USD");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
        result.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Add_DifferentCurrencies_ShouldThrow()
    {
        var usd = Money.Of(100m, "USD");
        var mxn = Money.Of(100m, "MXN");

        var act = () => usd.Add(mxn);
        act.Should().Throw<DomainException>().WithMessage("*currencies*");
    }

    [Fact]
    public void Subtract_SameCurrency_ShouldReturnDifference()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(30m, "USD");

        var result = a.Subtract(b);
        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Subtract_WhenResultWouldBeNegative_ShouldThrow()
    {
        var a = Money.Of(10m, "USD");
        var b = Money.Of(50m, "USD");

        var act = () => a.Subtract(b);
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Multiply_ByFactor_ShouldRoundToTwoDecimals()
    {
        var price = Money.Of(33.33m, "USD");
        var result = price.Multiply(3m);
        result.Amount.Should().Be(99.99m);
    }

    [Fact]
    public void Multiply_ByNegativeFactor_ShouldThrow()
    {
        var money = Money.Of(10m, "USD");
        var act = () => money.Multiply(-1m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_ShouldBeEqual()
    {
        var a = Money.Of(100m, "USD");
        var b = Money.Of(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrency_ShouldNotBeEqual()
    {
        var usd = Money.Of(100m, "USD");
        var eur = Money.Of(100m, "EUR");

        usd.Should().NotBe(eur);
    }
}
