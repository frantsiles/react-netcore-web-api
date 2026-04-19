using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Users.Repositories;
using MediatR;

namespace Api.Application.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new DomainException("Invalid email or password.");

        if (!user.IsActive)
            throw new DomainException("Account is disabled.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash.Value))
            throw new DomainException("Invalid email or password.");

        var token = tokenGenerator.GenerateToken(user);

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct();

        return new LoginResult(token, user.Email.Value, user.FullName, permissions);
    }
}
