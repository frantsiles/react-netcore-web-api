using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.Repositories;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Accounts.Queries.GetAccountById;

public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto>;

public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
{
    public GetAccountByIdQueryValidator() => RuleFor(x => x.AccountId).NotEmpty();
}

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IAccountRepository _repo;

    public GetAccountByIdQueryHandler(IAccountRepository repo) => _repo = repo;

    public async Task<AccountDto> Handle(GetAccountByIdQuery query, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(query.AccountId, ct)
            ?? throw new DomainException($"Account '{query.AccountId}' not found.");
        return account.ToDto();
    }
}
