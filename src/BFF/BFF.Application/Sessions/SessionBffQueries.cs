using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Sessions;

// ── My sessions ──────────────────────────────────────────────────────────────

public record GetMySessionsBffQuery(string BearerToken) : IRequest<IReadOnlyList<SessionBffDto>>;

public record SessionBffDto(
    Guid SessionId,
    string UserAgent,
    string IpAddress,
    DateTime CreatedAt,
    DateTime LastUsedAt,
    DateTime ExpiresAt,
    bool IsActive,
    bool IsCurrent);

public class GetMySessionsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetMySessionsBffQuery, IReadOnlyList<SessionBffDto>>
{
    public async Task<IReadOnlyList<SessionBffDto>> Handle(GetMySessionsBffQuery request, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<List<SessionBffDto>>("api/sessions/my", request.BearerToken, ct);
        return result ?? [];
    }
}

// ── Admin: all sessions ───────────────────────────────────────────────────────

public record GetAllSessionsBffQuery(string BearerToken) : IRequest<IReadOnlyList<SessionAdminBffDto>>;

public record SessionAdminBffDto(
    Guid SessionId,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string UserAgent,
    string IpAddress,
    DateTime CreatedAt,
    DateTime LastUsedAt);

public class GetAllSessionsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetAllSessionsBffQuery, IReadOnlyList<SessionAdminBffDto>>
{
    public async Task<IReadOnlyList<SessionAdminBffDto>> Handle(GetAllSessionsBffQuery request, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<List<SessionAdminBffDto>>("api/sessions", request.BearerToken, ct);
        return result ?? [];
    }
}
