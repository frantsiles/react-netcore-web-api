import api from '@/services/api'
import type {
  QuoteDto, SalesOrderDto,
  SearchQuotesParams, SearchOrdersParams,
  CreateQuoteBody, AddQuoteLineBody, ConvertToOrderBody,
} from '@/types/erp/sales'

export const salesService = {
  quotes: {
    search: (params: SearchQuotesParams) =>
      api.get<QuoteDto[]>('/bff/sales/quotes', { params }).then(r => r.data),

    create: (body: CreateQuoteBody) =>
      api.post<QuoteDto>('/bff/sales/quotes', body).then(r => r.data),

    addLine: (quoteId: string, body: AddQuoteLineBody) =>
      api.post<QuoteDto>(`/bff/sales/quotes/${quoteId}/lines`, body).then(r => r.data),

    send: (quoteId: string) =>
      api.post<QuoteDto>(`/bff/sales/quotes/${quoteId}/send`).then(r => r.data),

    accept: (quoteId: string) =>
      api.post<QuoteDto>(`/bff/sales/quotes/${quoteId}/accept`).then(r => r.data),

    reject: (quoteId: string) =>
      api.post<QuoteDto>(`/bff/sales/quotes/${quoteId}/reject`).then(r => r.data),

    convertToOrder: (quoteId: string, body: ConvertToOrderBody) =>
      api.post<SalesOrderDto>(`/bff/sales/quotes/${quoteId}/convert-to-order`, body).then(r => r.data),
  },

  orders: {
    search: (params: SearchOrdersParams) =>
      api.get<SalesOrderDto[]>('/bff/sales/orders', { params }).then(r => r.data),

    confirm: (orderId: string) =>
      api.post<SalesOrderDto>(`/bff/sales/orders/${orderId}/confirm`).then(r => r.data),

    cancel: (orderId: string, reason: string) =>
      api.post<SalesOrderDto>(`/bff/sales/orders/${orderId}/cancel`, { reason }).then(r => r.data),
  },
}
