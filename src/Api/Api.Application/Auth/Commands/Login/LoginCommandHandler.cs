using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Sessions;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Sessions.ValueObjects;
using Api.Domain.Users.Repositories;
using MediatR;
using System.Security.Cryptography;

namespace Api.Application.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    ISessionRepository sessionRepository) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new DomainException("Invalid email or password.");

        if (!user.IsActive)
            throw new DomainException("Account is disabled.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash.Value))
            throw new DomainException("Invalid email or password.");

        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var refreshTokenHash = tokenHasher.HashToken(rawRefreshToken);

        var deviceInfo = DeviceInfo.Create(request.UserAgent, request.IpAddress);
        var session = Session.Create(
            user.Id,
            refreshTokenHash,
            deviceInfo,
            DateTime.UtcNow.AddDays(30));

        await sessionRepository.AddAsync(session, ct);

        var accessToken = tokenGenerator.GenerateToken(user, session.Id);

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct();

        return new LoginResult(accessToken, rawRefreshToken, session.Id, user.Email.Value, user.FullName, permissions);
    }
}
