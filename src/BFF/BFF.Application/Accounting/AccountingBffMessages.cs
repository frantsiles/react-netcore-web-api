namespace BFF.Application.Accounting;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record AccountBffDto(
    Guid Id, string AccountNumber, string Name, string Type, string CurrencyCode,
    decimal Balance, bool IsActive, string? Description, DateTime CreatedAt, DateTime UpdatedAt);

public record JournalEntryLineBffDto(
    Guid Id, Guid AccountId, string AccountNumber, string AccountName,
    string Side, decimal Amount, string? Description);

public record JournalEntryBffDto(
    Guid Id, string EntryNumber, string FiscalPeriod, DateTime EntryDate, string Status,
    string Description, string? ReferenceType, Guid? ReferenceId,
    decimal TotalDebits, decimal TotalCredits, bool IsBalanced,
    IReadOnlyList<JournalEntryLineBffDto> Lines, DateTime CreatedAt, DateTime UpdatedAt);

// ── Queries ───────────────────────────────────────────────────────────────────

public record ListAccountsBffQuery(string Token, string? Type, bool? IsActive)
    : MediatR.IRequest<IReadOnlyList<AccountBffDto>>;

public record SearchJournalEntriesBffQuery(
    string Token, string? FiscalPeriod, string? Status, Guid? AccountId, int Skip, int Take)
    : MediatR.IRequest<IReadOnlyList<JournalEntryBffDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateAccountBffCommand(
    string Token, string AccountNumber, string Name, string Type,
    string CurrencyCode, string? Description)
    : MediatR.IRequest<AccountBffDto>;

public record CreateJournalEntryBffCommand(
    string Token, string FiscalPeriod, DateTime EntryDate, string Description,
    string? ReferenceType, Guid? ReferenceId)
    : MediatR.IRequest<JournalEntryBffDto>;

public record AddJeLineBffCommand(
    string Token, Guid EntryId, Guid AccountId, string Side, decimal Amount, string? Description)
    : MediatR.IRequest<JournalEntryBffDto>;

public record PostJournalEntryBffCommand(string Token, Guid EntryId)
    : MediatR.IRequest<JournalEntryBffDto>;

public record ReverseJournalEntryBffCommand(string Token, Guid EntryId, DateTime ReversalDate)
    : MediatR.IRequest<JournalEntryBffDto>;
