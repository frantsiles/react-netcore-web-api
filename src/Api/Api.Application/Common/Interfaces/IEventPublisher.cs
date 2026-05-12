namespace Api.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the message bus publisher.
/// Defined in Application so command handlers don't depend on MassTransit directly.
/// Implemented in Infrastructure on top of IPublishEndpoint + EF Core Outbox.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class;
}
