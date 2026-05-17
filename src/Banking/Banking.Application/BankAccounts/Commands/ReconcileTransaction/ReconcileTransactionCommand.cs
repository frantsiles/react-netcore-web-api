using Api.Domain.Common;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Banking.Application.BankAccounts.Commands.ReconcileTransaction;

public record ReconcileTransactionCommand(
    Guid BankAccountId,
    Guid TransactionId,
    Guid JournalEntryId) : IRequest<BankAccountDto>;

public class ReconcileTransactionCommandValidator : AbstractValidator<ReconcileTransactionCommand>
{
    public ReconcileTransactionCommandValidator()
    {
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.JournalEntryId).NotEmpty();
    }
}

public class ReconcileTransactionCommandHandler : IRequestHandler<ReconcileTransactionCommand, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;

    public ReconcileTransactionCommandHandler(IBankAccountRepository repo) => _repo = repo;

    public async Task<BankAccountDto> Handle(ReconcileTransactionCommand cmd, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(cmd.BankAccountId, ct)
            ?? throw new DomainException($"Bank account '{cmd.BankAccountId}' not found.");

        account.ReconcileTransaction(cmd.TransactionId, cmd.JournalEntryId);
        await _repo.UpdateAsync(account, ct);
        return account.ToDto();
    }
}
