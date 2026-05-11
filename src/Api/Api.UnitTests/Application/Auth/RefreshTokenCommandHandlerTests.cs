using Api.Application.Auth.Commands.RefreshToken;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly Mock<ITokenHasher> _tokenHasher = new();
    private readonly RefreshTokenCommandHandler _handler;

    private static readonly DeviceInfo Device =
        DeviceInfo.Create("TestAgent/1.0", "127.0.0.1");

    public RefreshTokenCommandHandlerTests()
    {
        _tokenGenerator.Setup(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<Guid>()))
                       .Returns("new-jwt");
        _tokenHasher.Setup(h => h.HashToken(It.IsAny<string>())).Returns("hashed");
        _sessionRepo.Setup(r => r.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        _handler = new RefreshTokenCommandHandler(
            _sessionRepo.Object, _userRepo.Object, _tokenGenerator.Object, _tokenHasher.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldRotateToken()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("Jane", "Doe", "jane@test.com", "hash");
        var session = Session.Create(userId, "hashed", Device, DateTime.UtcNow.AddDays(1));

        _sessionRepo.Setup(r => r.GetByRefreshTokenHashAsync("hashed", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(session);
        _sessionRepo.Setup(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _handler.Handle(
            new RefreshTokenCommand("rawToken", "TestAgent/1.0", "127.0.0.1"), default);

        result.Token.Should().Be("new-jwt");
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithRevokedSession_ShouldThrowDomainException()
    {
        var session = Session.Create(Guid.NewGuid(), "hashed", Device, DateTime.UtcNow.AddDays(1));
        session.Revoke(null, SessionRevokedReason.Logout);

        _sessionRepo.Setup(r => r.GetByRefreshTokenHashAsync("hashed", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(session);

        var act = async () => await _handler.Handle(
            new RefreshTokenCommand("rawToken", "TestAgent/1.0", "127.0.0.1"), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*revoked or expired*");
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ShouldThrowDomainException()
    {
        _sessionRepo.Setup(r => r.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Session?)null);

        var act = async () => await _handler.Handle(
            new RefreshTokenCommand("unknownToken", "TestAgent/1.0", "127.0.0.1"), default);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid refresh token.");
    }
}
