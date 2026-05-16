using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.Accounts;
using Accounting.Domain.Repositories;
using Api.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Queries.ListAccounts;

public record ListAccountsQuery(
    AccountType? Type = null,
    bool? IsActive = null) : IRequest<IReadOnlyList<AccountDto>>;

public class ListAccountsQueryHandler : IRequestHandler<ListAccountsQuery, IReadOnlyList<AccountDto>>
{
    private readonly IAccountRepository _repo;
    private readonly ITenantContext _tenant;

    public ListAccountsQueryHandler(IAccountRepository repo, ITenantContext tenant)
        => (_repo, _tenant) = (repo, tenant);

    public async Task<IReadOnlyList<AccountDto>> Handle(ListAccountsQuery query, CancellationToken ct)
    {
        var accounts = await _repo.ListAsync(_tenant.TenantId, query.Type, query.IsActive, ct);
        return accounts.Select(a => a.ToDto()).ToList();
    }
}
