using MediatR;

namespace Api.Application.Auth.Commands.Login;

/// <summary>
/// Command: authenticate a user with email + password.
/// Returns a JWT access token, a refresh token, and the new session id.
/// </summary>
public record LoginCommand(string Email, string Password, string UserAgent, string IpAddress)
    : IRequest<LoginResult>;

public record LoginResult(
    string Token,
    string RefreshToken,
    Guid SessionId,
    string Email,
    string FullName,
    IEnumerable<string> Permissions);
