using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.Repositories;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Accounts.Commands.UpdateAccount;

public record UpdateAccountCommand(
    Guid AccountId,
    string Name,
    string? Description = null) : IRequest<AccountDto>;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    private readonly IAccountRepository _repo;

    public UpdateAccountCommandHandler(IAccountRepository repo) => _repo = repo;

    public async Task<AccountDto> Handle(UpdateAccountCommand cmd, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(cmd.AccountId, ct)
            ?? throw new DomainException($"Account '{cmd.AccountId}' not found.");

        account.Update(cmd.Name, cmd.Description);
        await _repo.UpdateAsync(account, ct);
        return account.ToDto();
    }
}
