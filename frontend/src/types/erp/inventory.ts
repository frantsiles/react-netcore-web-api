export type WarehouseStatus = 'Active' | 'Inactive'
export type MovementType = 'Receipt' | 'Issue' | 'Adjustment' | 'WriteOff' | 'Reservation' | 'ReleaseReservation'

export interface WarehouseDto {
  id: string
  code: string
  name: string
  address?: string
  status: WarehouseStatus
  createdAt: string
}

export interface InventoryItemDto {
  id: string
  catalogItemId: string
  warehouseId: string
  sku: string
  quantityOnHand: number
  quantityReserved: number
  quantityAvailable: number
  reorderPoint?: number
  isLowStock: boolean
  createdAt: string
  updatedAt: string
}

export interface StockMovementDto {
  id: string
  type: MovementType
  quantity: number
  referenceNumber?: string
  referenceId?: string
  reason?: string
  notes?: string
  occurredAt: string
}

export interface SearchInventoryParams {
  warehouseId?: string
  sku?: string
  belowReorderPoint?: boolean
  skip?: number
  take?: number
}

export interface ReceiveStockBody {
  catalogItemId: string
  warehouseId: string
  sku: string
  quantity: number
  referenceNumber?: string
  notes?: string
  reorderPoint?: number
}

export interface AdjustStockBody {
  delta: number
  reason: string
  notes?: string
}
