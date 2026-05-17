using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.BankAccounts;
using Banking.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Banking.Application.BankAccounts.Commands.AddBankTransaction;

public record AddBankTransactionCommand(
    Guid BankAccountId,
    DateTime TransactionDate,
    string Description,
    decimal Amount,
    BankTransactionType Type,
    string? ReferenceNumber = null) : IRequest<BankAccountDto>;

public class AddBankTransactionCommandValidator : AbstractValidator<AddBankTransactionCommand>
{
    public AddBankTransactionCommandValidator()
    {
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.TransactionDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class AddBankTransactionCommandHandler : IRequestHandler<AddBankTransactionCommand, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;
    private readonly IEventPublisher _events;

    public AddBankTransactionCommandHandler(IBankAccountRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<BankAccountDto> Handle(AddBankTransactionCommand cmd, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(cmd.BankAccountId, ct)
            ?? throw new DomainException($"Bank account '{cmd.BankAccountId}' not found.");

        account.AddTransaction(cmd.TransactionDate, cmd.Description,
            cmd.Amount, cmd.Type, cmd.ReferenceNumber);

        await _repo.UpdateAsync(account, ct);
        foreach (var e in account.DomainEvents)
            await _events.PublishAsync(e, ct);

        return account.ToDto();
    }
}
