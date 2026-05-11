using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Auth;

public record RefreshTokenBffCommand(string RefreshToken, string UserAgent, string IpAddress)
    : IRequest<RefreshTokenBffResult>;

public record RefreshTokenBffResult(string Token, string RefreshToken, Guid SessionId);

public class RefreshTokenBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<RefreshTokenBffCommand, RefreshTokenBffResult>
{
    public async Task<RefreshTokenBffResult> Handle(RefreshTokenBffCommand request, CancellationToken ct)
    {
        var response = await apiClient.PostAsync<RefreshRequest, RefreshResponse>(
            "api/auth/refresh",
            new RefreshRequest(request.RefreshToken, request.UserAgent, request.IpAddress),
            ct);

        if (response is null)
            throw new InvalidOperationException("Backend API returned an empty response.");

        return new RefreshTokenBffResult(response.Token, response.RefreshToken, response.SessionId);
    }
}

record RefreshRequest(string RefreshToken, string UserAgent, string IpAddress);
record RefreshResponse(string Token, string RefreshToken, Guid SessionId);
