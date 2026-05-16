using Api.Application.Common.Interfaces;
using Api.Application.Sessions.Commands;
using Api.Domain.Common;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using FluentAssertions;
using Moq;

namespace Api.UnitTests.Application.Sessions;

public class RevokeSessionCommandHandlerTests
{
    private readonly Mock<ISessionRepository> _sessionRepo = new();
    private readonly Mock<ISessionNotifier> _notifier = new();
    private readonly RevokeSessionCommandHandler _handler;

    private static readonly DeviceInfo Device = DeviceInfo.Create("TestAgent/1.0", "127.0.0.1");

    public RevokeSessionCommandHandlerTests()
    {
        _notifier.Setup(n => n.NotifySessionRevoked(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessionRepo.Setup(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _handler = new RevokeSessionCommandHandler(_sessionRepo.Object, _notifier.Object);
    }

    private Session MakeActiveSession(Guid userId)
    {
        var session = Session.Create(userId, "hashed", Device, DateTime.UtcNow.AddDays(1));
        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(session);
        return session;
    }

    [Fact]
    public async Task Handle_OwnerRevoking_ShouldRevokeWithLogoutReason()
    {
        var userId = Guid.NewGuid();
        var session = MakeActiveSession(userId);

        await _handler.Handle(new RevokeSessionCommand(session.Id, userId, RequesterIsAdmin: false), default);

        session.IsActive.Should().BeFalse();
        _sessionRepo.Verify(r => r.UpdateAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(n => n.NotifySessionRevoked(
            userId, session.Id, SessionRevokedReason.Logout.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminRevokingOtherUserSession_ShouldRevokeWithAdminReason()
    {
        var targetUserId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var session = MakeActiveSession(targetUserId);

        await _handler.Handle(new RevokeSessionCommand(session.Id, adminId, RequesterIsAdmin: true), default);

        session.IsActive.Should().BeFalse();
        _notifier.Verify(n => n.NotifySessionRevoked(
            targetUserId, session.Id, SessionRevokedReason.AdminRevoked.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonAdminRevokingOtherUserSession_ShouldThrowDomainException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var session = MakeActiveSession(ownerId);

        var act = async () => await _handler.Handle(
            new RevokeSessionCommand(session.Id, otherId, RequesterIsAdmin: false), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Access denied*");
        _sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidSessionId_ShouldThrowDomainException()
    {
        _sessionRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Session?)null);

        var act = async () => await _handler.Handle(
            new RevokeSessionCommand(Guid.NewGuid(), Guid.NewGuid(), RequesterIsAdmin: false), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Session not found*");
    }
}
