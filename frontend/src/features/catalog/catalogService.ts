import api from '@/services/api'
import type {
  CatalogItemDto,
  PriceListDto,
  SearchCatalogItemsParams,
  CreateCatalogItemBody,
  CreatePriceListBody,
} from '@/types/erp/catalog'

export const catalogService = {
  items: {
    search: (params: SearchCatalogItemsParams) =>
      api.get<CatalogItemDto[]>('/bff/catalog/items', { params }).then(r => r.data),

    create: (body: CreateCatalogItemBody) =>
      api.post<CatalogItemDto>('/bff/catalog/items', body).then(r => r.data),

    deactivate: (id: string) =>
      api.delete(`/bff/catalog/items/${id}`),
  },

  priceLists: {
    list: () =>
      api.get<PriceListDto[]>('/bff/catalog/pricelists').then(r => r.data),

    create: (body: CreatePriceListBody) =>
      api.post<PriceListDto>('/bff/catalog/pricelists', body).then(r => r.data),

    setDefault: (id: string) =>
      api.post(`/bff/catalog/pricelists/${id}/set-default`),
  },
}
