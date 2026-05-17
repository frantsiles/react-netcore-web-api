using MediatR;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Application.Queries.RevenueTimeSeries;

public record RevenueTimeSeriesQuery(int Months = 6) : IRequest<RevenueTimeSeriesDto>;

public class RevenueTimeSeriesHandler(IReportingStore store)
    : IRequestHandler<RevenueTimeSeriesQuery, RevenueTimeSeriesDto>
{
    public Task<RevenueTimeSeriesDto> Handle(RevenueTimeSeriesQuery query, CancellationToken ct) =>
        store.GetRevenueTimeSeriesAsync(query.Months, ct);
}
