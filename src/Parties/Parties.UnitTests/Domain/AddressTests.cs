using Api.Domain.Common;
using FluentAssertions;
using Parties.Domain.ValueObjects;

namespace Parties.UnitTests.Domain;

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var address = Address.Create("123 Main", null, "Austin", "TX", "78701", "US");

        address.Line1.Should().Be("123 Main");
        address.City.Should().Be("Austin");
        address.CountryCode.Should().Be("US");
    }

    [Theory]
    [InlineData("", "city", "state", "12345", "US", "*Line 1*")]
    [InlineData("street", "", "state", "12345", "US", "*City*")]
    [InlineData("street", "city", "", "12345", "US", "*State*")]
    [InlineData("street", "city", "state", "", "US", "*Postal*")]
    [InlineData("street", "city", "state", "12345", "X", "*2-letter*")]
    public void Create_WithMissingField_ThrowsDomainException(
        string line1, string city, string state, string postal, string country, string msgPattern)
    {
        var act = () => Address.Create(line1, null, city, state, postal, country);

        act.Should().Throw<DomainException>().WithMessage(msgPattern);
    }

    [Fact]
    public void AsPrimary_ReturnsCopyWithIsPrimaryTrue()
    {
        var address = Address.Create("1 St", null, "City", "State", "12345", "US");
        var primary = address.AsPrimary();

        primary.IsPrimary.Should().BeTrue();
        address.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void TwoAddressesWithSameData_AreEqual()
    {
        var a = Address.Create("1 St", null, "City", "State", "12345", "US");
        var b = Address.Create("1 St", null, "City", "State", "12345", "US");

        a.Should().Be(b);
    }
}
