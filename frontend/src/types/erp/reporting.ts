export interface MonthlyRevenueDto {
  month: string
  revenue: number
  cogs: number
}

export interface MonthlyOrdersDto {
  month: string
  salesOrders: number
  purchaseOrders: number
}

export interface RevenueTimeSeriesDto {
  revenue: MonthlyRevenueDto[]
  orders: MonthlyOrdersDto[]
}

export interface KpiDashboardDto {
  asOf: string
  totalRevenue: number
  totalCogs: number
  grossMarginPct: number
  totalPayables: number
  totalReceivables: number
  openPurchaseOrders: number
  openSalesOrders: number
  openApprovalRequests: number
  lowStockItems: number
  cashBalance: number
}

export interface TrialBalanceLineDto {
  accountNumber: string
  accountName: string
  accountType: string
  debitBalance: number
  creditBalance: number
}

export interface TrialBalanceReportDto {
  fiscalPeriod: string
  lines: TrialBalanceLineDto[]
  totalDebits: number
  totalCredits: number
  isBalanced: boolean
}

export interface PnLLineDto {
  accountNumber: string
  accountName: string
  amount: number
}

export interface PnLSectionDto {
  section: string
  lines: PnLLineDto[]
  total: number
}

export interface ProfitAndLossReportDto {
  from: string
  to: string
  sections: PnLSectionDto[]
  grossProfit: number
  operatingIncome: number
  netIncome: number
}

export interface InventoryPositionLineDto {
  sku: string
  itemName: string
  warehouseName: string
  quantityOnHand: number
  reorderPoint: number
  belowReorderPoint: boolean
}

export interface InventoryPositionReportDto {
  asOf: string
  lines: InventoryPositionLineDto[]
  totalItems: number
  totalUnits: number
}
