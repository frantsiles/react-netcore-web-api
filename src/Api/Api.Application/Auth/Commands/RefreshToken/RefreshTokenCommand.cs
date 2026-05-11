using MediatR;

namespace Api.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string UserAgent, string IpAddress)
    : IRequest<RefreshTokenResult>;

public record RefreshTokenResult(string Token, string RefreshToken, Guid SessionId);
