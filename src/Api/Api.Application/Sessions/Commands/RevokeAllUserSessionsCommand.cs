using Api.Application.Common.Interfaces;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using MediatR;

namespace Api.Application.Sessions.Commands;

public record RevokeAllUserSessionsCommand(Guid TargetUserId, Guid AdminId) : IRequest;

public class RevokeAllUserSessionsCommandHandler(
    ISessionRepository sessionRepository,
    ISessionNotifier sessionNotifier)
    : IRequestHandler<RevokeAllUserSessionsCommand>
{
    public async Task Handle(RevokeAllUserSessionsCommand request, CancellationToken ct)
    {
        var sessions = await sessionRepository.GetActiveSessionsByUserIdAsync(request.TargetUserId, ct);

        foreach (var session in sessions)
        {
            session.Revoke(request.AdminId, SessionRevokedReason.AdminRevoked);
            await sessionRepository.UpdateAsync(session, ct);

            await sessionNotifier.NotifySessionRevoked(
                session.UserId,
                session.Id,
                SessionRevokedReason.AdminRevoked.ToString(),
                ct);
        }
    }
}
