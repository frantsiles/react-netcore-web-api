using Api.Domain.Common;

namespace Sales.Domain.DomainEvents;

public record QuoteCreatedEvent(
    Guid QuoteId,
    string QuoteNumber,
    Guid CustomerId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record QuoteSentEvent(
    Guid QuoteId,
    string QuoteNumber,
    Guid CustomerId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record QuoteAcceptedEvent(
    Guid QuoteId,
    string QuoteNumber,
    Guid CustomerId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record QuoteConvertedToOrderEvent(
    Guid QuoteId,
    string QuoteNumber,
    Guid CustomerId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
