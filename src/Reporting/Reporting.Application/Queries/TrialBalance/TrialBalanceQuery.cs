using MediatR;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Application.Queries.TrialBalance;

public record TrialBalanceQuery(string FiscalPeriod) : IRequest<TrialBalanceReportDto>;

public class TrialBalanceHandler(IReportingStore store)
    : IRequestHandler<TrialBalanceQuery, TrialBalanceReportDto>
{
    public Task<TrialBalanceReportDto> Handle(TrialBalanceQuery query, CancellationToken ct) =>
        store.GetTrialBalanceAsync(query.FiscalPeriod, ct);
}
