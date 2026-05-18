using MediatR;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Application.Queries.BalanceSheet;

public record BalanceSheetQuery(DateTime AsOf) : IRequest<BalanceSheetReportDto>;

public class BalanceSheetHandler(IReportingStore store)
    : IRequestHandler<BalanceSheetQuery, BalanceSheetReportDto>
{
    public Task<BalanceSheetReportDto> Handle(BalanceSheetQuery query, CancellationToken ct) =>
        store.GetBalanceSheetAsync(query.AsOf, ct);
}
