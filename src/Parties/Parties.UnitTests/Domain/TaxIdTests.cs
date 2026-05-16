using Api.Domain.Common;
using FluentAssertions;
using Parties.Domain.ValueObjects;

namespace Parties.UnitTests.Domain;

public class TaxIdTests
{
    [Theory]
    [InlineData("12-3456789", "US")]
    [InlineData("123456789", "US")]
    [InlineData("XAXX010101000", "MX")]
    [InlineData("A1234567B", "ES")]
    public void Create_WithValidValue_Succeeds(string value, string country)
    {
        var taxId = TaxId.Create(value, country);

        taxId.Value.Should().NotBeEmpty();
        taxId.CountryCode.Should().Be(country);
    }

    [Theory]
    [InlineData("", "US")]
    [InlineData("  ", "US")]
    public void Create_WithEmptyValue_ThrowsDomainException(string value, string country)
    {
        var act = () => TaxId.Create(value, country);

        act.Should().Throw<DomainException>().WithMessage("*Tax ID*empty*");
    }

    [Theory]
    [InlineData("123", "U")]
    [InlineData("123", "USA")]
    public void Create_WithInvalidCountryCode_ThrowsDomainException(string value, string country)
    {
        var act = () => TaxId.Create(value, country);

        act.Should().Throw<DomainException>().WithMessage("*2-letter ISO*");
    }

    [Fact]
    public void Create_WithInvalidUSFormat_ThrowsDomainException()
    {
        var act = () => TaxId.Create("INVALID-EIN", "US");

        act.Should().Throw<DomainException>().WithMessage("*not valid*");
    }

    [Fact]
    public void TwoSameTaxIds_AreEqual()
    {
        var a = TaxId.Create("12-3456789", "US");
        var b = TaxId.Create("12-3456789", "US");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoDifferentTaxIds_AreNotEqual()
    {
        var a = TaxId.Create("12-3456789", "US");
        var b = TaxId.Create("XAXX010101000", "MX");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Create_ForUnknownCountry_AllowsAnyValue()
    {
        var act = () => TaxId.Create("ANY-FORMAT-999", "JP");

        act.Should().NotThrow();
    }
}
