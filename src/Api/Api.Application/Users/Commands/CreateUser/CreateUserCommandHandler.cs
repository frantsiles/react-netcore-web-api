using Api.Application.Common.Interfaces;
using Api.Application.Users.Queries.GetUsers;
using Api.Domain.Common;
using Api.Domain.Roles.Repositories;
using Api.Domain.Users;
using Api.Domain.Users.Repositories;
using MediatR;
using Shared.Messages;

namespace Api.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IEventPublisher eventPublisher) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, ct))
            throw new DomainException($"A user with email '{request.Email}' already exists.");

        var role = await roleRepository.GetByNameAsync(request.RoleName, ct)
            ?? throw new DomainException($"Role '{request.RoleName}' does not exist.");

        var hash = passwordHasher.Hash(request.Password);
        User user = User.Create(request.FirstName, request.LastName, request.Email, hash);
        user.AssignRole(role);

        await userRepository.AddAsync(user, ct);

        await eventPublisher.PublishAsync(
            new UserCreated(user.Id, user.Email.Value, role.Name, DateTime.UtcNow),
            ct);

        return new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.IsActive,
            user.Roles.Select(r => r.Name));
    }
}
