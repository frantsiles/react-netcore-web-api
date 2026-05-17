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
import { Plus, BookOpen, List, ChevronUp, ChevronDown, X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

import { accountingService, type AccountDto, type JournalEntryDto, type AccountType, type EntryStatus } from './accountingService'
import { AccountPicker } from '@/components/pickers/AccountPicker'

const fmt = (n: number) => new Intl.NumberFormat('es-MX', { minimumFractionDigits: 2 }).format(n)

const ACCOUNT_TYPE_LABELS: Record<AccountType, string> = {
  Asset: 'Activo', Liability: 'Pasivo', Equity: 'Capital',
  Revenue: 'Ingreso', Expense: 'Gasto',
}
const ENTRY_STATUS_LABELS: Record<EntryStatus, string> = {
  Draft: 'Borrador', Posted: 'Publicado', Reversed: 'Revertido',
}
const ENTRY_STATUS_VARIANT: Record<EntryStatus, 'default' | 'secondary' | 'outline'> = {
  Draft: 'secondary', Posted: 'default', Reversed: 'outline',
}

const accountSchema = z.object({
  accountNumber: z.string().min(1, 'Requerido'),
  name:          z.string().min(1, 'Requerido'),
  type:          z.string().min(1, 'Requerido'),
  currencyCode:  z.string().length(3, '3 letras'),
  description:   z.string().optional(),
})
type AccountForm = z.infer<typeof accountSchema>

const entrySchema = z.object({
  fiscalPeriod: z.string().min(1, 'Requerido'),
  entryDate:    z.string().min(1, 'Requerido'),
  description:  z.string().min(1, 'Requerido'),
})
type EntryForm = z.infer<typeof entrySchema>

const lineSchema = z.object({
  accountId:   z.string().min(1, 'Seleccionar cuenta'),
  side:        z.enum(['Debit', 'Credit']),
  amount:      z.number().positive('> 0'),
  description: z.string().optional(),
})
type LineForm = z.infer<typeof lineSchema>

interface LocalLine { accountId: string; accountLabel: string; side: 'Debit' | 'Credit'; amount: number; description?: string }

const aCol = createColumnHelper<AccountDto>()
const eCol = createColumnHelper<JournalEntryDto>()

export function AccountingPage() {
  const qc = useQueryClient()
  const [activeTab, setActiveTab] = useState('accounts')
  const [statusFilter, setStatusFilter] = useState('all')
  const [aSorting, setASorting] = useState<SortingState>([])
  const [eSorting, setESorting] = useState<SortingState>([])
  const [isAccountOpen, setIsAccountOpen] = useState(false)
  const [isEntryOpen,   setIsEntryOpen]   = useState(false)
  const [localLines,    setLocalLines]    = useState<LocalLine[]>([])
  const [lineAccLabel,  setLineAccLabel]  = useState<string>('')

  const { data: accounts, isLoading: accLoading } = useQuery({
    queryKey: ['accounts'],
    queryFn: () => accountingService.accounts.list(),
  })

  const { data: entries, isLoading: entLoading } = useQuery({
    queryKey: ['journal-entries', statusFilter],
    queryFn: () => accountingService.entries.search(
      undefined, statusFilter !== 'all' ? statusFilter : undefined),
  })

  const createAccountMut = useMutation({
    mutationFn: accountingService.accounts.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['accounts'] }); setIsAccountOpen(false); toast.success('Cuenta creada') },
    onError:   () => toast.error('Error al crear cuenta'),
  })

  const createEntryMut = useMutation({
    mutationFn: async (v: EntryForm) => {
      const entry = await accountingService.entries.create({ ...v })
      for (const line of localLines) {
        await accountingService.entries.addLine(entry.id, {
          accountId: line.accountId, side: line.side, amount: line.amount, description: line.description,
        })
      }
      return entry
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['journal-entries'] })
      setIsEntryOpen(false)
      setLocalLines([])
      entryForm.reset()
      toast.success('Asiento creado')
    },
    onError: () => toast.error('Error al crear asiento'),
  })

  const postMut = useMutation({
    mutationFn: (id: string) => accountingService.entries.post(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['journal-entries'] }); toast.success('Asiento publicado') },
    onError:   () => toast.error('Error al publicar'),
  })

  const accountForm = useForm<AccountForm>({ resolver: zodResolver(accountSchema), defaultValues: { currencyCode: 'MXN' } })
  const entryForm   = useForm<EntryForm>({  resolver: zodResolver(entrySchema) })
  const lineForm    = useForm<LineForm>({   resolver: zodResolver(lineSchema), defaultValues: { side: 'Debit' } })

  const accountCols = [
    aCol.accessor('accountNumber', { header: 'Número', cell: i => <span className="font-mono text-sm">{i.getValue()}</span> }),
    aCol.accessor('name', { header: 'Nombre' }),
    aCol.accessor('type', { header: 'Tipo', cell: i => <Badge variant="secondary">{ACCOUNT_TYPE_LABELS[i.getValue()]}</Badge> }),
    aCol.accessor('currencyCode', { header: 'Moneda', cell: i => <span className="text-sm">{i.getValue()}</span> }),
    aCol.accessor('balance', { header: 'Saldo', cell: i => <span className="font-medium">{fmt(i.getValue())}</span> }),
    aCol.accessor('isActive', { header: 'Estado', cell: i => <Badge variant={i.getValue() ? 'success' : 'secondary'}>{i.getValue() ? 'Activa' : 'Inactiva'}</Badge> }),
  ]

  const entryCols = [
    eCol.accessor('entryNumber', { header: 'Número', cell: i => <span className="font-mono text-sm font-medium">{i.getValue()}</span> }),
    eCol.accessor('fiscalPeriod', { header: 'Período', cell: i => <span className="text-sm">{i.getValue()}</span> }),
    eCol.accessor('entryDate', { header: 'Fecha', cell: i => <span className="text-sm">{new Date(i.getValue()).toLocaleDateString('es-MX')}</span> }),
    eCol.accessor('description', { header: 'Descripción', cell: i => <span className="text-sm max-w-48 truncate block">{i.getValue()}</span> }),
    eCol.accessor('status', { header: 'Estado', cell: i => <Badge variant={ENTRY_STATUS_VARIANT[i.getValue()]}>{ENTRY_STATUS_LABELS[i.getValue()]}</Badge> }),
    eCol.accessor('totalDebits', { header: 'Débitos', cell: i => <span className="font-medium">{fmt(i.getValue())}</span> }),
    eCol.accessor('isBalanced', { header: 'Bal.', cell: i => <Badge variant={i.getValue() ? 'success' : 'destructive'}>{i.getValue() ? '✓' : '✗'}</Badge> }),
    eCol.display({
      id: 'actions', header: '',
      cell: ({ row }) => row.original.status === 'Draft' ? (
        <Button size="sm" variant="outline" className="h-7 text-xs" onClick={() => postMut.mutate(row.original.id)}>
          Publicar
        </Button>
      ) : null,
    }),
  ]

  const aTable = useReactTable({ data: accounts ?? [], columns: accountCols, state: { sorting: aSorting }, onSortingChange: setASorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })
  const eTable = useReactTable({ data: entries ?? [], columns: entryCols, state: { sorting: eSorting }, onSortingChange: setESorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Contabilidad</h1>
          <p className="text-sm text-muted-foreground">Plan de cuentas y asientos contables</p>
        </div>
        {activeTab === 'accounts' ? (
          <Button onClick={() => { accountForm.reset({ currencyCode: 'MXN' }); setIsAccountOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />Nueva cuenta
          </Button>
        ) : (
          <Button onClick={() => { entryForm.reset(); setIsEntryOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />Nuevo asiento
          </Button>
        )}
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="accounts"><List className="mr-2 h-4 w-4" />Cuentas</TabsTrigger>
          <TabsTrigger value="entries"><BookOpen className="mr-2 h-4 w-4" />Asientos</TabsTrigger>
        </TabsList>

        <TabsContent value="accounts" className="space-y-4">
          <TableView table={aTable} columns={accountCols} isLoading={accLoading} empty="No hay cuentas registradas" />
        </TabsContent>

        <TabsContent value="entries" className="space-y-4">
          <div className="flex gap-3">
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-[180px]"><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos los estados</SelectItem>
                <SelectItem value="Draft">Borrador</SelectItem>
                <SelectItem value="Posted">Publicado</SelectItem>
                <SelectItem value="Reversed">Revertido</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <TableView table={eTable} columns={entryCols} isLoading={entLoading} empty="No hay asientos registrados" />
        </TabsContent>
      </Tabs>

      {/* ── Create Account Dialog ── */}
      <Dialog open={isAccountOpen} onOpenChange={setIsAccountOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Nueva Cuenta</DialogTitle></DialogHeader>
          <form onSubmit={accountForm.handleSubmit(v => createAccountMut.mutate({ ...v, description: v.description || undefined }))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Número *</Label>
                <Input {...accountForm.register('accountNumber')} placeholder="1000" />
                {accountForm.formState.errors.accountNumber && <p className="text-xs text-destructive">{accountForm.formState.errors.accountNumber.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Nombre *</Label>
                <Input {...accountForm.register('name')} placeholder="Caja y bancos" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Tipo *</Label>
                <Select onValueChange={v => accountForm.setValue('type', v)}>
                  <SelectTrigger><SelectValue placeholder="Seleccionar…" /></SelectTrigger>
                  <SelectContent>
                    {(Object.keys(ACCOUNT_TYPE_LABELS) as AccountType[]).map(t => (
                      <SelectItem key={t} value={t}>{ACCOUNT_TYPE_LABELS[t]}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Moneda *</Label>
                <Input {...accountForm.register('currencyCode')} placeholder="MXN" maxLength={3} className="uppercase" />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Descripción</Label>
              <Input {...accountForm.register('description')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsAccountOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createAccountMut.isPending}>{createAccountMut.isPending ? 'Creando…' : 'Crear'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Create Journal Entry Dialog ── */}
      <Dialog open={isEntryOpen} onOpenChange={open => { setIsEntryOpen(open); if (!open) { setLocalLines([]); lineForm.reset({ side: 'Debit' }); setLineAccLabel('') } }}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader><DialogTitle>Nuevo Asiento Contable</DialogTitle></DialogHeader>
          <form onSubmit={entryForm.handleSubmit(v => createEntryMut.mutate(v))} className="space-y-5">
            {/* Header fields */}
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Período fiscal *</Label>
                <Input {...entryForm.register('fiscalPeriod')} placeholder="2026-05" />
                {entryForm.formState.errors.fiscalPeriod && <p className="text-xs text-destructive">{entryForm.formState.errors.fiscalPeriod.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Fecha *</Label>
                <Input type="date" {...entryForm.register('entryDate')} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Descripción *</Label>
              <Input {...entryForm.register('description')} placeholder="Pago a proveedor XYZ" />
            </div>

            {/* Lines section */}
            <div className="space-y-2">
              <Label className="text-sm font-medium">Líneas de asiento</Label>

              {/* Existing lines */}
              {localLines.length > 0 && (
                <div className="rounded-md border divide-y text-sm">
                  {localLines.map((l, i) => (
                    <div key={i} className="flex items-center gap-2 px-3 py-2">
                      <span className="flex-1 text-xs font-mono truncate">{l.accountLabel}</span>
                      <Badge variant={l.side === 'Debit' ? 'default' : 'outline'} className="text-xs w-16 justify-center">{l.side === 'Debit' ? 'Débito' : 'Crédito'}</Badge>
                      <span className="w-24 text-right font-medium">{new Intl.NumberFormat('es-MX', { minimumFractionDigits: 2 }).format(l.amount)}</span>
                      <Button type="button" variant="ghost" size="sm" className="h-6 w-6 p-0"
                        onClick={() => setLocalLines(prev => prev.filter((_, j) => j !== i))}>
                        <X className="h-3 w-3" />
                      </Button>
                    </div>
                  ))}
                  {/* Totals row */}
                  {(() => {
                    const totalDebits  = localLines.reduce((s, x) => s + (x.side === 'Debit'  ? x.amount : 0), 0)
                    const totalCredits = localLines.reduce((s, x) => s + (x.side === 'Credit' ? x.amount : 0), 0)
                    const balanced     = Math.abs(totalDebits - totalCredits) < 0.01
                    const fmt2 = (n: number) => new Intl.NumberFormat('es-MX', { minimumFractionDigits: 2 }).format(n)
                    return (
                      <div className="flex items-center gap-2 px-3 py-2 bg-muted/40 text-xs text-muted-foreground">
                        <span className="flex-1">Totales</span>
                        <span>Débitos: <strong>{fmt2(totalDebits)}</strong></span>
                        <span>Créditos: <strong>{fmt2(totalCredits)}</strong></span>
                        <Badge variant={balanced ? 'default' : 'destructive'} className="text-xs">{balanced ? '✓ Balanceado' : '✗ Desbalanceado'}</Badge>
                      </div>
                    )
                  })()}
                </div>
              )}

              {/* Add line form */}
              <div className="rounded-md border p-3 space-y-2 bg-muted/20">
                <p className="text-xs text-muted-foreground font-medium">Añadir línea</p>
                <div className="grid grid-cols-12 gap-2 items-end">
                  <div className="col-span-5">
                    <Label className="text-xs">Cuenta</Label>
                    <AccountPicker
                      value={lineForm.watch('accountId')}
                      onChange={(id, label) => {
                        lineForm.setValue('accountId', id ?? '', { shouldValidate: true })
                        setLineAccLabel(label ?? id ?? '')
                      }}
                      placeholder="Seleccionar cuenta…"
                    />
                    {lineForm.formState.errors.accountId && <p className="text-xs text-destructive">{lineForm.formState.errors.accountId.message}</p>}
                  </div>
                  <div className="col-span-3">
                    <Label className="text-xs">Debe / Haber</Label>
                    <Select value={lineForm.watch('side')} onValueChange={v => lineForm.setValue('side', v as LineForm['side'])}>
                      <SelectTrigger className="h-9"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Debit">Débito</SelectItem>
                        <SelectItem value="Credit">Crédito</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="col-span-3">
                    <Label className="text-xs">Importe</Label>
                    <Input type="number" step="0.01" className="h-9"
                      {...lineForm.register('amount', { valueAsNumber: true })}
                      placeholder="0.00" />
                    {lineForm.formState.errors.amount && <p className="text-xs text-destructive">{lineForm.formState.errors.amount.message}</p>}
                  </div>
                  <div className="col-span-1">
                    <Button type="button" size="sm" className="h-9 w-full"
                      onClick={lineForm.handleSubmit(l => {
                        setLocalLines(prev => [...prev, { ...l, accountLabel: lineAccLabel }])
                        lineForm.reset({ side: lineForm.getValues('side') })
                        setLineAccLabel('')
                      })}>
                      <Plus className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </div>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => { setIsEntryOpen(false); setLocalLines([]); lineForm.reset({ side: 'Debit' }); setLineAccLabel('') }}>Cancelar</Button>
              <Button type="submit" disabled={createEntryMut.isPending}>
                {createEntryMut.isPending ? 'Creando…' : 'Crear asiento'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function TableView<T>({ table, columns, isLoading, empty }: {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  table: ReturnType<typeof useReactTable<T>>
  columns: unknown[]
  isLoading: boolean
  empty: string
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
                    {h.column.getIsSorted() === 'asc'  && <ChevronUp className="h-3 w-3" />}
                    {h.column.getIsSorted() === 'desc' && <ChevronDown className="h-3 w-3" />}
                  </div>
                </TableHead>
              ))}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {isLoading
            ? Array.from({ length: 4 }).map((_, i) => (
                <TableRow key={i}>{(columns as unknown[]).map((_, j) => <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>)}</TableRow>
              ))
            : table.getRowModel().rows.length === 0
              ? <TableRow><TableCell colSpan={columns.length as number} className="h-24 text-center text-muted-foreground">{empty}</TableCell></TableRow>
              : table.getRowModel().rows.map(row => (
                  <TableRow key={row.id}>
                    {row.getVisibleCells().map(cell => (
                      <TableCell key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</TableCell>
                    ))}
                  </TableRow>
                ))
          }
        </TableBody>
      </Table>
    </div>
  )
}
