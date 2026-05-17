using MediatR;
using Reporting.Application.Common.Dtos;
using Reporting.Application.Common.Interfaces;

namespace Reporting.Application.Queries.KpiDashboard;

public record KpiDashboardQuery : IRequest<KpiDashboardDto>;

public class KpiDashboardHandler(IReportingStore store)
    : IRequestHandler<KpiDashboardQuery, KpiDashboardDto>
{
    public Task<KpiDashboardDto> Handle(KpiDashboardQuery query, CancellationToken ct) =>
        store.GetKpiDashboardAsync(ct);
}
