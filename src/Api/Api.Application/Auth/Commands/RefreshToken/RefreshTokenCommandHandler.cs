using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using Api.Domain.Users.Repositories;
using MediatR;
using System.Security.Cryptography;

namespace Api.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    IJwtTokenGenerator tokenGenerator,
    ITokenHasher tokenHasher) : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var hash = tokenHasher.HashToken(request.RefreshToken);
        var session = await sessionRepository.GetByRefreshTokenHashAsync(hash, ct)
            ?? throw new DomainException("Invalid refresh token.");

        if (!session.IsActive)
            throw new DomainException("Session has been revoked or expired.");

        var user = await userRepository.GetByIdAsync(session.UserId, ct)
            ?? throw new DomainException("User not found.");

        if (!user.IsActive)
            throw new DomainException("Account is disabled.");

        session.Revoke(null, SessionRevokedReason.TokenRotation);
        await sessionRepository.UpdateAsync(session, ct);

        var newRawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var newHash = tokenHasher.HashToken(newRawToken);

        var deviceInfo = DeviceInfo.Create(request.UserAgent, session.DeviceInfo.IpAddress);
        var newSession = Session.Create(
            user.Id,
            newHash,
            deviceInfo,
            DateTime.UtcNow.AddDays(30));

        await sessionRepository.AddAsync(newSession, ct);

        var accessToken = tokenGenerator.GenerateToken(user, newSession.Id);

        return new RefreshTokenResult(accessToken, newRawToken, newSession.Id);
    }
}
