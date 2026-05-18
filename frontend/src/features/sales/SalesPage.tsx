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
  Plus, MoreHorizontal, ArrowRightCircle,
  ShoppingCart, FileText, ChevronUp, ChevronDown, FileDown,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

import api from '@/services/api'
import { salesService } from './salesService'
import type { QuoteDto, SalesOrderDto, QuoteStatus, SalesOrderStatus } from '@/types/erp/sales'
import { PartyPicker } from '@/components/pickers/PartyPicker'
import { CatalogItemPicker } from '@/components/pickers/CatalogItemPicker'

// ── Formatters ────────────────────────────────────────────────────────────────

const fmt = (n: number, currency = 'USD') =>
  new Intl.NumberFormat('es-MX', { style: 'currency', currency }).format(n)

const QUOTE_STATUS_LABELS: Record<QuoteStatus, string> = {
  Draft: 'Borrador', Sent: 'Enviada', Accepted: 'Aceptada',
  Rejected: 'Rechazada', Expired: 'Expirada', ConvertedToOrder: 'Convertida',
}
const ORDER_STATUS_LABELS: Record<SalesOrderStatus, string> = {
  Draft: 'Borrador', Confirmed: 'Confirmada', PartiallyFulfilled: 'Parcial',
  Fulfilled: 'Surtida', Invoiced: 'Facturada', Cancelled: 'Cancelada',
}

type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success'

const QUOTE_STATUS_VARIANT: Record<QuoteStatus, BadgeVariant> = {
  Draft: 'secondary', Sent: 'default', Accepted: 'success',
  Rejected: 'destructive', Expired: 'outline', ConvertedToOrder: 'outline',
}
const ORDER_STATUS_VARIANT: Record<SalesOrderStatus, BadgeVariant> = {
  Draft: 'secondary', Confirmed: 'default', PartiallyFulfilled: 'default',
  Fulfilled: 'success', Invoiced: 'outline', Cancelled: 'destructive',
}

// ── Schemas ───────────────────────────────────────────────────────────────────

const quoteSchema = z.object({
  customerId:   z.string().uuid('UUID requerido'),
  validUntil:   z.string().min(1, 'Requerido'),
  currencyCode: z.string().length(3, '3 letras'),
  countryCode:  z.string().length(2, '2 letras'),
  notes:        z.string().optional(),
})
type QuoteForm = z.infer<typeof quoteSchema>

const lineSchema = z.object({
  catalogItemId:   z.string().uuid('UUID requerido'),
  sku:             z.string().min(1, 'Requerido'),
  itemName:        z.string().min(1, 'Requerido'),
  quantity:        z.number().positive('> 0'),
  unitPrice:       z.number().min(0),
  currencyCode:    z.string().length(3, '3 letras'),
  discountPercent: z.number().min(0).max(100),
  notes:           z.string().optional(),
})
type LineForm = z.infer<typeof lineSchema>

const convertSchema = z.object({
  requestedDeliveryDate: z.string().optional(),
  notes:                 z.string().optional(),
})
type ConvertForm = z.infer<typeof convertSchema>

const cancelSchema = z.object({
  reason: z.string().min(3, 'Mínimo 3 caracteres'),
})
type CancelForm = z.infer<typeof cancelSchema>

// ── Column helpers ────────────────────────────────────────────────────────────

const qCol = createColumnHelper<QuoteDto>()
const oCol = createColumnHelper<SalesOrderDto>()

// ── Page ──────────────────────────────────────────────────────────────────────

export function SalesPage() {
  const qc = useQueryClient()
  const [activeTab, setActiveTab] = useState('quotes')

  // Filter state
  const [quoteStatusFilter, setQuoteStatusFilter] = useState('all')
  const [orderStatusFilter, setOrderStatusFilter] = useState('all')

  // Dialog state + context
  const [isQuoteDialogOpen,   setIsQuoteDialogOpen]   = useState(false)
  const [isLineDialogOpen,    setIsLineDialogOpen]     = useState(false)
  const [isConvertDialogOpen, setIsConvertDialogOpen]  = useState(false)
  const [isCancelDialogOpen,  setIsCancelDialogOpen]   = useState(false)
  const [selectedQuoteId,     setSelectedQuoteId]      = useState<string | null>(null)
  const [selectedOrderId,     setSelectedOrderId]      = useState<string | null>(null)
  const [quoteSorting,        setQuoteSorting]         = useState<SortingState>([])
  const [orderSorting,        setOrderSorting]         = useState<SortingState>([])

  // ── Queries ────────────────────────────────────────────────────────────────

  const { data: quotes, isLoading: quotesLoading } = useQuery({
    queryKey: ['quotes', quoteStatusFilter],
    queryFn: () => salesService.quotes.search({
      status: quoteStatusFilter !== 'all' ? (quoteStatusFilter as QuoteStatus) : undefined,
      skip: 0, take: 100,
    }),
  })

  const { data: orders, isLoading: ordersLoading } = useQuery({
    queryKey: ['orders', orderStatusFilter],
    queryFn: () => salesService.orders.search({
      status: orderStatusFilter !== 'all' ? (orderStatusFilter as SalesOrderStatus) : undefined,
      skip: 0, take: 100,
    }),
  })

  // ── Mutations ──────────────────────────────────────────────────────────────

  const createQuoteMut = useMutation({
    mutationFn: salesService.quotes.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['quotes'] }); setIsQuoteDialogOpen(false); toast.success('Cotización creada') },
    onError:   () => toast.error('Error al crear cotización'),
  })

  const addLineMut = useMutation({
    mutationFn: ({ quoteId, body }: { quoteId: string; body: LineForm }) =>
      salesService.quotes.addLine(quoteId, { ...body, discountPercent: body.discountPercent ?? 0 }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['quotes'] }); setIsLineDialogOpen(false); toast.success('Línea agregada') },
    onError:   () => toast.error('Error al agregar línea'),
  })

  const sendMut = useMutation({
    mutationFn: (id: string) => salesService.quotes.send(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['quotes'] }); toast.success('Cotización enviada') },
    onError:   () => toast.error('Error al enviar'),
  })

  const acceptMut = useMutation({
    mutationFn: (id: string) => salesService.quotes.accept(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['quotes'] }); toast.success('Cotización aceptada') },
    onError:   () => toast.error('Error al aceptar'),
  })

  const rejectMut = useMutation({
    mutationFn: (id: string) => salesService.quotes.reject(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['quotes'] }); toast.success('Cotización rechazada') },
    onError:   () => toast.error('Error al rechazar'),
  })

  const convertMut = useMutation({
    mutationFn: ({ quoteId, body }: { quoteId: string; body: ConvertForm }) =>
      salesService.quotes.convertToOrder(quoteId, {
        requestedDeliveryDate: body.requestedDeliveryDate || undefined,
        notes: body.notes || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['quotes'] })
      qc.invalidateQueries({ queryKey: ['orders'] })
      setIsConvertDialogOpen(false)
      toast.success('Orden de venta creada')
    },
    onError: () => toast.error('Error al convertir'),
  })

  const confirmMut = useMutation({
    mutationFn: (id: string) => salesService.orders.confirm(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['orders'] }); toast.success('Orden confirmada') },
    onError:   () => toast.error('Error al confirmar'),
  })

  const cancelMut = useMutation({
    mutationFn: ({ orderId, reason }: { orderId: string; reason: string }) =>
      salesService.orders.cancel(orderId, reason),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['orders'] }); setIsCancelDialogOpen(false); toast.success('Orden cancelada') },
    onError:   () => toast.error('Error al cancelar'),
  })

  // ── Forms ──────────────────────────────────────────────────────────────────

  const quoteForm   = useForm<QuoteForm>({   resolver: zodResolver(quoteSchema),   defaultValues: { currencyCode: 'USD', countryCode: 'MX', validUntil: '' } })
  const lineForm    = useForm<LineForm>({    resolver: zodResolver(lineSchema),     defaultValues: { currencyCode: 'USD', discountPercent: 0, quantity: 1, unitPrice: 0 } })
  const convertForm = useForm<ConvertForm>({ resolver: zodResolver(convertSchema) })
  const cancelForm  = useForm<CancelForm>({  resolver: zodResolver(cancelSchema) })

  // ── Helpers ────────────────────────────────────────────────────────────────

  const downloadPdf = async (path: string, filename: string) => {
    const resp = await api.get(path, { responseType: 'blob' })
    const url = URL.createObjectURL(resp.data)
    const a = document.createElement('a')
    a.href = url; a.download = filename; a.click()
    URL.revokeObjectURL(url)
  }

  const openLineDialog = (quoteId: string) => {
    setSelectedQuoteId(quoteId)
    lineForm.reset({ currencyCode: 'USD', discountPercent: 0, quantity: 1, unitPrice: 0 })
    setIsLineDialogOpen(true)
  }
  const openConvertDialog = (quoteId: string) => {
    setSelectedQuoteId(quoteId)
    convertForm.reset()
    setIsConvertDialogOpen(true)
  }
  const openCancelDialog = (orderId: string) => {
    setSelectedOrderId(orderId)
    cancelForm.reset()
    setIsCancelDialogOpen(true)
  }

  // ── Columns ────────────────────────────────────────────────────────────────

  const quoteColumns = [
    qCol.accessor('quoteNumber', {
      header: 'Número',
      cell: i => <span className="font-mono text-sm font-medium">{i.getValue()}</span>,
    }),
    qCol.accessor('customerName', {
      header: 'Cliente',
      cell: i => <span className="text-sm">{i.getValue()}</span>,
    }),
    qCol.accessor('status', {
      header: 'Estado',
      cell: i => <Badge variant={QUOTE_STATUS_VARIANT[i.getValue()]}>{QUOTE_STATUS_LABELS[i.getValue()]}</Badge>,
    }),
    qCol.accessor('validUntil', {
      header: 'Válida hasta',
      cell: i => <span className="text-sm">{i.getValue()}</span>,
    }),
    qCol.display({
      id: 'lines',
      header: 'Líneas',
      cell: ({ row }) => <span className="text-sm text-muted-foreground">{row.original.lines.length}</span>,
    }),
    qCol.accessor('total', {
      header: 'Total',
      cell: i => <span className="font-medium">{fmt(i.getValue(), i.row.original.currencyCode)}</span>,
    }),
    qCol.display({
      id: 'actions',
      header: '',
      cell: ({ row }) => {
        const q = row.original
        return (
          <div className="flex items-center gap-2">
            {q.status === 'Accepted' && (
              <Button size="sm" onClick={() => openConvertDialog(q.id)}
                className="h-7 gap-1 text-xs">
                <ArrowRightCircle className="h-3.5 w-3.5" />
                Convertir en Orden
              </Button>
            )}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" className="h-8 w-8">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {q.status === 'Draft' && <>
                  <DropdownMenuItem onClick={() => openLineDialog(q.id)}>Agregar línea</DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => sendMut.mutate(q.id)}>Enviar</DropdownMenuItem>
                </>}
                {q.status === 'Sent' && <>
                  <DropdownMenuItem onClick={() => acceptMut.mutate(q.id)}>Aceptar</DropdownMenuItem>
                  <DropdownMenuItem className="text-destructive" onClick={() => rejectMut.mutate(q.id)}>Rechazar</DropdownMenuItem>
                </>}
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => downloadPdf(`/bff/sales/quotes/${q.id}/pdf`, `COT-${q.quoteNumber}.pdf`)}>
                  <FileDown className="mr-2 h-4 w-4" />Descargar PDF
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        )
      },
    }),
  ]

  const orderColumns = [
    oCol.accessor('orderNumber', {
      header: 'Número',
      cell: i => <span className="font-mono text-sm font-medium">{i.getValue()}</span>,
    }),
    oCol.accessor('customerName', {
      header: 'Cliente',
      cell: i => <span className="text-sm">{i.getValue()}</span>,
    }),
    oCol.accessor('status', {
      header: 'Estado',
      cell: i => <Badge variant={ORDER_STATUS_VARIANT[i.getValue()]}>{ORDER_STATUS_LABELS[i.getValue()]}</Badge>,
    }),
    oCol.accessor('orderDate', {
      header: 'Fecha',
      cell: i => <span className="text-sm">{i.getValue()}</span>,
    }),
    oCol.display({
      id: 'lines',
      header: 'Líneas',
      cell: ({ row }) => <span className="text-sm text-muted-foreground">{row.original.lines.length}</span>,
    }),
    oCol.accessor('total', {
      header: 'Total',
      cell: i => <span className="font-medium">{fmt(i.getValue(), i.row.original.currencyCode)}</span>,
    }),
    oCol.accessor('originQuoteId', {
      header: 'Cotización',
      cell: i => i.getValue()
        ? <span className="font-mono text-xs text-muted-foreground">{i.getValue()!.slice(0, 8)}…</span>
        : <span className="text-muted-foreground">—</span>,
    }),
    oCol.display({
      id: 'actions',
      header: '',
      cell: ({ row }) => {
        const o = row.original
        if (o.status !== 'Draft' && o.status !== 'Confirmed') return null
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {o.status === 'Draft' && (
                <DropdownMenuItem onClick={() => confirmMut.mutate(o.id)}>Confirmar</DropdownMenuItem>
              )}
              {o.status === 'Confirmed' && (
                <DropdownMenuItem className="text-destructive" onClick={() => openCancelDialog(o.id)}>
                  Cancelar
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    }),
  ]

  const quoteTable = useReactTable({
    data: quotes ?? [], columns: quoteColumns,
    state: { sorting: quoteSorting }, onSortingChange: setQuoteSorting,
    getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel(),
  })

  const orderTable = useReactTable({
    data: orders ?? [], columns: orderColumns,
    state: { sorting: orderSorting }, onSortingChange: setOrderSorting,
    getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel(),
  })

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Ventas</h1>
          <p className="text-sm text-muted-foreground">Cotizaciones y órdenes de venta</p>
        </div>
        {activeTab === 'quotes' && (
          <Button onClick={() => { quoteForm.reset({ currencyCode: 'USD', countryCode: 'MX' }); setIsQuoteDialogOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />
            Nueva cotización
          </Button>
        )}
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="quotes">
            <FileText className="mr-2 h-4 w-4" />
            Cotizaciones
          </TabsTrigger>
          <TabsTrigger value="orders">
            <ShoppingCart className="mr-2 h-4 w-4" />
            Órdenes de Venta
          </TabsTrigger>
        </TabsList>

        {/* ── Quotes Tab ── */}
        <TabsContent value="quotes" className="space-y-4">
          <div className="flex gap-3">
            <Select value={quoteStatusFilter} onValueChange={setQuoteStatusFilter}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos los estados</SelectItem>
                <SelectItem value="Draft">Borrador</SelectItem>
                <SelectItem value="Sent">Enviadas</SelectItem>
                <SelectItem value="Accepted">Aceptadas</SelectItem>
                <SelectItem value="Rejected">Rechazadas</SelectItem>
                <SelectItem value="ConvertedToOrder">Convertidas</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <SalesTable table={quoteTable} columns={quoteColumns} isLoading={quotesLoading} />
        </TabsContent>

        {/* ── Orders Tab ── */}
        <TabsContent value="orders" className="space-y-4">
          <div className="flex gap-3">
            <Select value={orderStatusFilter} onValueChange={setOrderStatusFilter}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos los estados</SelectItem>
                <SelectItem value="Draft">Borrador</SelectItem>
                <SelectItem value="Confirmed">Confirmadas</SelectItem>
                <SelectItem value="Fulfilled">Surtidas</SelectItem>
                <SelectItem value="Cancelled">Canceladas</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <SalesTable table={orderTable} columns={orderColumns} isLoading={ordersLoading} />
        </TabsContent>
      </Tabs>

      {/* ── Create Quote Dialog ── */}
      <Dialog open={isQuoteDialogOpen} onOpenChange={setIsQuoteDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Nueva Cotización</DialogTitle></DialogHeader>
          <form onSubmit={quoteForm.handleSubmit(v => createQuoteMut.mutate({ ...v, notes: v.notes || undefined }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Cliente *</Label>
              <PartyPicker
                roleType="Customer"
                value={quoteForm.watch('customerId') ?? ''}
                onChange={id => quoteForm.setValue('customerId', id, { shouldValidate: true })}
              />
              {quoteForm.formState.errors.customerId && <p className="text-xs text-destructive">{quoteForm.formState.errors.customerId.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="validUntil">Válida hasta *</Label>
                <Input id="validUntil" type="date" {...quoteForm.register('validUntil')} />
                {quoteForm.formState.errors.validUntil && <p className="text-xs text-destructive">{quoteForm.formState.errors.validUntil.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="q-currency">Moneda *</Label>
                <Input id="q-currency" {...quoteForm.register('currencyCode')} placeholder="USD" maxLength={3} className="uppercase" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="q-country">País *</Label>
                <Input id="q-country" {...quoteForm.register('countryCode')} placeholder="MX" maxLength={2} className="uppercase" />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="q-notes">Notas</Label>
              <Input id="q-notes" {...quoteForm.register('notes')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsQuoteDialogOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createQuoteMut.isPending}>{createQuoteMut.isPending ? 'Guardando…' : 'Crear'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Add Line Dialog ── */}
      <Dialog open={isLineDialogOpen} onOpenChange={setIsLineDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Agregar Línea</DialogTitle></DialogHeader>
          <form onSubmit={lineForm.handleSubmit(v => addLineMut.mutate({ quoteId: selectedQuoteId!, body: v }))} className="space-y-4">
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
            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="l-qty">Cantidad *</Label>
                <Input id="l-qty" type="number" step="0.01" {...lineForm.register('quantity', { valueAsNumber: true })} />
                {lineForm.formState.errors.quantity && <p className="text-xs text-destructive">{lineForm.formState.errors.quantity.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="l-price">Precio Unit. *</Label>
                <Input id="l-price" type="number" step="0.01" {...lineForm.register('unitPrice', { valueAsNumber: true })} />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="l-disc">Desc. % </Label>
                <Input id="l-disc" type="number" step="0.01" min="0" max="100" {...lineForm.register('discountPercent', { valueAsNumber: true })} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="l-currency">Moneda *</Label>
              <Input id="l-currency" {...lineForm.register('currencyCode')} placeholder="USD" maxLength={3} className="uppercase w-24" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsLineDialogOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={addLineMut.isPending}>{addLineMut.isPending ? 'Guardando…' : 'Agregar'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Convert to Order Dialog ── */}
      <Dialog open={isConvertDialogOpen} onOpenChange={setIsConvertDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader><DialogTitle>Convertir en Orden de Venta</DialogTitle></DialogHeader>
          <form onSubmit={convertForm.handleSubmit(v => convertMut.mutate({ quoteId: selectedQuoteId!, body: v }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="c-delivery">Fecha de entrega solicitada</Label>
              <Input id="c-delivery" type="date" {...convertForm.register('requestedDeliveryDate')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="c-notes">Notas</Label>
              <Input id="c-notes" {...convertForm.register('notes')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsConvertDialogOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={convertMut.isPending}>
                {convertMut.isPending ? 'Creando…' : 'Crear Orden →'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Cancel Order Dialog ── */}
      <Dialog open={isCancelDialogOpen} onOpenChange={setIsCancelDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader><DialogTitle>Cancelar Orden</DialogTitle></DialogHeader>
          <form onSubmit={cancelForm.handleSubmit(v => cancelMut.mutate({ orderId: selectedOrderId!, reason: v.reason }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="cancel-reason">Motivo *</Label>
              <Input id="cancel-reason" {...cancelForm.register('reason')} placeholder="Describe el motivo de cancelación" />
              {cancelForm.formState.errors.reason && <p className="text-xs text-destructive">{cancelForm.formState.errors.reason.message}</p>}
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsCancelDialogOpen(false)}>Volver</Button>
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

// ── Shared table ──────────────────────────────────────────────────────────────

function SalesTable<T>({
  table, columns, isLoading,
}: {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  table: ReturnType<typeof useReactTable<T>>
  columns: unknown[]
  isLoading: boolean
}) {
  return (
    <div className="rounded-md border">
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
                {(columns as unknown[]).map((_, j) => (
                  <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                ))}
              </TableRow>
            ))
          ) : table.getRowModel().rows.length === 0 ? (
            <TableRow>
              <TableCell colSpan={columns.length as number}
                className="h-24 text-center text-muted-foreground">
                No se encontraron resultados
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
  )
}
