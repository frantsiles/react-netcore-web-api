using Api.Application.Auth.Commands.Login;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly Mock<ITokenHasher> _tokenHasher = new();
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _tokenHasher.Setup(h => h.HashToken(It.IsAny<string>())).Returns("hashedRefreshToken");
        _sessionRepo.Setup(r => r.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _tokenGenerator.Setup(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<Guid>()))
                       .Returns("jwt-token");

        _handler = new LoginCommandHandler(
            _userRepo.Object,
            _passwordHasher.Object,
            _tokenGenerator.Object,
            _tokenHasher.Object,
            _sessionRepo.Object);
    }

    private static LoginCommand ValidCommand(string email = "john@example.com", string password = "password")
        => new(email, password, "TestAgent/1.0", "127.0.0.1");

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnLoginResult()
    {
        var user = User.Create("John", "Doe", "john@example.com", "hashedPw");
        _userRepo.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("password", "hashedPw")).Returns(true);

        var result = await _handler.Handle(ValidCommand(), default);

        result.Token.Should().Be("jwt-token");
        result.Email.Should().Be("john@example.com");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.SessionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ShouldThrowDomainException()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = async () => await _handler.Handle(ValidCommand("nobody@example.com"), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrowDomainException()
    {
        var user = User.Create("John", "Doe", "john@example.com", "hashedPw");
        _userRepo.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hashedPw")).Returns(false);

        var act = async () => await _handler.Handle(ValidCommand(password: "wrong"), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid email or password.");
    }
}
