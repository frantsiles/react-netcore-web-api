using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.BankAccounts;
using Banking.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Banking.Application.BankAccounts.Commands.CreateBankAccount;

public record CreateBankAccountCommand(
    string AccountNumber,
    string BankName,
    string CurrencyCode,
    Guid? LinkedAccountingAccountId = null,
    string? Iban = null,
    string? Swift = null) : IRequest<BankAccountDto>;

public class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
    }
}

public class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IEventPublisher _events;

    public CreateBankAccountCommandHandler(
        IBankAccountRepository repo, ITenantContext tenant, IEventPublisher events)
        => (_repo, _tenant, _events) = (repo, tenant, events);

    public async Task<BankAccountDto> Handle(CreateBankAccountCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsByAccountNumberAsync(_tenant.TenantId, cmd.AccountNumber, ct))
            throw new DomainException($"Bank account '{cmd.AccountNumber}' already exists.");

        var account = BankAccount.Create(_tenant.TenantId, cmd.AccountNumber, cmd.BankName,
            cmd.CurrencyCode, cmd.LinkedAccountingAccountId, cmd.Iban, cmd.Swift);

        await _repo.AddAsync(account, ct);
        foreach (var e in account.DomainEvents)
            await _events.PublishAsync(e, ct);

        return account.ToDto();
    }
}
