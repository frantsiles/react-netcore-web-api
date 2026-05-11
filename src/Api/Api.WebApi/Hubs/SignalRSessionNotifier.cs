using Api.Application.Common.Interfaces;
using Api.Application.Sessions.Queries;
using Microsoft.AspNetCore.SignalR;

namespace Api.WebApi.Hubs;

public class SignalRSessionNotifier(IHubContext<SessionHub> hubContext) : ISessionNotifier
{
    public async Task NotifySessionRevoked(Guid userId, Guid sessionId, string reason, CancellationToken ct = default)
        => await hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("SessionRevoked", new { sessionId, reason }, ct);

    public async Task NotifyNewSession(SessionAdminDto session, CancellationToken ct = default)
        => await hubContext.Clients
            .Group("admin-sessions")
            .SendAsync("NewSession", session, ct);
}
