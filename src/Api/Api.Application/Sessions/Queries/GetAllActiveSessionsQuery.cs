using Api.Domain.Sessions.Repositories;
using Api.Domain.Users.Repositories;
using MediatR;

namespace Api.Application.Sessions.Queries;

public record GetAllActiveSessionsQuery : IRequest<IReadOnlyList<SessionAdminDto>>;

public class GetAllActiveSessionsQueryHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetAllActiveSessionsQuery, IReadOnlyList<SessionAdminDto>>
{
    public async Task<IReadOnlyList<SessionAdminDto>> Handle(GetAllActiveSessionsQuery request, CancellationToken ct)
    {
        var sessions = await sessionRepository.GetAllActiveSessionsAsync(ct);

        var userIds = sessions.Select(s => s.UserId).Distinct().ToList();
        var users = new Dictionary<Guid, (string Email, string FullName)>();

        foreach (var userId in userIds)
        {
            var user = await userRepository.GetByIdAsync(userId, ct);
            if (user is not null)
                users[userId] = (user.Email.Value, user.FullName);
        }

        return sessions
            .Select(s =>
            {
                var (email, fullName) = users.TryGetValue(s.UserId, out var u)
                    ? u
                    : ("unknown", "Unknown User");

                return new SessionAdminDto(
                    s.Id,
                    s.UserId,
                    email,
                    fullName,
                    s.DeviceInfo.UserAgent,
                    s.DeviceInfo.IpAddress,
                    s.CreatedAt,
                    s.LastUsedAt);
            })
            .ToList();
    }
}
