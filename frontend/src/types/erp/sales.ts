export type QuoteStatus =
  | 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Expired' | 'ConvertedToOrder'

export type SalesOrderStatus =
  | 'Draft' | 'Confirmed' | 'PartiallyFulfilled' | 'Fulfilled' | 'Invoiced' | 'Cancelled'

export interface QuoteLineDto {
  id: string
  catalogItemId: string
  sku: string
  itemName: string
  quantity: number
  unitPrice: number
  currencyCode: string
  discountPercent: number
  lineTotal: number
  notes?: string
}

export interface QuoteDto {
  id: string
  quoteNumber: string
  customerId: string
  customerName: string
  status: QuoteStatus
  validUntil: string      // "YYYY-MM-DD"
  currencyCode: string
  countryCode: string
  lines: QuoteLineDto[]
  subtotal: number
  taxAmount: number
  total: number
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface SalesOrderLineDto {
  id: string
  catalogItemId: string
  sku: string
  itemName: string
  quantity: number
  unitPrice: number
  currencyCode: string
  discountPercent: number
  lineTotal: number
  fulfilledQuantity: number
  notes?: string
}

export interface SalesOrderDto {
  id: string
  orderNumber: string
  customerId: string
  customerName: string
  status: SalesOrderStatus
  orderDate: string       // "YYYY-MM-DD"
  requestedDeliveryDate?: string
  currencyCode: string
  countryCode: string
  originQuoteId?: string
  lines: SalesOrderLineDto[]
  subtotal: number
  taxAmount: number
  total: number
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface SearchQuotesParams {
  customerId?: string
  status?: QuoteStatus
  skip?: number
  take?: number
}

export interface SearchOrdersParams {
  customerId?: string
  status?: SalesOrderStatus
  skip?: number
  take?: number
}

export interface CreateQuoteBody {
  customerId: string
  validUntil: string
  currencyCode: string
  countryCode: string
  notes?: string
}

export interface AddQuoteLineBody {
  catalogItemId: string
  sku: string
  itemName: string
  quantity: number
  unitPrice: number
  currencyCode: string
  discountPercent?: number
  notes?: string
}

export interface ConvertToOrderBody {
  requestedDeliveryDate?: string
  notes?: string
}
