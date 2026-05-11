using Api.Application.Sessions.Queries;

namespace Api.Application.Common.Interfaces;

public interface ISessionNotifier
{
    Task NotifySessionRevoked(Guid userId, Guid sessionId, string reason, CancellationToken ct = default);
    Task NotifyNewSession(SessionAdminDto session, CancellationToken ct = default);
}
