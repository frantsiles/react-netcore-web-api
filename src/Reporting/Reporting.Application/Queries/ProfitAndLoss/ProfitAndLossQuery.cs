using MediatR;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Application.Queries.ProfitAndLoss;

public record ProfitAndLossQuery(DateTime From, DateTime To) : IRequest<ProfitAndLossReportDto>;

public class ProfitAndLossHandler(IReportingStore store)
    : IRequestHandler<ProfitAndLossQuery, ProfitAndLossReportDto>
{
    public Task<ProfitAndLossReportDto> Handle(ProfitAndLossQuery query, CancellationToken ct) =>
        store.GetProfitAndLossAsync(query.From, query.To, ct);
}
