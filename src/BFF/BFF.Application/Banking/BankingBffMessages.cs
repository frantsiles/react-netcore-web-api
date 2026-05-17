namespace BFF.Application.Banking;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record BankAccountBffDto(
    Guid Id, string AccountNumber, string BankName, string CurrencyCode,
    decimal Balance, string Status, Guid? LinkedAccountingAccountId,
    string? Iban, string? Swift, int TransactionCount, int UnreconciledCount,
    DateTime CreatedAt, DateTime UpdatedAt);

public record BankTransactionBffDto(
    Guid Id, DateTime TransactionDate, string Description, decimal Amount,
    string Type, string Status, string? ReferenceNumber,
    Guid? LinkedJournalEntryId, DateTime CreatedAt);

// ── Queries ───────────────────────────────────────────────────────────────────

public record ListBankAccountsBffQuery(string Token, string? Status)
    : MediatR.IRequest<IReadOnlyList<BankAccountBffDto>>;

public record GetBankTransactionsBffQuery(
    string Token, Guid AccountId, string? Status, DateTime? From, DateTime? To)
    : MediatR.IRequest<IReadOnlyList<BankTransactionBffDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateBankAccountBffCommand(
    string Token, string AccountNumber, string BankName, string CurrencyCode,
    string? Iban, string? Swift, Guid? LinkedAccountingAccountId)
    : MediatR.IRequest<BankAccountBffDto>;

public record AddBankTransactionBffCommand(
    string Token, Guid AccountId, DateTime TransactionDate, string Description,
    decimal Amount, string Type, string? ReferenceNumber)
    : MediatR.IRequest<BankTransactionBffDto>;

public record ReconcileTransactionBffCommand(
    string Token, Guid AccountId, Guid TransactionId, Guid JournalEntryId)
    : MediatR.IRequest<BankTransactionBffDto>;

public record UnreconcileBankTransactionBffCommand(
    string Token, Guid AccountId, Guid TransactionId)
    : MediatR.IRequest<BankTransactionBffDto>;

public record VoidBankTransactionBffCommand(
    string Token, Guid AccountId, Guid TransactionId)
    : MediatR.IRequest<BankTransactionBffDto>;
