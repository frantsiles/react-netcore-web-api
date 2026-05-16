using Api.Domain.Common;

namespace Accounting.Domain.DomainEvents;

public record AccountCreatedEvent(
    Guid AccountId, Guid TenantId, string AccountNumber, string Name,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record JournalEntryPostedEvent(
    Guid JournalEntryId, Guid TenantId, string EntryNumber,
    decimal TotalAmount, string FiscalPeriod,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record JournalEntryReversedEvent(
    Guid JournalEntryId, Guid TenantId, string EntryNumber, string ReversalEntryNumber,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
