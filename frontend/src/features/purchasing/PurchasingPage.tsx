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
import {
  Plus, MoreHorizontal, PackageCheck,
  ShoppingBag, ChevronUp, ChevronDown, FileDown,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

import api from '@/services/api'
import { purchasingService } from './purchasingService'
import type { PurchaseOrderDto, PurchaseOrderStatus } from '@/types/erp/purchasing'
import { PartyPicker } from '@/components/pickers/PartyPicker'
import { CatalogItemPicker } from '@/components/pickers/CatalogItemPicker'
import { WarehousePicker } from '@/components/pickers/WarehousePicker'

// ── Formatters ────────────────────────────────────────────────────────────────

const fmt = (n: number, currency = 'USD') =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency }).format(n)

const STATUS_LABELS: Record<PurchaseOrderStatus, string> = {
  Draft: 'Borrador',
  Sent: 'Enviada',
  Confirmed: 'Confirmada',
  PartiallyReceived: 'Parcial',
  Received: 'Recibida',
  Cancelled: 'Cancelada',
}

type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success'

const STATUS_VARIANT: Record<PurchaseOrderStatus, BadgeVariant> = {
  Draft: 'secondary',
  Sent: 'default',
  Confirmed: 'default',
  PartiallyReceived: 'default',
  Received: 'success',
  Cancelled: 'destructive',
}

// ── Schemas ───────────────────────────────────────────────────────────────────

const createSchema = z.object({
  supplierId:           z.string().uuid('UUID requerido'),
  currencyCode:         z.string().length(3, '3 letras'),
  countryCode:          z.string().length(2, '2 letras'),
  expectedDeliveryDate: z.string().optional(),
  notes:                z.string().optional(),
})
type CreateForm = z.infer<typeof createSchema>

const lineSchema = z.object({
  catalogItemId:  z.string().uuid('UUID requerido'),
  sku:            z.string().min(1, 'Requerido'),
  itemName:       z.string().min(1, 'Requerido'),
  quantityOrdered: z.number().positive('> 0'),
  unitCostAmount:  z.number().min(0),
  notes:           z.string().optional(),
})
type LineForm = z.infer<typeof lineSchema>

const receiveSchema = z.object({
  lineId:           z.string().uuid('UUID requerido'),
  warehouseId:      z.string().uuid('UUID requerido'),
  quantityReceived: z.number().positive('> 0'),
})
type ReceiveForm = z.infer<typeof receiveSchema>

const cancelSchema = z.object({
  reason: z.string().min(3, 'Mínimo 3 caracteres'),
})
type CancelForm = z.infer<typeof cancelSchema>

// ── Column helper ─────────────────────────────────────────────────────────────

const col = createColumnHelper<PurchaseOrderDto>()

// ── Page ──────────────────────────────────────────────────────────────────────

export function PurchasingPage() {
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('all')
  const [sorting, setSorting] = useState<SortingState>([])

  const [isCreateOpen,  setIsCreateOpen]  = useState(false)
  const [isLineOpen,    setIsLineOpen]    = useState(false)
  const [isReceiveOpen, setIsReceiveOpen] = useState(false)
  const [isCancelOpen,  setIsCancelOpen]  = useState(false)
  const [selectedId,    setSelectedId]    = useState<string | null>(null)
  const [selectedOrder, setSelectedOrder] = useState<PurchaseOrderDto | null>(null)

  // ── Query ──────────────────────────────────────────────────────────────────

  const { data: orders, isLoading } = useQuery({
    queryKey: ['purchase-orders', statusFilter],
    queryFn: () => purchasingService.orders.search({
      status: statusFilter !== 'all' ? (statusFilter as PurchaseOrderStatus) : undefined,
      skip: 0, take: 100,
    }),
  })

  // ── Mutations ──────────────────────────────────────────────────────────────

  const invalidate = () => qc.invalidateQueries({ queryKey: ['purchase-orders'] })

  const createMut = useMutation({
    mutationFn: purchasingService.orders.create,
    onSuccess: () => { invalidate(); setIsCreateOpen(false); toast.success('Orden creada') },
    onError:   () => toast.error('Error al crear la orden'),
  })

  const addLineMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: LineForm }) =>
      purchasingService.orders.addLine(id, body),
    onSuccess: () => { invalidate(); setIsLineOpen(false); toast.success('Línea agregada') },
    onError:   () => toast.error('Error al agregar línea'),
  })

  const sendMut = useMutation({
    mutationFn: (id: string) => purchasingService.orders.send(id),
    onSuccess: () => { invalidate(); toast.success('Orden enviada al proveedor') },
    onError:   () => toast.error('Error al enviar'),
  })

  const confirmMut = useMutation({
    mutationFn: (id: string) => purchasingService.orders.confirm(id),
    onSuccess: () => { invalidate(); toast.success('Orden confirmada') },
    onError:   () => toast.error('Error al confirmar'),
  })

  const receiveMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: ReceiveForm }) =>
      purchasingService.orders.receive(id, body),
    onSuccess: () => { invalidate(); setIsReceiveOpen(false); toast.success('Recepción registrada') },
    onError:   () => toast.error('Error al registrar recepción'),
  })

  const cancelMut = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      purchasingService.orders.cancel(id, reason),
    onSuccess: () => { invalidate(); setIsCancelOpen(false); toast.success('Orden cancelada') },
    onError:   () => toast.error('Error al cancelar'),
  })

  // ── Forms ──────────────────────────────────────────────────────────────────

  const createForm  = useForm<CreateForm>({  resolver: zodResolver(createSchema),  defaultValues: { currencyCode: 'USD', countryCode: 'MX' } })
  const lineForm    = useForm<LineForm>({    resolver: zodResolver(lineSchema),     defaultValues: { quantityOrdered: 1, unitCostAmount: 0 } })
  const receiveForm = useForm<ReceiveForm>({ resolver: zodResolver(receiveSchema), defaultValues: { quantityReceived: 1 } })
  const cancelForm  = useForm<CancelForm>({  resolver: zodResolver(cancelSchema) })

  // ── Helpers ────────────────────────────────────────────────────────────────

  const openLineDialog = (id: string) => {
    setSelectedId(id)
    lineForm.reset({ quantityOrdered: 1, unitCostAmount: 0 })
    setIsLineOpen(true)
  }
  const openReceiveDialog = (order: PurchaseOrderDto) => {
    setSelectedId(order.id)
    setSelectedOrder(order)
    receiveForm.reset({ quantityReceived: 1 })
    setIsReceiveOpen(true)
  }
  const openCancelDialog = (id: string) => {
    setSelectedId(id)
    cancelForm.reset()
    setIsCancelOpen(true)
  }

  const downloadPdf = async (path: string, filename: string) => {
    const resp = await api.get(path, { responseType: 'blob' })
    const url = URL.createObjectURL(resp.data)
    const a = document.createElement('a')
    a.href = url; a.download = filename; a.click()
    URL.revokeObjectURL(url)
  }

  // ── Columns ────────────────────────────────────────────────────────────────

  const columns = [
    col.accessor('poNumber', {
      header: 'N° Orden',
      cell: i => <span className="font-mono text-sm font-medium">{i.getValue()}</span>,
    }),
    col.accessor('supplierName', {
      header: 'Proveedor',
      cell: i => <span className="text-sm">{i.getValue()}</span>,
    }),
    col.accessor('status', {
      header: 'Estado',
      cell: i => <Badge variant={STATUS_VARIANT[i.getValue()]}>{STATUS_LABELS[i.getValue()]}</Badge>,
    }),
    col.accessor('expectedDeliveryDate', {
      header: 'Entrega esperada',
      cell: i => i.getValue()
        ? <span className="text-sm">{new Date(i.getValue()!).toLocaleDateString('es-MX')}</span>
        : <span className="text-muted-foreground">—</span>,
    }),
    col.display({
      id: 'lines',
      header: 'Líneas',
      cell: ({ row }) => <span className="text-sm text-muted-foreground">{row.original.lines.length}</span>,
    }),
    col.accessor('total', {
      header: 'Total',
      cell: i => <span className="font-medium">{fmt(i.getValue(), i.row.original.currencyCode)}</span>,
    }),
    col.display({
      id: 'actions',
      header: '',
      cell: ({ row }) => {
        const o = row.original
        const canReceive = o.status === 'Confirmed' || o.status === 'PartiallyReceived'
        if (!['Draft', 'Sent', 'Confirmed', 'PartiallyReceived'].includes(o.status)) return null
        return (
          <div className="flex items-center gap-2">
            {canReceive && (
              <Button size="sm" onClick={() => openReceiveDialog(o)} className="h-7 gap-1 text-xs">
                <PackageCheck className="h-3.5 w-3.5" />
                Recibir
              </Button>
            )}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" className="h-8 w-8">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {o.status === 'Draft' && <>
                  <DropdownMenuItem onClick={() => openLineDialog(o.id)}>Agregar línea</DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => sendMut.mutate(o.id)}>Enviar a proveedor</DropdownMenuItem>
                </>}
                {o.status === 'Sent' && (
                  <DropdownMenuItem onClick={() => confirmMut.mutate(o.id)}>Confirmar</DropdownMenuItem>
                )}
                {(o.status === 'Confirmed' || o.status === 'Draft') && (
                  <DropdownMenuItem className="text-destructive" onClick={() => openCancelDialog(o.id)}>
                    Cancelar
                  </DropdownMenuItem>
                )}
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => downloadPdf(`/bff/purchasing/orders/${o.id}/pdf`, `OC-${o.poNumber}.pdf`)}>
                  <FileDown className="mr-2 h-4 w-4" />Descargar PDF
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        )
      },
    }),
  ]

  const table = useReactTable({
    data: orders ?? [], columns,
    state: { sorting }, onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel(),
  })

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Compras</h1>
          <p className="text-sm text-muted-foreground">Órdenes de compra a proveedores</p>
        </div>
        <Button onClick={() => { createForm.reset({ currencyCode: 'USD', countryCode: 'MX' }); setIsCreateOpen(true) }}>
          <Plus className="mr-2 h-4 w-4" />
          Nueva orden
        </Button>
      </div>

      {/* Filters */}
      <div className="flex gap-3">
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="Estado" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los estados</SelectItem>
            <SelectItem value="Draft">Borrador</SelectItem>
            <SelectItem value="Sent">Enviadas</SelectItem>
            <SelectItem value="Confirmed">Confirmadas</SelectItem>
            <SelectItem value="PartiallyReceived">Parcialmente recibidas</SelectItem>
            <SelectItem value="Received">Recibidas</SelectItem>
            <SelectItem value="Cancelled">Canceladas</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <div className="rounded-md border overflow-x-auto">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map(hg => (
              <TableRow key={hg.id}>
                {hg.headers.map(h => (
                  <TableHead key={h.id}
                    className={h.column.getCanSort() ? 'cursor-pointer select-none' : ''}
                    onClick={h.column.getToggleSortingHandler()}>
                    <div className="flex items-center gap-1">
                      {flexRender(h.column.columnDef.header, h.getContext())}
                      {h.column.getIsSorted() === 'asc'  && <ChevronUp   className="h-3 w-3" />}
                      {h.column.getIsSorted() === 'desc' && <ChevronDown className="h-3 w-3" />}
                    </div>
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 4 }).map((_, i) => (
                <TableRow key={i}>
                  {columns.map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                  <ShoppingBag className="mx-auto mb-2 h-8 w-8 opacity-30" />
                  No se encontraron órdenes de compra
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map(row => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map(cell => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* ── Create Order Dialog ── */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Nueva Orden de Compra</DialogTitle></DialogHeader>
          <form onSubmit={createForm.handleSubmit(v => createMut.mutate({
            ...v,
            expectedDeliveryDate: v.expectedDeliveryDate || undefined,
            notes: v.notes || undefined,
          }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Proveedor *</Label>
              <PartyPicker
                roleType="Supplier"
                value={createForm.watch('supplierId') ?? ''}
                onChange={id => createForm.setValue('supplierId', id, { shouldValidate: true })}
              />
              {createForm.formState.errors.supplierId && <p className="text-xs text-destructive">{createForm.formState.errors.supplierId.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="po-currency">Moneda *</Label>
                <Input id="po-currency" {...createForm.register('currencyCode')} placeholder="USD" maxLength={3} className="uppercase" />
                {createForm.formState.errors.currencyCode && <p className="text-xs text-destructive">{createForm.formState.errors.currencyCode.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="po-country">País *</Label>
                <Input id="po-country" {...createForm.register('countryCode')} placeholder="MX" maxLength={2} className="uppercase" />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="po-delivery">Fecha de entrega esperada</Label>
              <Input id="po-delivery" type="date" {...createForm.register('expectedDeliveryDate')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="po-notes">Notas</Label>
              <Input id="po-notes" {...createForm.register('notes')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsCreateOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createMut.isPending}>{createMut.isPending ? 'Creando…' : 'Crear'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Add Line Dialog ── */}
      <Dialog open={isLineOpen} onOpenChange={setIsLineOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Agregar Línea</DialogTitle></DialogHeader>
          <form onSubmit={lineForm.handleSubmit(v => addLineMut.mutate({ id: selectedId!, body: v }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Artículo *</Label>
              <CatalogItemPicker
                value={lineForm.watch('catalogItemId') ?? ''}
                onChange={(id, sku, name) => {
                  lineForm.setValue('catalogItemId', id, { shouldValidate: true })
                  lineForm.setValue('sku', sku)
                  lineForm.setValue('itemName', name)
                }}
              />
              {lineForm.formState.errors.catalogItemId && <p className="text-xs text-destructive">{lineForm.formState.errors.catalogItemId.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="l-qty">Cantidad *</Label>
                <Input id="l-qty" type="number" step="0.01" {...lineForm.register('quantityOrdered', { valueAsNumber: true })} />
                {lineForm.formState.errors.quantityOrdered && <p className="text-xs text-destructive">{lineForm.formState.errors.quantityOrdered.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="l-cost">Costo unitario *</Label>
                <Input id="l-cost" type="number" step="0.01" {...lineForm.register('unitCostAmount', { valueAsNumber: true })} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="l-notes">Notas</Label>
              <Input id="l-notes" {...lineForm.register('notes')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsLineOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={addLineMut.isPending}>{addLineMut.isPending ? 'Guardando…' : 'Agregar'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Receive Dialog ── */}
      <Dialog open={isReceiveOpen} onOpenChange={setIsReceiveOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader><DialogTitle>Registrar Recepción</DialogTitle></DialogHeader>
          <form onSubmit={receiveForm.handleSubmit(v => receiveMut.mutate({ id: selectedId!, body: v }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Línea *</Label>
              <Select onValueChange={v => receiveForm.setValue('lineId', v, { shouldValidate: true })}>
                <SelectTrigger>
                  <SelectValue placeholder="Seleccionar línea…" />
                </SelectTrigger>
                <SelectContent>
                  {(selectedOrder?.lines ?? []).map(line => (
                    <SelectItem key={line.id} value={line.id}>
                      {line.sku} — {line.itemName} (pedido: {line.quantityOrdered}, recibido: {line.quantityReceived})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {receiveForm.formState.errors.lineId && <p className="text-xs text-destructive">{receiveForm.formState.errors.lineId.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Almacén destino *</Label>
              <WarehousePicker
                value={receiveForm.watch('warehouseId') ?? ''}
                onChange={id => receiveForm.setValue('warehouseId', id, { shouldValidate: true })}
              />
              {receiveForm.formState.errors.warehouseId && <p className="text-xs text-destructive">{receiveForm.formState.errors.warehouseId.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="r-qty">Cantidad recibida *</Label>
              <Input id="r-qty" type="number" step="0.01" {...receiveForm.register('quantityReceived', { valueAsNumber: true })} />
              {receiveForm.formState.errors.quantityReceived && <p className="text-xs text-destructive">{receiveForm.formState.errors.quantityReceived.message}</p>}
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsReceiveOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={receiveMut.isPending}>
                {receiveMut.isPending ? 'Registrando…' : 'Registrar recepción'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Cancel Dialog ── */}
      <Dialog open={isCancelOpen} onOpenChange={setIsCancelOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader><DialogTitle>Cancelar Orden</DialogTitle></DialogHeader>
          <form onSubmit={cancelForm.handleSubmit(v => cancelMut.mutate({ id: selectedId!, reason: v.reason }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="cancel-reason">Motivo *</Label>
              <Input id="cancel-reason" {...cancelForm.register('reason')} placeholder="Describe el motivo de cancelación" />
              {cancelForm.formState.errors.reason && <p className="text-xs text-destructive">{cancelForm.formState.errors.reason.message}</p>}
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsCancelOpen(false)}>Volver</Button>
              <Button type="submit" variant="destructive" disabled={cancelMut.isPending}>
                {cancelMut.isPending ? 'Cancelando…' : 'Cancelar Orden'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
