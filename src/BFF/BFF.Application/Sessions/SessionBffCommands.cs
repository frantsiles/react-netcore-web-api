using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Sessions;

public record RevokeSessionBffCommand(Guid SessionId, string BearerToken) : IRequest;

public class RevokeSessionBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RevokeSessionBffCommand>
{
    public async Task Handle(RevokeSessionBffCommand request, CancellationToken ct)
        => await apiClient.PatchAsync($"api/sessions/{request.SessionId}/revoke", request.BearerToken, ct);
}

public record RevokeAllMySessionsBffCommand(string BearerToken) : IRequest;

public class RevokeAllMySessionsBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RevokeAllMySessionsBffCommand>
{
    public async Task Handle(RevokeAllMySessionsBffCommand request, CancellationToken ct)
        => await apiClient.DeleteAsync("api/sessions/my", request.BearerToken, ct);
}

public record RevokeAllUserSessionsBffCommand(Guid UserId, string BearerToken) : IRequest;

public class RevokeAllUserSessionsBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RevokeAllUserSessionsBffCommand>
{
    public async Task Handle(RevokeAllUserSessionsBffCommand request, CancellationToken ct)
        => await apiClient.DeleteAsync($"api/admin/users/{request.UserId}/sessions", request.BearerToken, ct);
}
