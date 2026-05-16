using Api.Domain.Common;

namespace Sales.Domain.DomainEvents;

public record SalesOrderCreatedEvent(
    Guid SalesOrderId,
    string OrderNumber,
    Guid CustomerId,
    Guid? OriginQuoteId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record SalesOrderConfirmedEvent(
    Guid SalesOrderId,
    string OrderNumber,
    Guid CustomerId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record SalesOrderCancelledEvent(
    Guid SalesOrderId,
    string OrderNumber,
    Guid CustomerId,
    string Reason,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
