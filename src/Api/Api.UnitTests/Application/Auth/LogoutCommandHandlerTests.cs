using Api.Application.Auth.Commands.Logout;
using Api.Application.Common.Interfaces;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly Mock<ITokenHasher> _tokenHasher = new();
    private readonly LogoutCommandHandler _handler;

    private static readonly DeviceInfo Device = DeviceInfo.Create("TestAgent/1.0", "127.0.0.1");

    public LogoutCommandHandlerTests()
    {
        _tokenHasher.Setup(h => h.HashToken(It.IsAny<string>())).Returns("hashed");
        _handler = new LogoutCommandHandler(_sessionRepo.Object, _tokenHasher.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldRevokeSession()
    {
        var userId = Guid.NewGuid();
        var session = Session.Create(userId, "hashed", Device, DateTime.UtcNow.AddDays(1));
        _sessionRepo.Setup(r => r.GetByRefreshTokenHashAsync("hashed", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(session);
        _sessionRepo.Setup(r => r.UpdateAsync(session, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        await _handler.Handle(new LogoutCommand("rawToken", userId), default);

        session.IsActive.Should().BeFalse();
        _sessionRepo.Verify(r => r.UpdateAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTokenBelongingToDifferentUser_ShouldDoNothing()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var session = Session.Create(ownerId, "hashed", Device, DateTime.UtcNow.AddDays(1));
        _sessionRepo.Setup(r => r.GetByRefreshTokenHashAsync("hashed", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(session);

        await _handler.Handle(new LogoutCommand("rawToken", requesterId), default);

        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedSession_ShouldDoNothing()
    {
        var userId = Guid.NewGuid();
        var session = Session.Create(userId, "hashed", Device, DateTime.UtcNow.AddDays(1));
        session.Revoke(userId, SessionRevokedReason.Logout);
        _sessionRepo.Setup(r => r.GetByRefreshTokenHashAsync("hashed", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(session);

        await _handler.Handle(new LogoutCommand("rawToken", userId), default);

        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ShouldDoNothing()
    {
        _sessionRepo.Setup(r => r.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Session?)null);

        await _handler.Handle(new LogoutCommand("unknownToken", Guid.NewGuid()), default);

        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
