using Banking.Domain.BankAccounts;

namespace Banking.Application.Common.Dtos;

public record BankTransactionDto(
    Guid Id,
    DateTime TransactionDate,
    string Description,
    decimal Amount,
    BankTransactionType Type,
    BankTransactionStatus Status,
    string? ReferenceNumber,
    Guid? LinkedJournalEntryId,
    DateTime CreatedAt);

public record BankAccountDto(
    Guid Id,
    Guid TenantId,
    string AccountNumber,
    string BankName,
    string CurrencyCode,
    decimal Balance,
    BankAccountStatus Status,
    Guid? LinkedAccountingAccountId,
    string? Iban,
    string? Swift,
    int TransactionCount,
    int UnreconciledCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
