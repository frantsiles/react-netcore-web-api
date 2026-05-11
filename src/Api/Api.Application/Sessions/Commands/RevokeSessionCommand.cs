using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using MediatR;

namespace Api.Application.Sessions.Commands;

public record RevokeSessionCommand(Guid SessionId, Guid RequesterId, bool RequesterIsAdmin) : IRequest;

public class RevokeSessionCommandHandler(
    ISessionRepository sessionRepository,
    ISessionNotifier sessionNotifier)
    : IRequestHandler<RevokeSessionCommand>
{
    public async Task Handle(RevokeSessionCommand request, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, ct)
            ?? throw new DomainException("Session not found.");

        if (!request.RequesterIsAdmin && session.UserId != request.RequesterId)
            throw new DomainException("Access denied.");

        var reason = request.RequesterIsAdmin && session.UserId != request.RequesterId
            ? SessionRevokedReason.AdminRevoked
            : SessionRevokedReason.Logout;

        session.Revoke(request.RequesterId, reason);
        await sessionRepository.UpdateAsync(session, ct);

        await sessionNotifier.NotifySessionRevoked(
            session.UserId,
            session.Id,
            reason.ToString(),
            ct);
    }
}
