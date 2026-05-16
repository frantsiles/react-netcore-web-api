using Api.Application.Common.Interfaces;
using Api.Application.Sessions.Commands;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Sessions;

public class RevokeAllUserSessionsCommandHandlerTests
{
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly Mock<ISessionNotifier> _notifier = new();
    private readonly RevokeAllUserSessionsCommandHandler _handler;

    private static readonly DeviceInfo Device = DeviceInfo.Create("TestAgent/1.0", "127.0.0.1");

    public RevokeAllUserSessionsCommandHandlerTests()
    {
        _notifier.Setup(n => n.NotifySessionRevoked(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessionRepo.Setup(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _handler = new RevokeAllUserSessionsCommandHandler(_sessionRepo.Object, _notifier.Object);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllUserSessionsWithAdminReason()
    {
        var targetUserId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var session1 = Session.Create(targetUserId, "hash1", Device, DateTime.UtcNow.AddDays(1));
        var session2 = Session.Create(targetUserId, "hash2", Device, DateTime.UtcNow.AddDays(1));

        _sessionRepo.Setup(r => r.GetActiveSessionsByUserIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([session1, session2]);

        await _handler.Handle(new RevokeAllUserSessionsCommand(targetUserId, adminId), default);

        session1.IsActive.Should().BeFalse();
        session2.IsActive.Should().BeFalse();
        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _notifier.Verify(n => n.NotifySessionRevoked(
            targetUserId, It.IsAny<Guid>(), SessionRevokedReason.AdminRevoked.ToString(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithNoActiveSessions_ShouldDoNothing()
    {
        var targetUserId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetActiveSessionsByUserIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);

        await _handler.Handle(new RevokeAllUserSessionsCommand(targetUserId, Guid.NewGuid()), default);

        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
        _notifier.Verify(n => n.NotifySessionRevoked(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
