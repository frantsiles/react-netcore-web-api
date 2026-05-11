using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Auth;

/// <summary>
/// BFF command: forwards login credentials to the backend API and returns the token.
/// The BFF is the only service React talks to — React never calls the backend API directly.
/// </summary>
public record LoginBffCommand(string Email, string Password, string UserAgent, string IpAddress)
    : IRequest<LoginBffResult>;

public record LoginBffResult(
    string Token,
    string RefreshToken,
    Guid SessionId,
    string Email,
    string FullName,
    IEnumerable<string> Permissions);

public class LoginBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<LoginBffCommand, LoginBffResult>
{
    public async Task<LoginBffResult> Handle(LoginBffCommand request, CancellationToken ct)
    {
        var response = await apiClient.PostAsync<LoginRequest, LoginResponse>(
            "api/auth/login",
            new LoginRequest(request.Email, request.Password, request.UserAgent, request.IpAddress),
            ct);

        if (response is null)
            throw new InvalidOperationException("Backend API returned an empty response.");

        return new LoginBffResult(
            response.Token,
            response.RefreshToken,
            response.SessionId,
            response.Email,
            response.FullName,
            response.Permissions);
    }
}

record LoginRequest(string Email, string Password, string UserAgent, string IpAddress);
record LoginResponse(
    string Token,
    string RefreshToken,
    Guid SessionId,
    string Email,
    string FullName,
    IEnumerable<string> Permissions);
