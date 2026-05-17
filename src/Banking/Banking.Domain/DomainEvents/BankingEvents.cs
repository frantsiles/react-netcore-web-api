using Api.Domain.Common;

namespace Banking.Domain.DomainEvents;

public record BankAccountCreatedEvent(
    Guid BankAccountId, Guid TenantId, string AccountNumber, string BankName,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record BankTransactionAddedEvent(
    Guid BankAccountId, Guid TenantId, Guid TransactionId,
    decimal Amount, string Type, DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record BankTransactionReconciledEvent(
    Guid BankAccountId, Guid TenantId, Guid TransactionId, Guid JournalEntryId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
