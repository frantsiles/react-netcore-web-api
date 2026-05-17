using Reporting.Application.Common.Dtos;

namespace Reporting.Application.Common.Interfaces;

public interface IReportingStore
{
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(string fiscalPeriod, CancellationToken ct = default);
    Task<ProfitAndLossReportDto> GetProfitAndLossAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<InventoryPositionReportDto> GetInventoryPositionAsync(CancellationToken ct = default);
    Task<AgingReportDto> GetAccountsReceivableAgingAsync(DateTime asOf, CancellationToken ct = default);
    Task<AgingReportDto> GetAccountsPayableAgingAsync(DateTime asOf, CancellationToken ct = default);
    Task<KpiDashboardDto> GetKpiDashboardAsync(CancellationToken ct = default);
    Task<RevenueTimeSeriesDto> GetRevenueTimeSeriesAsync(int months, CancellationToken ct = default);
}
