import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  useReactTable, getCoreRowModel, getSortedRowModel,
  flexRender, createColumnHelper, type SortingState,
} from '@tanstack/react-table'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { Plus, MoreHorizontal, ChevronUp, ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

import { taxService, type TaxRateDto, type TaxApplicability } from './taxService'

const APP_LABELS: Record<TaxApplicability, string> = { Sales: 'Ventas', Purchases: 'Compras', Both: 'Ambos' }

const schema = z.object({
  code:          z.string().min(1, 'Requerido'),
  name:          z.string().min(1, 'Requerido'),
  rate:          z.number().min(0).max(100),
  applicability: z.enum(['Sales', 'Purchases', 'Both']),
  description:   z.string().optional(),
})
type FormData = z.infer<typeof schema>

const col = createColumnHelper<TaxRateDto>()

export function TaxPage() {
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('all')
  const [sorting, setSorting] = useState<SortingState>([])
  const [isOpen, setIsOpen] = useState(false)

  const { data: rates, isLoading } = useQuery({
    queryKey: ['tax-rates', statusFilter],
    queryFn: () => taxService.rates.list(statusFilter !== 'all' ? statusFilter : undefined),
  })

  const createMut = useMutation({
    mutationFn: taxService.rates.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['tax-rates'] }); setIsOpen(false); toast.success('Tasa creada') },
    onError: () => toast.error('Error al crear tasa'),
  })

  const deactivateMut = useMutation({
    mutationFn: taxService.rates.deactivate,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['tax-rates'] }); toast.success('Tasa desactivada') },
    onError: () => toast.error('Error al desactivar'),
  })

  const form = useForm<FormData>({ resolver: zodResolver(schema), defaultValues: { rate: 16, applicability: 'Both' } })

  const columns = [
    col.accessor('code', { header: 'Código', cell: i => <span className="font-mono text-sm font-medium">{i.getValue()}</span> }),
    col.accessor('name', { header: 'Nombre' }),
    col.accessor('rate', { header: 'Tasa', cell: i => <span className="font-medium">{i.getValue()}%</span> }),
    col.accessor('applicability', { header: 'Aplica a', cell: i => <Badge variant="secondary">{APP_LABELS[i.getValue()]}</Badge> }),
    col.accessor('status', { header: 'Estado', cell: i => <Badge variant={i.getValue() === 'Active' ? 'success' : 'secondary'}>{i.getValue() === 'Active' ? 'Activa' : 'Inactiva'}</Badge> }),
    col.accessor('description', { header: 'Descripción', cell: i => <span className="text-sm text-muted-foreground">{i.getValue() ?? '—'}</span> }),
    col.display({
      id: 'actions', header: '',
      cell: ({ row }) => row.original.status === 'Active' ? (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8"><MoreHorizontal className="h-4 w-4" /></Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem className="text-destructive" onClick={() => deactivateMut.mutate(row.original.id)}>Desactivar</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ) : null,
    }),
  ]

  const table = useReactTable({ data: rates ?? [], columns, state: { sorting }, onSortingChange: setSorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Impuestos</h1>
          <p className="text-sm text-muted-foreground">Configuración de tasas impositivas</p>
        </div>
        <Button onClick={() => { form.reset({ rate: 16, applicability: 'Both' }); setIsOpen(true) }}>
          <Plus className="mr-2 h-4 w-4" />Nueva tasa
        </Button>
      </div>

      <div className="flex gap-3">
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[160px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todas</SelectItem>
            <SelectItem value="Active">Activas</SelectItem>
            <SelectItem value="Inactive">Inactivas</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map(hg => (
              <TableRow key={hg.id}>
                {hg.headers.map(h => (
                  <TableHead key={h.id} className={h.column.getCanSort() ? 'cursor-pointer select-none' : ''} onClick={h.column.getToggleSortingHandler()}>
                    <div className="flex items-center gap-1">
                      {flexRender(h.column.columnDef.header, h.getContext())}
                      {h.column.getIsSorted() === 'asc' && <ChevronUp className="h-3 w-3" />}
                      {h.column.getIsSorted() === 'desc' && <ChevronDown className="h-3 w-3" />}
                    </div>
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 3 }).map((_, i) => <TableRow key={i}>{columns.map((_, j) => <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>)}</TableRow>)
              : table.getRowModel().rows.length === 0
                ? <TableRow><TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">No hay tasas registradas</TableCell></TableRow>
                : table.getRowModel().rows.map(row => (
                    <TableRow key={row.id}>
                      {row.getVisibleCells().map(cell => <TableCell key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</TableCell>)}
                    </TableRow>
                  ))
            }
          </TableBody>
        </Table>
      </div>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Nueva Tasa de Impuesto</DialogTitle></DialogHeader>
          <form onSubmit={form.handleSubmit(v => createMut.mutate({ ...v, description: v.description || undefined }))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Código *</Label>
                <Input {...form.register('code')} placeholder="IVA-16" />
                {form.formState.errors.code && <p className="text-xs text-destructive">{form.formState.errors.code.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Nombre *</Label>
                <Input {...form.register('name')} placeholder="IVA 16%" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Tasa % *</Label>
                <Input type="number" step="0.01" {...form.register('rate', { valueAsNumber: true })} />
              </div>
              <div className="space-y-1.5">
                <Label>Aplica a *</Label>
                <Select onValueChange={v => form.setValue('applicability', v as 'Sales' | 'Purchases' | 'Both')} defaultValue="Both">
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Both">Ambos</SelectItem>
                    <SelectItem value="Sales">Ventas</SelectItem>
                    <SelectItem value="Purchases">Compras</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Descripción</Label>
              <Input {...form.register('description')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createMut.isPending}>{createMut.isPending ? 'Creando…' : 'Crear'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
