using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Auth;

public record LogoutBffCommand(string RefreshToken, string BearerToken) : IRequest;

public class LogoutBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<LogoutBffCommand>
{
    public async Task Handle(LogoutBffCommand request, CancellationToken ct)
    {
        await apiClient.PostAsync<LogoutRequest, object>(
            "api/auth/logout",
            new LogoutRequest(request.RefreshToken),
            request.BearerToken,
            ct);
    }
}

record LogoutRequest(string RefreshToken);
