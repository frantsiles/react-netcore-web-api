import api from '@/services/api'

export interface PayrollEntryDto {
  id: string
  employeeId: string
  employeeNumber: string
  employeeName: string
  baseSalary: number
  grossSalary: number
  overtimePay: number
  totalGross: number
  ccssEmployee: number
  bancoPopular: number
  incomeTax: number
  totalDeductions: number
  netPay: number
  ccssEmployer: number
  insEmployer: number
  fcl: number
  totalEmployerContribution: number
  totalLaborCost: number
}

export interface PayrollRunDto {
  id: string
  runNumber: string
  periodType: string
  periodStart: string
  periodEnd: string
  currencyCode: string
  status: string
  employeeCount: number
  totalGross: number
  totalDeductions: number
  totalNet: number
  totalEmployerCost: number
  createdAt: string
  confirmedAt: string | null
  paidAt: string | null
  entries: PayrollEntryDto[]
}

export const payrollService = {
  runs: {
    list: (skip = 0, take = 20) =>
      api.get<PayrollRunDto[]>(`/bff/payroll/runs?skip=${skip}&take=${take}`).then(r => r.data),
    getById: (id: string) =>
      api.get<PayrollRunDto>(`/bff/payroll/runs/${id}`).then(r => r.data),
    create: (body: { periodType: string; periodStart: string; periodEnd: string; currencyCode: string }) =>
      api.post<PayrollRunDto>('/bff/payroll/runs', body).then(r => r.data),
    confirm: (id: string) =>
      api.post<PayrollRunDto>(`/bff/payroll/runs/${id}/confirm`).then(r => r.data),
    downloadPaystub: async (runId: string, entryId: string, fileName: string) => {
      const response = await api.get(`/bff/payroll/runs/${runId}/entries/${entryId}/paystub`, {
        responseType: 'blob',
      })
      const url = URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }))
      const a = document.createElement('a')
      a.href = url
      a.download = fileName
      a.click()
      URL.revokeObjectURL(url)
    },
  },
}
