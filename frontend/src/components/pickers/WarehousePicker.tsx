import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { EntityPicker, type PickerOption } from '@/components/ui/entity-picker'
import { inventoryService } from '@/features/inventory/inventoryService'

interface WarehousePickerProps {
  value: string
  onChange: (id: string) => void
  placeholder?: string
  disabled?: boolean
}

export function WarehousePicker({ value, onChange, placeholder, disabled }: WarehousePickerProps) {
  const { data, isLoading } = useQuery({
    queryKey: ['warehouses-picker'],
    queryFn: inventoryService.warehouses.list,
    staleTime: 60_000,
  })

  const options: PickerOption[] = useMemo(
    () =>
      (data ?? [])
        .filter(w => w.status === 'Active')
        .map(w => ({
          id:       w.id,
          label:    w.name,
          sublabel: w.code,
        })),
    [data]
  )

  return (
    <EntityPicker
      value={value}
      onChange={id => onChange(id)}
      options={options}
      isLoading={isLoading}
      placeholder={placeholder ?? 'Seleccionar almacén…'}
      searchPlaceholder="Buscar almacén…"
      disabled={disabled}
    />
  )
}
