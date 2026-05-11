using Api.Application.Common.Interfaces;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using MediatR;

namespace Api.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken, Guid UserId) : IRequest;

public class LogoutCommandHandler(
    ISessionRepository sessionRepository,
    ITokenHasher tokenHasher) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var hash = tokenHasher.HashToken(request.RefreshToken);
        var session = await sessionRepository.GetByRefreshTokenHashAsync(hash, ct);

        if (session is null || session.UserId != request.UserId)
            return;

        if (!session.IsActive)
            return;

        session.Revoke(request.UserId, SessionRevokedReason.Logout);
        await sessionRepository.UpdateAsync(session, ct);
    }
}
