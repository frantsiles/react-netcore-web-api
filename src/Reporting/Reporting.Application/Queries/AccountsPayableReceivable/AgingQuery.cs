using MediatR;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Application.Queries.AccountsPayableReceivable;

public record AccountsReceivableAgingQuery(DateTime AsOf) : IRequest<AgingReportDto>;
public record AccountsPayableAgingQuery(DateTime AsOf) : IRequest<AgingReportDto>;

public class AccountsReceivableAgingHandler(IReportingStore store)
    : IRequestHandler<AccountsReceivableAgingQuery, AgingReportDto>
{
    public Task<AgingReportDto> Handle(AccountsReceivableAgingQuery query, CancellationToken ct) =>
        store.GetAccountsReceivableAgingAsync(query.AsOf, ct);
}

public class AccountsPayableAgingHandler(IReportingStore store)
    : IRequestHandler<AccountsPayableAgingQuery, AgingReportDto>
{
    public Task<AgingReportDto> Handle(AccountsPayableAgingQuery query, CancellationToken ct) =>
        store.GetAccountsPayableAgingAsync(query.AsOf, ct);
}
