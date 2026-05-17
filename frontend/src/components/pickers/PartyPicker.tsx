import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { EntityPicker, type PickerOption } from '@/components/ui/entity-picker'
import { partiesService } from '@/features/parties/partiesService'
import type { PartyRoleType } from '@/types/erp/parties'

interface PartyPickerProps {
  value: string
  onChange: (id: string) => void
  /** Filter by active role — e.g. 'Customer' or 'Supplier' */
  roleType?: PartyRoleType
  placeholder?: string
  disabled?: boolean
}

export function PartyPicker({ value, onChange, roleType, placeholder, disabled }: PartyPickerProps) {
  const { data, isLoading } = useQuery({
    queryKey: ['parties-picker', roleType],
    queryFn: () => partiesService.search({ roleType, isActive: true, skip: 0, take: 200 }),
    staleTime: 60_000,
  })

  const options: PickerOption[] = useMemo(
    () =>
      (data ?? []).map(p => ({
        id:       p.id,
        label:    p.legalName,
        sublabel: [p.tradeName, p.taxId].filter(Boolean).join(' · ') || p.partyType,
      })),
    [data]
  )

  return (
    <EntityPicker
      value={value}
      onChange={id => onChange(id)}
      options={options}
      isLoading={isLoading}
      placeholder={placeholder ?? (roleType === 'Customer' ? 'Seleccionar cliente…' : roleType === 'Supplier' ? 'Seleccionar proveedor…' : 'Seleccionar empresa…')}
      searchPlaceholder="Buscar por nombre o RFC…"
      disabled={disabled}
    />
  )
}
