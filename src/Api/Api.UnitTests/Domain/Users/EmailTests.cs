using Api.Domain.Common;
using Api.Domain.Users.ValueObjects;
using FluentAssertions;

namespace Api.UnitTests.Domain.Users;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user.name+tag@sub.domain.com")]
    public void Create_WithValidEmail_ShouldNormalizeToLowercase(string input)
    {
        var email = Email.Create(input);
        email.Value.Should().Be(input.ToLowerInvariant());
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("")]
    public void Create_WithInvalidEmail_ShouldThrowDomainException(string input)
    {
        var act = () => Email.Create(input);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TwoEmailsWithSameValue_ShouldBeEqual()
    {
        var a = Email.Create("user@example.com");
        var b = Email.Create("USER@EXAMPLE.COM");
        a.Should().Be(b);
    }
}
