namespace Api.Application.Sessions.Queries;

public record SessionDto(
    Guid SessionId,
    string UserAgent,
    string IpAddress,
    DateTime CreatedAt,
    DateTime LastUsedAt,
    DateTime ExpiresAt,
    bool IsActive,
    bool IsCurrent);

public record SessionAdminDto(
    Guid SessionId,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string UserAgent,
    string IpAddress,
    DateTime CreatedAt,
    DateTime LastUsedAt);
