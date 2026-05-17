using Api.Domain.Common;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Banking.Application.BankAccounts.Commands.VoidTransaction;

public record VoidTransactionCommand(
    Guid BankAccountId,
    Guid TransactionId) : IRequest<BankAccountDto>;

public class VoidTransactionCommandValidator : AbstractValidator<VoidTransactionCommand>
{
    public VoidTransactionCommandValidator()
    {
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.TransactionId).NotEmpty();
    }
}

public class VoidTransactionCommandHandler : IRequestHandler<VoidTransactionCommand, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;

    public VoidTransactionCommandHandler(IBankAccountRepository repo) => _repo = repo;

    public async Task<BankAccountDto> Handle(VoidTransactionCommand cmd, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(cmd.BankAccountId, ct)
            ?? throw new DomainException($"Bank account '{cmd.BankAccountId}' not found.");

        account.VoidTransaction(cmd.TransactionId);
        await _repo.UpdateAsync(account, ct);
        return account.ToDto();
    }
}
