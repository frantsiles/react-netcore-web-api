import { useState, useMemo } from 'react'
import { ChevronsUpDown, Check, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

export interface PickerOption {
  id: string
  label: string
  sublabel?: string
}

interface EntityPickerProps {
  value: string
  onChange: (id: string, option: PickerOption) => void
  options: PickerOption[]
  isLoading?: boolean
  placeholder?: string
  searchPlaceholder?: string
  disabled?: boolean
}

export function EntityPicker({
  value,
  onChange,
  options,
  isLoading = false,
  placeholder = 'Seleccionar…',
  searchPlaceholder = 'Buscar…',
  disabled = false,
}: EntityPickerProps) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')

  const filtered = useMemo(() => {
    const q = query.toLowerCase()
    return q
      ? options.filter(o =>
          o.label.toLowerCase().includes(q) ||
          (o.sublabel?.toLowerCase().includes(q) ?? false)
        )
      : options
  }, [options, query])

  const selected = options.find(o => o.id === value)

  return (
    <Popover open={open} onOpenChange={o => { setOpen(o); if (!o) setQuery('') }}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          disabled={disabled || isLoading}
          className="w-full justify-between font-normal"
        >
          {isLoading ? (
            <Skeleton className="h-4 w-32" />
          ) : selected ? (
            <span className="truncate">{selected.label}</span>
          ) : (
            <span className="text-muted-foreground">{placeholder}</span>
          )}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[var(--radix-popover-trigger-width)] p-0">
        {/* Search */}
        <div className="flex items-center border-b px-3 py-2 gap-2">
          <Search className="h-4 w-4 text-muted-foreground shrink-0" />
          <Input
            value={query}
            onChange={e => setQuery(e.target.value)}
            placeholder={searchPlaceholder}
            className="h-7 border-0 p-0 text-sm shadow-none focus-visible:ring-0"
          />
        </div>
        {/* Options list */}
        <ul className="max-h-60 overflow-y-auto py-1">
          {filtered.length === 0 ? (
            <li className="px-3 py-4 text-center text-sm text-muted-foreground">
              Sin resultados
            </li>
          ) : (
            filtered.map(opt => (
              <li key={opt.id}>
                <button
                  type="button"
                  className={cn(
                    'flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent',
                    value === opt.id && 'bg-accent'
                  )}
                  onClick={() => { onChange(opt.id, opt); setOpen(false); setQuery('') }}
                >
                  <Check className={cn('h-4 w-4 shrink-0', value === opt.id ? 'opacity-100' : 'opacity-0')} />
                  <div className="flex flex-col items-start min-w-0">
                    <span className="truncate font-medium">{opt.label}</span>
                    {opt.sublabel && (
                      <span className="truncate text-xs text-muted-foreground">{opt.sublabel}</span>
                    )}
                  </div>
                </button>
              </li>
            ))
          )}
        </ul>
      </PopoverContent>
    </Popover>
  )
}
