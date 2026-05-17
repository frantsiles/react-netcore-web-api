import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { EntityPicker, type PickerOption } from '@/components/ui/entity-picker'
import { accountingService } from '@/features/accounting/accountingService'

interface JournalEntryPickerProps {
  value: string
  onChange: (id: string) => void
  placeholder?: string
  disabled?: boolean
}

export function JournalEntryPicker({ value, onChange, placeholder, disabled }: JournalEntryPickerProps) {
  const { data, isLoading } = useQuery({
    queryKey: ['journal-entries-picker'],
    queryFn: () => accountingService.entries.search(undefined, 'Posted'),
    staleTime: 30_000,
  })

  const options: PickerOption[] = useMemo(
    () =>
      (data ?? []).map(e => ({
        id:       e.id,
        label:    `${e.entryNumber} — ${e.description}`,
        sublabel: `${e.fiscalPeriod} · ${e.totalDebits.toFixed(2)}`,
      })),
    [data],
  )

  return (
    <EntityPicker
      value={value}
      onChange={(id) => onChange(id)}
      options={options}
      isLoading={isLoading}
      placeholder={placeholder ?? 'Seleccionar asiento contable…'}
      searchPlaceholder="Buscar por número o descripción…"
      disabled={disabled}
    />
  )
}
