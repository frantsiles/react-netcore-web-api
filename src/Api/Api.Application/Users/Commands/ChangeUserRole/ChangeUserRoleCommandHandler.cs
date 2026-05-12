using Api.Application.Common.Interfaces;
using Api.Application.Users.Queries.GetUsers;
using Api.Domain.Common;
using Api.Domain.Roles.Repositories;
using Api.Domain.Users.Repositories;
using MediatR;
using Shared.Messages;

namespace Api.Application.Users.Commands.ChangeUserRole;

public class ChangeUserRoleCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IEventPublisher eventPublisher) : IRequestHandler<ChangeUserRoleCommand, UserDto>
{
    public async Task<UserDto> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new DomainException($"User '{request.UserId}' does not exist.");

        string oldRoleName = user.Roles.FirstOrDefault()?.Name ?? string.Empty;

        var newRole = await roleRepository.GetByNameAsync(request.NewRoleName, ct)
            ?? throw new DomainException($"Role '{request.NewRoleName}' does not exist.");

        if (oldRoleName == newRole.Name)
            return new UserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email.Value,
                user.IsActive,
                user.Roles.Select(r => r.Name));

        user.ChangeRole(newRole);
        await userRepository.UpdateAsync(user, ct);

        await eventPublisher.PublishAsync(
            new UserRoleChanged(user.Id, oldRoleName, newRole.Name, DateTime.UtcNow),
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
