using Api.Domain.Common;
using Catalog.Domain.ValueObjects;
using FluentAssertions;

namespace Catalog.UnitTests.Domain;

public class UnitOfMeasureTests
{
    [Theory]
    [InlineData("EA")]
    [InlineData("KGM")]
    [InlineData("HUR")]
    public void Create_WithValidCode_Succeeds(string code)
    {
        var uom = UnitOfMeasure.Create(code);
        uom.Code.Should().Be(code.ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCode_ThrowsDomainException(string code)
    {
        var act = () => UnitOfMeasure.Create(code);
        act.Should().Throw<DomainException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_NormalizesToUpperCase()
    {
        var uom = UnitOfMeasure.Create("kgm");
        uom.Code.Should().Be("KGM");
    }

    [Fact]
    public void TwoSameCodes_AreEqual()
    {
        UnitOfMeasure.Create("EA").Should().Be(UnitOfMeasure.Create("ea"));
    }

    [Fact]
    public void TwoDifferentCodes_AreNotEqual()
    {
        UnitOfMeasure.Create("EA").Should().NotBe(UnitOfMeasure.Create("KGM"));
    }
}
