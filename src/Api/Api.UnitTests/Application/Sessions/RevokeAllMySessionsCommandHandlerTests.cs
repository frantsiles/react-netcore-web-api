using Api.Application.Common.Interfaces;
using Api.Application.Sessions.Commands;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Sessions;

public class RevokeAllMySessionsCommandHandlerTests
{
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly Mock<ISessionNotifier> _notifier = new();
    private readonly RevokeAllMySessionsCommandHandler _handler;

    private static readonly DeviceInfo Device = DeviceInfo.Create("TestAgent/1.0", "127.0.0.1");

    public RevokeAllMySessionsCommandHandlerTests()
    {
        _notifier.Setup(n => n.NotifySessionRevoked(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessionRepo.Setup(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _handler = new RevokeAllMySessionsCommandHandler(_sessionRepo.Object, _notifier.Object);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllSessionsExceptCurrentOne()
    {
        var userId = Guid.NewGuid();
        var current = Session.Create(userId, "hash0", Device, DateTime.UtcNow.AddDays(1));
        var other1 = Session.Create(userId, "hash1", Device, DateTime.UtcNow.AddDays(1));
        var other2 = Session.Create(userId, "hash2", Device, DateTime.UtcNow.AddDays(1));

        _sessionRepo.Setup(r => r.GetActiveSessionsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([current, other1, other2]);

        await _handler.Handle(new RevokeAllMySessionsCommand(userId, current.Id), default);

        other1.IsActive.Should().BeFalse();
        other2.IsActive.Should().BeFalse();
        current.IsActive.Should().BeTrue();
        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _notifier.Verify(n => n.NotifySessionRevoked(
            userId, It.IsAny<Guid>(), SessionRevokedReason.Logout.ToString(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithNoOtherSessions_ShouldDoNothing()
    {
        var userId = Guid.NewGuid();
        var current = Session.Create(userId, "hash0", Device, DateTime.UtcNow.AddDays(1));

        _sessionRepo.Setup(r => r.GetActiveSessionsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([current]);

        await _handler.Handle(new RevokeAllMySessionsCommand(userId, current.Id), default);

        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
        _notifier.Verify(n => n.NotifySessionRevoked(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptySessions_ShouldDoNothing()
    {
        var userId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetActiveSessionsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);

        await _handler.Handle(new RevokeAllMySessionsCommand(userId, Guid.NewGuid()), default);

        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
