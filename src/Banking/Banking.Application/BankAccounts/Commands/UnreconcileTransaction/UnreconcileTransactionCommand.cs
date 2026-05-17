using Api.Domain.Common;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Banking.Application.BankAccounts.Commands.UnreconcileTransaction;

public record UnreconcileTransactionCommand(
    Guid BankAccountId,
    Guid TransactionId) : IRequest<BankAccountDto>;

public class UnreconcileTransactionCommandValidator : AbstractValidator<UnreconcileTransactionCommand>
{
    public UnreconcileTransactionCommandValidator()
    {
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.TransactionId).NotEmpty();
    }
}

public class UnreconcileTransactionCommandHandler : IRequestHandler<UnreconcileTransactionCommand, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;

    public UnreconcileTransactionCommandHandler(IBankAccountRepository repo) => _repo = repo;

    public async Task<BankAccountDto> Handle(UnreconcileTransactionCommand cmd, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(cmd.BankAccountId, ct)
            ?? throw new DomainException($"Bank account '{cmd.BankAccountId}' not found.");

        account.UnreconcileTransaction(cmd.TransactionId);
        await _repo.UpdateAsync(account, ct);
        return account.ToDto();
    }
}
