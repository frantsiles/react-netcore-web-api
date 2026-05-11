using Api.Domain.Sessions.Repositories;
using MediatR;

namespace Api.Application.Sessions.Queries;

public record GetMySessionsQuery(Guid UserId, Guid CurrentSessionId) : IRequest<IReadOnlyList<SessionDto>>;

public class GetMySessionsQueryHandler(ISessionRepository sessionRepository)
    : IRequestHandler<GetMySessionsQuery, IReadOnlyList<SessionDto>>
{
    public async Task<IReadOnlyList<SessionDto>> Handle(GetMySessionsQuery request, CancellationToken ct)
    {
        var sessions = await sessionRepository.GetActiveSessionsByUserIdAsync(request.UserId, ct);

        return sessions
            .Select(s => new SessionDto(
                s.Id,
                s.DeviceInfo.UserAgent,
                s.DeviceInfo.IpAddress,
                s.CreatedAt,
                s.LastUsedAt,
                s.ExpiresAt,
                s.IsActive,
                s.Id == request.CurrentSessionId))
            .ToList();
    }
}
