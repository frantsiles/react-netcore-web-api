import api from '@/services/api'
import type { KpiDashboardDto, RevenueTimeSeriesDto, TrialBalanceReportDto, ProfitAndLossReportDto, InventoryPositionReportDto } from '@/types/erp/reporting'

export const reportingService = {
  getKpiDashboard: () =>
    api.get<KpiDashboardDto>('/bff/reports/kpi-dashboard').then(r => r.data),

  getRevenueTimeSeries: (months = 6) =>
    api.get<RevenueTimeSeriesDto>(`/bff/reports/revenue-time-series?months=${months}`).then(r => r.data),

  getTrialBalance: (fiscalPeriod: string) =>
    api.get<TrialBalanceReportDto>(`/bff/reports/trial-balance?fiscalPeriod=${fiscalPeriod}`).then(r => r.data),

  getProfitAndLoss: (from: string, to: string) =>
    api.get<ProfitAndLossReportDto>(`/bff/reports/profit-and-loss?from=${from}&to=${to}`).then(r => r.data),

  getInventoryPosition: () =>
    api.get<InventoryPositionReportDto>('/bff/reports/inventory-position').then(r => r.data),
}
