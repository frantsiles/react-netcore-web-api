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
import { CheckCircle2, XCircle, ChevronUp, ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

import { approvalsService, type ApprovalRequestDto, type ApprovalRequestStatus } from './approvalsService'

const STATUS_LABELS: Record<ApprovalRequestStatus, string> = {
  Pending: 'Pendiente', Approved: 'Aprobada', Rejected: 'Rechazada', Cancelled: 'Cancelada',
}
const STATUS_VARIANT: Record<ApprovalRequestStatus, 'default' | 'success' | 'destructive' | 'secondary'> = {
  Pending: 'default', Approved: 'success', Rejected: 'destructive', Cancelled: 'secondary',
}

const decisionSchema = z.object({
  approverUserId: z.string().uuid('UUID requerido'),
  notes:          z.string().optional(),
})
type DecisionForm = z.infer<typeof decisionSchema>

const col = createColumnHelper<ApprovalRequestDto>()

type DialogAction = 'approve' | 'reject'

export function ApprovalsPage() {
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('all')
  const [sorting, setSorting] = useState<SortingState>([])
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [dialogAction, setDialogAction] = useState<DialogAction>('approve')
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const { data: requests, isLoading } = useQuery({
    queryKey: ['approval-requests', statusFilter],
    queryFn: () => approvalsService.requests.list(statusFilter !== 'all' ? statusFilter : undefined),
  })

  const approveMut = useMutation({
    mutationFn: ({ id, approverUserId, notes }: { id: string; approverUserId: string; notes?: string }) =>
      approvalsService.requests.approve(id, approverUserId, notes),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['approval-requests'] }); setIsDialogOpen(false); toast.success('Solicitud aprobada') },
    onError: () => toast.error('Error al aprobar'),
  })

  const rejectMut = useMutation({
    mutationFn: ({ id, approverUserId, notes }: { id: string; approverUserId: string; notes?: string }) =>
      approvalsService.requests.reject(id, approverUserId, notes),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['approval-requests'] }); setIsDialogOpen(false); toast.success('Solicitud rechazada') },
    onError: () => toast.error('Error al rechazar'),
  })

  const form = useForm<DecisionForm>({ resolver: zodResolver(decisionSchema) })

  const openDialog = (id: string, action: DialogAction) => {
    setSelectedId(id)
    setDialogAction(action)
    form.reset()
    setIsDialogOpen(true)
  }

  const onSubmit = (v: DecisionForm) => {
    if (!selectedId) return
    const payload = { id: selectedId, approverUserId: v.approverUserId, notes: v.notes || undefined }
    if (dialogAction === 'approve') approveMut.mutate(payload)
    else rejectMut.mutate(payload)
  }

  const fmt = (n: number) => new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(n)

  const columns = [
    col.accessor('entityReference', { header: 'Referencia', cell: i => <span className="font-mono text-sm font-medium">{i.getValue()}</span> }),
    col.accessor('entityType', { header: 'Tipo', cell: i => <Badge variant="secondary">{i.getValue()}</Badge> }),
    col.accessor('amount', { header: 'Monto', cell: i => i.getValue() ? <span className="font-medium">{fmt(i.getValue()!)}</span> : <span className="text-muted-foreground">—</span> }),
    col.accessor('notes', { header: 'Notas', cell: i => <span className="text-sm text-muted-foreground max-w-48 truncate block">{i.getValue() ?? '—'}</span> }),
    col.accessor('status', { header: 'Estado', cell: i => <Badge variant={STATUS_VARIANT[i.getValue()]}>{STATUS_LABELS[i.getValue()]}</Badge> }),
    col.accessor('createdAt', { header: 'Creada', cell: i => <span className="text-sm">{new Date(i.getValue()).toLocaleDateString('es-MX')}</span> }),
    col.display({
      id: 'actions', header: '',
      cell: ({ row }) => row.original.status !== 'Pending' ? null : (
        <div className="flex gap-1">
          <Button size="sm" variant="outline" className="h-7 gap-1 text-xs text-green-700"
            onClick={() => openDialog(row.original.id, 'approve')}>
            <CheckCircle2 className="h-3.5 w-3.5" />Aprobar
          </Button>
          <Button size="sm" variant="outline" className="h-7 gap-1 text-xs text-destructive"
            onClick={() => openDialog(row.original.id, 'reject')}>
            <XCircle className="h-3.5 w-3.5" />Rechazar
          </Button>
        </div>
      ),
    }),
  ]

  const table = useReactTable({ data: requests ?? [], columns, state: { sorting }, onSortingChange: setSorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Aprobaciones</h1>
          <p className="text-sm text-muted-foreground">Solicitudes de aprobación pendientes</p>
        </div>
      </div>

      <div className="flex gap-3">
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[180px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los estados</SelectItem>
            <SelectItem value="Pending">Pendientes</SelectItem>
            <SelectItem value="Approved">Aprobadas</SelectItem>
            <SelectItem value="Rejected">Rechazadas</SelectItem>
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
                ? <TableRow><TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">No hay solicitudes</TableCell></TableRow>
                : table.getRowModel().rows.map(row => (
                    <TableRow key={row.id}>
                      {row.getVisibleCells().map(cell => <TableCell key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</TableCell>)}
                    </TableRow>
                  ))
            }
          </TableBody>
        </Table>
      </div>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>{dialogAction === 'approve' ? 'Aprobar Solicitud' : 'Rechazar Solicitud'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label>ID del Aprobador *</Label>
              <Input {...form.register('approverUserId')} placeholder="UUID del usuario aprobador" />
              {form.formState.errors.approverUserId && <p className="text-xs text-destructive">{form.formState.errors.approverUserId.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Notas</Label>
              <Input {...form.register('notes')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>Cancelar</Button>
              <Button type="submit" variant={dialogAction === 'reject' ? 'destructive' : 'default'}
                disabled={approveMut.isPending || rejectMut.isPending}>
                {dialogAction === 'approve' ? 'Aprobar' : 'Rechazar'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
