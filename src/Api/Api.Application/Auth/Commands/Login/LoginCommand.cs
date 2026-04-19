using MediatR;

namespace Api.Application.Auth.Commands.Login;

/// <summary>
/// Command: authenticate a user with email + password.
/// Returns a JWT token on success.
/// </summary>
public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string Token, string Email, string FullName, IEnumerable<string> Permissions);
