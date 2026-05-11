using Api.Domain.Common;
using Api.Domain.Sessions;
using Api.Domain.Sessions.ValueObjects;
using FluentAssertions;

namespace Api.UnitTests.Domain.Sessions;

public class SessionTests
{
    private static DeviceInfo DefaultDevice =>
        DeviceInfo.Create("Mozilla/5.0 (Test Browser)", "127.0.0.1");

    [Fact]
    public void Create_WithValidData_ShouldCreateActiveSession()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var session = Session.Create(userId, "tokenHash", DefaultDevice, expiresAt);

        session.UserId.Should().Be(userId);
        session.RefreshTokenHash.Should().Be("tokenHash");
        session.IsActive.Should().BeTrue();
        session.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Revoke_ShouldSetRevokedAt()
    {
        var session = Session.Create(Guid.NewGuid(), "hash", DefaultDevice, DateTime.UtcNow.AddDays(1));
        var adminId = Guid.NewGuid();

        session.Revoke(adminId, SessionRevokedReason.AdminRevoked);

        session.RevokedAt.Should().NotBeNull();
        session.RevokedBy.Should().Be(adminId);
        session.RevokedReason.Should().Be(SessionRevokedReason.AdminRevoked);
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ShouldThrowDomainException()
    {
        var session = Session.Create(Guid.NewGuid(), "hash", DefaultDevice, DateTime.UtcNow.AddDays(1));
        session.Revoke(null, SessionRevokedReason.Logout);

        var act = () => session.Revoke(null, SessionRevokedReason.Logout);

        act.Should().Throw<DomainException>().WithMessage("*already revoked*");
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldReturnFalse()
    {
        var session = Session.Create(Guid.NewGuid(), "hash", DefaultDevice, DateTime.UtcNow.AddDays(1));

        // Simulate expiry by setting ExpiresAt to the past via reflection
        var prop = typeof(Session).GetProperty("ExpiresAt")!;
        prop.SetValue(session, DateTime.UtcNow.AddSeconds(-1));

        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_ShouldUpdateLastUsedAt()
    {
        var session = Session.Create(Guid.NewGuid(), "hash", DefaultDevice, DateTime.UtcNow.AddDays(1));
        var before = session.LastUsedAt;

        session.MarkUsed();

        session.LastUsedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowDomainException()
    {
        var act = () => Session.Create(Guid.Empty, "hash", DefaultDevice, DateTime.UtcNow.AddDays(1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithPastExpiry_ShouldThrowDomainException()
    {
        var act = () => Session.Create(Guid.NewGuid(), "hash", DefaultDevice, DateTime.UtcNow.AddSeconds(-1));
        act.Should().Throw<DomainException>();
    }
}
