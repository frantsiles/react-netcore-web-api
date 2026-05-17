import api from '@/services/api'

export interface KpiDashboardDto {
  asOf: string; totalRevenue: number; totalCogs: number; grossMarginPct: number
  totalPayables: number; totalReceivables: number; openPurchaseOrders: number
  openSalesOrders: number; openApprovalRequests: number; lowStockItems: number; cashBalance: number
}

export interface TrialBalanceLineDto { accountNumber: string; accountName: string; accountType: string; debitBalance: number; creditBalance: number }
export interface TrialBalanceDto { fiscalPeriod: string; lines: TrialBalanceLineDto[]; totalDebits: number; totalCredits: number; isBalanced: boolean }

export interface PnLLineDto { accountNumber: string; accountName: string; amount: number }
export interface PnLSectionDto { section: string; lines: PnLLineDto[]; total: number }
export interface ProfitAndLossDto { from: string; to: string; sections: PnLSectionDto[]; grossProfit: number; operatingIncome: number; netIncome: number }

export const reportsService = {
  kpi: () => api.get<KpiDashboardDto>('/bff/reports/kpi-dashboard').then(r => r.data),
  trialBalance: (fiscalPeriod: string) =>
    api.get<TrialBalanceDto>('/bff/reports/trial-balance', { params: { fiscalPeriod } }).then(r => r.data),
  profitAndLoss: (from: string, to: string) =>
    api.get<ProfitAndLossDto>('/bff/reports/profit-and-loss', { params: { from, to } }).then(r => r.data),
}
