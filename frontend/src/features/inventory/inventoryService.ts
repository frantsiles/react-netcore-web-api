import api from '@/services/api'
import type {
  InventoryItemDto, WarehouseDto, StockMovementDto,
  SearchInventoryParams, ReceiveStockBody, AdjustStockBody,
} from '@/types/erp/inventory'

export const inventoryService = {
  items: {
    search: (params: SearchInventoryParams) =>
      api.get<InventoryItemDto[]>('/bff/inventory/items', { params }).then(r => r.data),

    receive: (body: ReceiveStockBody) =>
      api.post<InventoryItemDto>('/bff/inventory/items/receive', body).then(r => r.data),

    adjust: (itemId: string, body: AdjustStockBody) =>
      api.post<InventoryItemDto>(`/bff/inventory/items/${itemId}/adjust`, body).then(r => r.data),

    writeOff: (itemId: string, body: { quantity: number; reason: string; notes?: string }) =>
      api.post<InventoryItemDto>(`/bff/inventory/items/${itemId}/write-off`, body).then(r => r.data),

    getMovements: (itemId: string, type?: string) =>
      api.get<StockMovementDto[]>(`/bff/inventory/items/${itemId}/movements`, {
        params: { type, skip: 0, take: 100 },
      }).then(r => r.data),
  },

  warehouses: {
    list: () => api.get<WarehouseDto[]>('/bff/inventory/warehouses').then(r => r.data),
    create: (body: { code: string; name: string; address?: string }) =>
      api.post<WarehouseDto>('/bff/inventory/warehouses', body).then(r => r.data),
  },
}
