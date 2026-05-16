using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.Accounts;
using Accounting.Domain.Repositories;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Accounts.Commands.CreateAccount;

public record CreateAccountCommand(
    string AccountNumber,
    string Name,
    AccountType Type,
    string CurrencyCode,
    string? Description = null) : IRequest<AccountDto>;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
    }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IAccountRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IEventPublisher _events;

    public CreateAccountCommandHandler(IAccountRepository repo, ITenantContext tenant, IEventPublisher events)
        => (_repo, _tenant, _events) = (repo, tenant, events);

    public async Task<AccountDto> Handle(CreateAccountCommand cmd, CancellationToken ct)
    {
        if (await _repo.ExistsByNumberAsync(_tenant.TenantId, cmd.AccountNumber, ct))
            throw new DomainException($"Account number '{cmd.AccountNumber}' already exists.");

        var account = Account.Create(_tenant.TenantId, cmd.AccountNumber, cmd.Name,
            cmd.Type, cmd.CurrencyCode, cmd.Description);

        await _repo.AddAsync(account, ct);
        foreach (var e in account.DomainEvents)
            await _events.PublishAsync(e, ct);

        return account.ToDto();
    }
}
