using Api.Application.Common.Interfaces;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using MediatR;

namespace Api.Application.Sessions.Commands;

public record RevokeAllMySessionsCommand(Guid UserId, Guid CurrentSessionId) : IRequest;

public class RevokeAllMySessionsCommandHandler(
    ISessionRepository sessionRepository,
    ISessionNotifier sessionNotifier)
    : IRequestHandler<RevokeAllMySessionsCommand>
{
    public async Task Handle(RevokeAllMySessionsCommand request, CancellationToken ct)
    {
        var sessions = await sessionRepository.GetActiveSessionsByUserIdAsync(request.UserId, ct);

        foreach (var session in sessions.Where(s => s.Id != request.CurrentSessionId))
        {
            session.Revoke(request.UserId, SessionRevokedReason.Logout);
            await sessionRepository.UpdateAsync(session, ct);

            await sessionNotifier.NotifySessionRevoked(
                session.UserId,
                session.Id,
                SessionRevokedReason.Logout.ToString(),
                ct);
        }
    }
}
