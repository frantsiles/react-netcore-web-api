using MediatR;

namespace BFF.Application.Auth;

/// <summary>
/// BFF command: forwards login credentials to the backend API and returns the token.
/// The BFF is the only service React talks to — React never calls the backend API directly.
/// </summary>
public record LoginBffCommand(string Email, string Password) : IRequest<LoginBffResult>;

public record LoginBffResult(string Token, string Email, string FullName, IEnumerable<string> Permissions);
