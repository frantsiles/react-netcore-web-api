import api from '@/services/api'
import type {
  PurchaseOrderDto,
  SearchPurchaseOrdersParams,
  CreatePurchaseOrderBody,
  AddPoLineBody,
  ReceivePoLineBody,
} from '@/types/erp/purchasing'

export const purchasingService = {
  orders: {
    search: (params: SearchPurchaseOrdersParams) =>
      api.get<PurchaseOrderDto[]>('/bff/purchasing/orders', { params }).then(r => r.data),

    create: (body: CreatePurchaseOrderBody) =>
      api.post<PurchaseOrderDto>('/bff/purchasing/orders', body).then(r => r.data),

    addLine: (orderId: string, body: AddPoLineBody) =>
      api.post<PurchaseOrderDto>(`/bff/purchasing/orders/${orderId}/lines`, body).then(r => r.data),

    send: (orderId: string) =>
      api.post<PurchaseOrderDto>(`/bff/purchasing/orders/${orderId}/send`).then(r => r.data),

    confirm: (orderId: string) =>
      api.post<PurchaseOrderDto>(`/bff/purchasing/orders/${orderId}/confirm`).then(r => r.data),

    receive: (orderId: string, body: ReceivePoLineBody) =>
      api.post<PurchaseOrderDto>(`/bff/purchasing/orders/${orderId}/receive`, body).then(r => r.data),

    cancel: (orderId: string, reason: string) =>
      api.post<PurchaseOrderDto>(`/bff/purchasing/orders/${orderId}/cancel`, { reason }).then(r => r.data),
  },
}
