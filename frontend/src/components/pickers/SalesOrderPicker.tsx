import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { EntityPicker, type PickerOption } from '@/components/ui/entity-picker'
import { salesService } from '@/features/sales/salesService'
import type { SalesOrderDto } from '@/types/erp/sales'

interface SalesOrderPickerProps {
  value: string
  onChange: (id: string, order?: SalesOrderDto) => void
  placeholder?: string
  disabled?: boolean
}

const INVOICEABLE: SalesOrderDto['status'][] = ['Confirmed', 'PartiallyFulfilled', 'Fulfilled']

export function SalesOrderPicker({ value, onChange, placeholder, disabled }: SalesOrderPickerProps) {
  const { data, isLoading } = useQuery({
    queryKey: ['orders-picker'],
    queryFn: () => salesService.orders.search({ take: 500 }),
    staleTime: 30_000,
  })

  const eligible = useMemo(
    () => (data ?? []).filter(o => INVOICEABLE.includes(o.status)),
    [data]
  )

  const options: PickerOption[] = useMemo(
    () =>
      eligible.map(o => ({
        id:       o.id,
        label:    `${o.orderNumber} — ${o.currencyCode} ${o.total.toFixed(2)}`,
        sublabel: o.status,
      })),
    [eligible]
  )

  return (
    <EntityPicker
      value={value}
      onChange={(id) => onChange(id, eligible.find(o => o.id === id))}
      options={options}
      isLoading={isLoading}
      placeholder={placeholder ?? 'Seleccionar orden de venta…'}
      searchPlaceholder="Buscar por número de orden…"
      disabled={disabled}
    />
  )
}
