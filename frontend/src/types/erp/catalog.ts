export type ItemType = 'Product' | 'Service'

export interface CatalogItemDto {
  catalogItemId: string
  sku: string
  name: string
  description?: string
  itemType: ItemType
  unitOfMeasure: string
  taxCategory: string
  defaultCurrency: string
  countryCode: string
  isActive: boolean
  trackInventory: boolean
  reorderPoint?: number
}

export interface PriceListDto {
  priceListId: string
  name: string
  currencyCode: string
  validFrom: string   // ISO date "YYYY-MM-DD"
  validTo?: string
  isDefault: boolean
  status: string
}

export interface SearchCatalogItemsParams {
  name?: string
  sku?: string
  itemType?: ItemType
  isActive?: boolean
  skip?: number
  take?: number
}

export interface CreateCatalogItemBody {
  sku: string
  name: string
  description?: string
  itemType: ItemType
  unitOfMeasure: string
  taxCategory: string
  defaultCurrency: string
  countryCode: string
  trackInventory?: boolean
  reorderPoint?: number
}

export interface CreatePriceListBody {
  name: string
  currencyCode: string
  validFrom: string   // "YYYY-MM-DD"
  validTo?: string
}
