export type PurchaseOrderStatus =
  | 'Draft' | 'Sent' | 'Confirmed' | 'PartiallyReceived' | 'Received' | 'Cancelled'

export interface PurchaseOrderLineDto {
  id: string
  catalogItemId: string
  sku: string
  itemName: string
  quantityOrdered: number
  quantityReceived: number
  unitCostAmount: number
  currencyCode: string
  lineTotal: number
  notes?: string
}

export interface PurchaseOrderDto {
  id: string
  poNumber: string
  supplierId: string
  supplierName: string
  status: PurchaseOrderStatus
  expectedDeliveryDate?: string
  currencyCode: string
  countryCode: string
  notes?: string
  subtotal: number
  total: number
  lines: PurchaseOrderLineDto[]
  createdAt: string
  updatedAt: string
}

export interface SearchPurchaseOrdersParams {
  supplierId?: string
  status?: PurchaseOrderStatus
  poNumber?: string
  skip?: number
  take?: number
}

export interface CreatePurchaseOrderBody {
  supplierId: string
  currencyCode: string
  countryCode: string
  expectedDeliveryDate?: string
  notes?: string
}

export interface AddPoLineBody {
  catalogItemId: string
  sku: string
  itemName: string
  quantityOrdered: number
  unitCostAmount: number
  notes?: string
}

export interface ReceivePoLineBody {
  lineId: string
  warehouseId: string
  quantityReceived: number
}
