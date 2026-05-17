using Api.Domain.Common;

namespace Invoicing.Domain.DomainEvents;

public record InvoiceCreatedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    Guid? OriginSalesOrderId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn => OccurredAt;
}

public record InvoiceIssuedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    Money TotalAmount,
    DateOnly DueDate,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn => OccurredAt;
}

public record InvoicePaymentRecordedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    Money PaymentAmount,
    Money NewPaidAmount,
    Money NewBalanceDue,
    DateOnly PaidAt,
    string? Reference,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn => OccurredAt;
}

public record InvoiceCancelledEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn => OccurredAt;
}

public record InvoiceVoidedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn => OccurredAt;
}
