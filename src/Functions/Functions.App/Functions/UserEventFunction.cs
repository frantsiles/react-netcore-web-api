using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions.App.Functions;

/// <summary>
/// Processes user domain events published to the Azure Service Bus topic "user-events".
/// Local equivalent: RabbitMQ via MassTransit in Worker.Service.
/// </summary>
public class UserEventFunction(ILogger<UserEventFunction> logger)
{
    [Function("ProcessUserCreated")]
    public void ProcessUserCreated(
        [ServiceBusTrigger(
            topicName: "user-events",
            subscriptionName: "user-created-subscription",
            Connection = "ServiceBusConnection")] string message)
    {
        logger.LogInformation("UserCreated event received: {Message}", message);
        // production: deserialize → update read models, send welcome email, etc.
    }

    [Function("ProcessUserDeleted")]
    public void ProcessUserDeleted(
        [ServiceBusTrigger(
            topicName: "user-events",
            subscriptionName: "user-deleted-subscription",
            Connection = "ServiceBusConnection")] string message)
    {
        logger.LogInformation("UserDeleted event received: {Message}", message);
        // production: clean up user data, revoke sessions, audit log
    }
}
