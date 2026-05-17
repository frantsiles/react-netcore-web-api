import api from '@/services/api'

export type TaxApplicability = 'Sales' | 'Purchases' | 'Both'
export type TaxRateStatus = 'Active' | 'Inactive'

export interface TaxRateDto {
  id: string; code: string; name: string; rate: number
  applicability: TaxApplicability; status: TaxRateStatus; description?: string
  createdAt: string; updatedAt: string
}

export const taxService = {
  rates: {
    list: (status?: string) =>
      api.get<TaxRateDto[]>('/bff/tax/rates', { params: { status } }).then(r => r.data),
    create: (body: { code: string; name: string; rate: number; applicability: string; description?: string }) =>
      api.post<TaxRateDto>('/bff/tax/rates', body).then(r => r.data),
    deactivate: (id: string) =>
      api.post<TaxRateDto>(`/bff/tax/rates/${id}/deactivate`).then(r => r.data),
  },
}
