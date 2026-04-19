using Api.Application.Auth.Commands.Login;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Auth;

/// <summary>
/// Tests for LoginCommandHandler using Moq to isolate from infrastructure.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_userRepo.Object, _passwordHasher.Object, _tokenGenerator.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnLoginResult()
    {
        // Arrange
        var user = User.Create("John", "Doe", "john@example.com", "hashedPw");
        _userRepo.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("password", "hashedPw")).Returns(true);
        _tokenGenerator.Setup(t => t.GenerateToken(user)).Returns("jwt-token");

        // Act
        var result = await _handler.Handle(new LoginCommand("john@example.com", "password"), default);

        // Assert
        result.Token.Should().Be("jwt-token");
        result.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ShouldThrowDomainException()
    {
        // Arrange
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(new LoginCommand("nobody@example.com", "pw"), default);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrowDomainException()
    {
        // Arrange
        var user = User.Create("John", "Doe", "john@example.com", "hashedPw");
        _userRepo.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hashedPw")).Returns(false);

        // Act
        var act = async () => await _handler.Handle(new LoginCommand("john@example.com", "wrong"), default);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid email or password.");
    }
}
