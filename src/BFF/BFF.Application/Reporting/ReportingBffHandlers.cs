using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Reporting;

public class GetKpiDashboardBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetKpiDashboardBffQuery, KpiDashboardBffDto>
{
    public async Task<KpiDashboardBffDto> Handle(GetKpiDashboardBffQuery request, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<KpiDashboardBffDto>(
            "api/reports/kpi-dashboard", request.Token, ct);
        return result!;
    }
}

public class GetTrialBalanceBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetTrialBalanceBffQuery, TrialBalanceBffDto>
{
    public async Task<TrialBalanceBffDto> Handle(GetTrialBalanceBffQuery request, CancellationToken ct)
    {
        var url = $"api/reports/trial-balance?fiscalPeriod={Uri.EscapeDataString(request.FiscalPeriod)}";
        var result = await apiClient.GetAsync<TrialBalanceBffDto>(url, request.Token, ct);
        return result!;
    }
}

public class GetProfitAndLossBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetProfitAndLossBffQuery, ProfitAndLossBffDto>
{
    public async Task<ProfitAndLossBffDto> Handle(GetProfitAndLossBffQuery request, CancellationToken ct)
    {
        var url = $"api/reports/profit-and-loss?from={request.From:O}&to={request.To:O}";
        var result = await apiClient.GetAsync<ProfitAndLossBffDto>(url, request.Token, ct);
        return result!;
    }
}

public class GetRevenueTimeSeriesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetRevenueTimeSeriesBffQuery, RevenueTimeSeriesBffDto>
{
    public async Task<RevenueTimeSeriesBffDto> Handle(GetRevenueTimeSeriesBffQuery request, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<RevenueTimeSeriesBffDto>(
            $"api/reports/revenue-time-series?months={request.Months}", request.Token, ct);
        return result!;
    }
}

public class GetBalanceSheetBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetBalanceSheetBffQuery, BalanceSheetBffDto>
{
    public async Task<BalanceSheetBffDto> Handle(GetBalanceSheetBffQuery request, CancellationToken ct)
    {
        var url = request.AsOf.HasValue
            ? $"api/reports/balance-sheet?asOf={request.AsOf.Value:O}"
            : "api/reports/balance-sheet";
        var result = await apiClient.GetAsync<BalanceSheetBffDto>(url, request.Token, ct);
        return result!;
    }
}
