import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { EntityPicker, type PickerOption } from '@/components/ui/entity-picker'
import { catalogService } from '@/features/catalog/catalogService'

interface CatalogItemPickerProps {
  value: string
  onChange: (id: string, sku: string, name: string) => void
  placeholder?: string
  disabled?: boolean
}

export function CatalogItemPicker({ value, onChange, placeholder, disabled }: CatalogItemPickerProps) {
  const { data, isLoading } = useQuery({
    queryKey: ['catalog-items-picker'],
    queryFn: () => catalogService.items.search({ isActive: true, skip: 0, take: 300 }),
    staleTime: 60_000,
  })

  const options: PickerOption[] = useMemo(
    () =>
      (data ?? []).map(item => ({
        id:       item.catalogItemId,
        label:    item.name,
        sublabel: item.sku,
      })),
    [data]
  )

  const handleChange = (id: string, opt: PickerOption) => {
    const item = data?.find(i => i.catalogItemId === id)
    onChange(id, item?.sku ?? opt.sublabel ?? '', opt.label)
  }

  return (
    <EntityPicker
      value={value}
      onChange={handleChange}
      options={options}
      isLoading={isLoading}
      placeholder={placeholder ?? 'Seleccionar artículo…'}
      searchPlaceholder="Buscar por nombre o SKU…"
      disabled={disabled}
    />
  )
}
