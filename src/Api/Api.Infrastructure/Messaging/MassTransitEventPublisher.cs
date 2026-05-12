using Api.Application.Common.Interfaces;
using MassTransit;

namespace Api.Infrastructure.Messaging;

public class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class
        => publishEndpoint.Publish(message, ct);
}
