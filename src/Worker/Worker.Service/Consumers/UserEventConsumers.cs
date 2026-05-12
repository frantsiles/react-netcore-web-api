using MassTransit;
using Shared.Messages;

namespace Worker.Service.Consumers;

public class UserCreatedConsumer(ILogger<UserCreatedConsumer> logger) : IConsumer<UserCreated>
{
    public Task Consume(ConsumeContext<UserCreated> context)
    {
        logger.LogInformation(
            "User created — Id: {UserId} | Email: {Email} | Role: {Role} | At: {CreatedAt}",
            context.Message.UserId,
            context.Message.Email,
            context.Message.Role,
            context.Message.CreatedAt);

        return Task.CompletedTask;
    }
}

public class UserDeletedConsumer(ILogger<UserDeletedConsumer> logger) : IConsumer<UserDeleted>
{
    public Task Consume(ConsumeContext<UserDeleted> context)
    {
        logger.LogInformation(
            "User deleted — Id: {UserId} | At: {DeletedAt}",
            context.Message.UserId,
            context.Message.DeletedAt);

        return Task.CompletedTask;
    }
}

public class UserRoleChangedConsumer(ILogger<UserRoleChangedConsumer> logger) : IConsumer<UserRoleChanged>
{
    public Task Consume(ConsumeContext<UserRoleChanged> context)
    {
        logger.LogInformation(
            "User role changed — Id: {UserId} | {OldRole} → {NewRole} | At: {ChangedAt}",
            context.Message.UserId,
            context.Message.OldRole,
            context.Message.NewRole,
            context.Message.ChangedAt);

        return Task.CompletedTask;
    }
}
