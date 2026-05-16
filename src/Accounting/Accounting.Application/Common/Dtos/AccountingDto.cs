using Accounting.Domain.Accounts;
using Accounting.Domain.JournalEntries;

namespace Accounting.Application.Common.Dtos;

public record AccountDto(
    Guid Id,
    Guid TenantId,
    string AccountNumber,
    string Name,
    AccountType Type,
    string CurrencyCode,
    decimal Balance,
    bool IsActive,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record JournalEntryLineDto(
    Guid Id,
    Guid AccountId,
    string AccountNumber,
    string AccountName,
    EntrySide Side,
    decimal Amount,
    string? Description);

public record JournalEntryDto(
    Guid Id,
    Guid TenantId,
    string EntryNumber,
    string FiscalPeriod,
    DateTime EntryDate,
    EntryStatus Status,
    string Description,
    string? ReferenceType,
    Guid? ReferenceId,
    decimal TotalDebits,
    decimal TotalCredits,
    bool IsBalanced,
    IReadOnlyList<JournalEntryLineDto> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt);
