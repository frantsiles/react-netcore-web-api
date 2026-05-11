using Api.Domain.Common;
using Api.Domain.Sessions.ValueObjects;

namespace Api.Domain.Sessions;

public class Session : Entity
{
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public DeviceInfo DeviceInfo { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public SessionRevokedReason? RevokedReason { get; private set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    // EF Core
    private Session() : base() { RefreshTokenHash = ""; DeviceInfo = null!; }

    private Session(Guid userId, string refreshTokenHash, DeviceInfo deviceInfo, DateTime expiresAt) : base()
    {
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        DeviceInfo = deviceInfo;
        ExpiresAt = expiresAt;
        LastUsedAt = DateTime.UtcNow;
    }

    public static Session Create(Guid userId, string refreshTokenHash, DeviceInfo deviceInfo, DateTime expiresAt)
    {
        if (userId == Guid.Empty) throw new DomainException("UserId cannot be empty.");
        if (string.IsNullOrWhiteSpace(refreshTokenHash)) throw new DomainException("Refresh token hash cannot be empty.");
        if (expiresAt <= DateTime.UtcNow) throw new DomainException("Expiry must be in the future.");

        return new Session(userId, refreshTokenHash, deviceInfo, expiresAt);
    }

    public void Revoke(Guid? revokedBy, SessionRevokedReason reason)
    {
        if (RevokedAt is not null)
            throw new DomainException("Session is already revoked.");

        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokedReason = reason;
        SetUpdatedAt();
    }

    public void MarkUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
