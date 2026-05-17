using Api.Application.Common.Interfaces;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.BankAccounts;
using Banking.Domain.Repositories;
using MediatR;

namespace Banking.Application.BankAccounts.Queries.ListBankAccounts;

public record ListBankAccountsQuery(BankAccountStatus? Status = null) : IRequest<IReadOnlyList<BankAccountDto>>;

public class ListBankAccountsQueryHandler : IRequestHandler<ListBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    private readonly IBankAccountRepository _repo;
    private readonly ITenantContext _tenant;

    public ListBankAccountsQueryHandler(IBankAccountRepository repo, ITenantContext tenant)
        => (_repo, _tenant) = (repo, tenant);

    public async Task<IReadOnlyList<BankAccountDto>> Handle(ListBankAccountsQuery query, CancellationToken ct)
    {
        var accounts = await _repo.ListAsync(_tenant.TenantId, query.Status, ct);
        return accounts.Select(a => a.ToDto()).ToList();
    }
}
