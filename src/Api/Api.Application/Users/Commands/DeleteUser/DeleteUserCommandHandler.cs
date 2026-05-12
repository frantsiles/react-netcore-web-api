using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Api.Domain.Users.Repositories;
using MediatR;
using Shared.Messages;

namespace Api.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler(
    IUserRepository userRepository,
    IEventPublisher eventPublisher) : IRequestHandler<DeleteUserCommand>
{
    private const string AdminRoleName = "Admin";

    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new DomainException($"User '{request.UserId}' does not exist.");

        if (!user.IsActive)
            throw new DomainException("User is already deactivated.");

        bool isAdmin = user.Roles.Any(r => r.Name == AdminRoleName);
        if (isAdmin)
        {
            var allUsers = await userRepository.GetAllAsync(ct);
            int activeAdmins = allUsers.Count(u => u.IsActive && u.Roles.Any(r => r.Name == AdminRoleName));
            if (activeAdmins <= 1)
                throw new DomainException("Cannot deactivate the last active Admin user.");
        }

        user.Deactivate();
        await userRepository.UpdateAsync(user, ct);

        await eventPublisher.PublishAsync(
            new UserDeleted(user.Id, DateTime.UtcNow),
            ct);
    }
}
