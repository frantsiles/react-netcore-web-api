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
import { Plus, Building2, MoreHorizontal, ChevronUp, ChevronDown, Settings2, X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import api from '@/services/api'

// ── Types ─────────────────────────────────────────────────────────────────────

interface TenantSetting { key: string; value: string }

interface Tenant {
  id: string
  name: string
  slug: string
  countryCode: string
  currencyCode: string
  plan: string
  status: string
  settings: TenantSetting[]
  createdAt: string
  updatedAt: string
}

// ── Schemas ───────────────────────────────────────────────────────────────────

const createSchema = z.object({
  name:         z.string().min(2, 'Mínimo 2 caracteres'),
  slug:         z.string().min(2).regex(/^[a-z0-9-]+$/, 'Solo minúsculas, números y guiones'),
  countryCode:  z.string().length(2, 'Código de 2 letras'),
  currencyCode: z.string().length(3, 'Código de 3 letras'),
  plan:         z.enum(['Free', 'Standard', 'Enterprise']),
})
type CreateForm = z.infer<typeof createSchema>

const editSchema = z.object({
  name:         z.string().min(2, 'Mínimo 2 caracteres'),
  countryCode:  z.string().length(2, 'Código de 2 letras'),
  currencyCode: z.string().length(3, 'Código de 3 letras'),
  plan:         z.enum(['Free', 'Standard', 'Enterprise']),
})
type EditForm = z.infer<typeof editSchema>

const settingSchema = z.object({
  key:   z.string().min(1, 'Requerido'),
  value: z.string().min(1, 'Requerido'),
})
type SettingForm = z.infer<typeof settingSchema>

// ── Service ───────────────────────────────────────────────────────────────────

const svc = {
  list:       ()                              => api.get<Tenant[]>('/bff/admin/tenants').then(r => r.data),
  create:     (b: CreateForm)                 => api.post<Tenant>('/bff/admin/tenants', b).then(r => r.data),
  update:     (id: string, b: EditForm)       => api.put<Tenant>(`/bff/admin/tenants/${id}`, b).then(r => r.data),
  suspend:    (id: string)                    => api.post<Tenant>(`/bff/admin/tenants/${id}/suspend`).then(r => r.data),
  reactivate: (id: string)                    => api.post<Tenant>(`/bff/admin/tenants/${id}/reactivate`).then(r => r.data),
  setSetting: (id: string, k: string, v: string) => api.put<Tenant>(`/bff/admin/tenants/${id}/settings/${encodeURIComponent(k)}`, { value: v }).then(r => r.data),
  delSetting: (id: string, k: string)         => api.delete<Tenant>(`/bff/admin/tenants/${id}/settings/${encodeURIComponent(k)}`).then(r => r.data),
}

// ── Badges ────────────────────────────────────────────────────────────────────

const statusVariant: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  Active:    'default',
  Suspended: 'outline',
  Cancelled: 'destructive',
}
const planVariant: Record<string, 'default' | 'secondary' | 'outline'> = {
  Free:       'secondary',
  Standard:   'default',
  Enterprise: 'outline',
}

// ── Column helper ─────────────────────────────────────────────────────────────

const col = createColumnHelper<Tenant>()

// ── Page ──────────────────────────────────────────────────────────────────────

export function TenantsAdminPage() {
  const qc = useQueryClient()
  const [sorting, setSorting] = useState<SortingState>([])
  const [createOpen, setCreateOpen]     = useState(false)
  const [editTenant, setEditTenant]     = useState<Tenant | null>(null)
  const [settingsTenant, setSettingsTenant] = useState<Tenant | null>(null)

  const { data: tenants = [], isLoading } = useQuery<Tenant[]>({
    queryKey: ['tenants'],
    queryFn:  svc.list,
  })

  const inv = () => qc.invalidateQueries({ queryKey: ['tenants'] })

  const createMut = useMutation({
    mutationFn: svc.create,
    onSuccess: (t) => { toast.success(`Tenant "${t.name}" creado`); inv(); setCreateOpen(false); createForm.reset() },
    onError: () => toast.error('Error al crear el tenant'),
  })

  const editMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: EditForm }) => svc.update(id, body),
    onSuccess: (t) => { toast.success(`Tenant "${t.name}" actualizado`); inv(); setEditTenant(null) },
    onError: () => toast.error('Error al actualizar'),
  })

  const suspendMut = useMutation({
    mutationFn: svc.suspend,
    onSuccess: () => { toast.success('Tenant suspendido'); inv() },
    onError: () => toast.error('Error al suspender'),
  })

  const reactivateMut = useMutation({
    mutationFn: svc.reactivate,
    onSuccess: () => { toast.success('Tenant reactivado'); inv() },
    onError: () => toast.error('Error al reactivar'),
  })

  const setSettingMut = useMutation({
    mutationFn: ({ id, key, value }: { id: string; key: string; value: string }) => svc.setSetting(id, key, value),
    onSuccess: (t) => { toast.success('Setting guardado'); inv(); setSettingsTenant(t); settingForm.reset() },
    onError: () => toast.error('Error al guardar setting'),
  })

  const delSettingMut = useMutation({
    mutationFn: ({ id, key }: { id: string; key: string }) => svc.delSetting(id, key),
    onSuccess: (t) => { toast.success('Setting eliminado'); inv(); setSettingsTenant(t) },
    onError: () => toast.error('Error al eliminar setting'),
  })

  const createForm = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { plan: 'Free', countryCode: 'MX', currencyCode: 'MXN' },
  })

  const editForm = useForm<EditForm>({ resolver: zodResolver(editSchema) })

  const settingForm = useForm<SettingForm>({ resolver: zodResolver(settingSchema) })

  const openEdit = (t: Tenant) => {
    editForm.reset({ name: t.name, countryCode: t.countryCode, currencyCode: t.currencyCode, plan: t.plan as EditForm['plan'] })
    setEditTenant(t)
  }

  const columns = [
    col.accessor('name', { header: 'Nombre' }),
    col.accessor('slug', {
      header: 'Slug',
      cell: info => <code className="text-xs bg-muted px-1 rounded">{info.getValue()}</code>,
    }),
    col.accessor('plan', {
      header: 'Plan',
      cell: info => <Badge variant={planVariant[info.getValue()] ?? 'secondary'}>{info.getValue()}</Badge>,
    }),
    col.accessor('status', {
      header: 'Estado',
      cell: info => <Badge variant={statusVariant[info.getValue()] ?? 'secondary'}>{info.getValue()}</Badge>,
    }),
    col.accessor('countryCode', { header: 'País' }),
    col.accessor('currencyCode', { header: 'Moneda' }),
    col.accessor('createdAt', {
      header: 'Creado',
      cell: info => new Date(info.getValue()).toLocaleDateString('es-MX'),
    }),
    col.display({
      id: 'actions',
      header: '',
      cell: ({ row }) => {
        const t = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm"><MoreHorizontal className="h-4 w-4" /></Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openEdit(t)}>Editar</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setSettingsTenant(t)}>
                <Settings2 className="h-3.5 w-3.5 mr-1" /> Settings
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {t.status === 'Active' ? (
                <DropdownMenuItem className="text-amber-600" onClick={() => suspendMut.mutate(t.id)}>
                  Suspender
                </DropdownMenuItem>
              ) : t.status === 'Suspended' ? (
                <DropdownMenuItem onClick={() => reactivateMut.mutate(t.id)}>
                  Reactivar
                </DropdownMenuItem>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    }),
  ]

  const table = useReactTable({
    data: tenants,
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <Building2 className="h-6 w-6" /> Tenants
          </h1>
          <p className="text-muted-foreground text-sm">Administración de organizaciones en el sistema</p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="h-4 w-4 mr-1" /> Nuevo Tenant
        </Button>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">{Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}</div>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              {table.getHeaderGroups().map(hg => (
                <TableRow key={hg.id}>
                  {hg.headers.map(h => (
                    <TableHead key={h.id} onClick={h.column.getToggleSortingHandler()}
                      className={h.column.getCanSort() ? 'cursor-pointer select-none' : ''}>
                      <span className="flex items-center gap-1">
                        {flexRender(h.column.columnDef.header, h.getContext())}
                        {h.column.getIsSorted() === 'asc' && <ChevronUp className="h-3 w-3" />}
                        {h.column.getIsSorted() === 'desc' && <ChevronDown className="h-3 w-3" />}
                      </span>
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {table.getRowModel().rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={columns.length} className="text-center text-muted-foreground py-8">
                    No hay tenants registrados
                  </TableCell>
                </TableRow>
              ) : (
                table.getRowModel().rows.map(row => (
                  <TableRow key={row.id}>
                    {row.getVisibleCells().map(cell => (
                      <TableCell key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</TableCell>
                    ))}
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Create Dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Nuevo Tenant</DialogTitle></DialogHeader>
          <form onSubmit={createForm.handleSubmit(d => createMut.mutate(d))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2 space-y-1.5">
                <Label>Nombre *</Label>
                <Input {...createForm.register('name')} placeholder="Empresa Demo S.A. de C.V." />
                {createForm.formState.errors.name && <p className="text-xs text-destructive">{createForm.formState.errors.name.message}</p>}
              </div>
              <div className="col-span-2 space-y-1.5">
                <Label>Slug * <span className="text-muted-foreground text-xs">(URL-friendly, solo minúsculas)</span></Label>
                <Input {...createForm.register('slug')} placeholder="empresa-demo" />
                {createForm.formState.errors.slug && <p className="text-xs text-destructive">{createForm.formState.errors.slug.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>País (ISO 2) *</Label>
                <Input {...createForm.register('countryCode')} placeholder="MX" maxLength={2} className="uppercase" />
                {createForm.formState.errors.countryCode && <p className="text-xs text-destructive">{createForm.formState.errors.countryCode.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Moneda (ISO 3) *</Label>
                <Input {...createForm.register('currencyCode')} placeholder="MXN" maxLength={3} className="uppercase" />
                {createForm.formState.errors.currencyCode && <p className="text-xs text-destructive">{createForm.formState.errors.currencyCode.message}</p>}
              </div>
              <div className="col-span-2 space-y-1.5">
                <Label>Plan *</Label>
                <Select
                  value={createForm.watch('plan')}
                  onValueChange={v => createForm.setValue('plan', v as CreateForm['plan'])}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Free">Free</SelectItem>
                    <SelectItem value="Standard">Standard</SelectItem>
                    <SelectItem value="Enterprise">Enterprise</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createMut.isPending}>
                {createMut.isPending ? 'Creando…' : 'Crear Tenant'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editTenant} onOpenChange={open => !open && setEditTenant(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader><DialogTitle>Editar Tenant — {editTenant?.slug}</DialogTitle></DialogHeader>
          <form onSubmit={editForm.handleSubmit(d => editMut.mutate({ id: editTenant!.id, body: d }))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2 space-y-1.5">
                <Label>Nombre *</Label>
                <Input {...editForm.register('name')} />
                {editForm.formState.errors.name && <p className="text-xs text-destructive">{editForm.formState.errors.name.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>País (ISO 2) *</Label>
                <Input {...editForm.register('countryCode')} maxLength={2} className="uppercase" />
                {editForm.formState.errors.countryCode && <p className="text-xs text-destructive">{editForm.formState.errors.countryCode.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Moneda (ISO 3) *</Label>
                <Input {...editForm.register('currencyCode')} maxLength={3} className="uppercase" />
                {editForm.formState.errors.currencyCode && <p className="text-xs text-destructive">{editForm.formState.errors.currencyCode.message}</p>}
              </div>
              <div className="col-span-2 space-y-1.5">
                <Label>Plan *</Label>
                <Select
                  value={editForm.watch('plan')}
                  onValueChange={v => editForm.setValue('plan', v as EditForm['plan'])}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Free">Free</SelectItem>
                    <SelectItem value="Standard">Standard</SelectItem>
                    <SelectItem value="Enterprise">Enterprise</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setEditTenant(null)}>Cancelar</Button>
              <Button type="submit" disabled={editMut.isPending}>
                {editMut.isPending ? 'Guardando…' : 'Guardar Cambios'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Settings Dialog */}
      <Dialog open={!!settingsTenant} onOpenChange={open => !open && setSettingsTenant(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Settings2 className="h-5 w-5" /> Settings — {settingsTenant?.name}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            {/* Existing settings */}
            {settingsTenant && settingsTenant.settings.length > 0 ? (
              <div className="rounded-md border divide-y">
                {settingsTenant.settings.map(s => (
                  <div key={s.key} className="flex items-center justify-between px-3 py-2">
                    <div>
                      <span className="font-mono text-xs font-medium">{s.key}</span>
                      <span className="mx-2 text-muted-foreground">→</span>
                      <span className="text-sm">{s.value}</span>
                    </div>
                    <Button
                      variant="ghost" size="sm"
                      onClick={() => delSettingMut.mutate({ id: settingsTenant.id, key: s.key })}
                      disabled={delSettingMut.isPending}>
                      <X className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Sin settings configurados.</p>
            )}

            {/* Add setting */}
            <form
              onSubmit={settingForm.handleSubmit(d =>
                setSettingMut.mutate({ id: settingsTenant!.id, key: d.key, value: d.value })
              )}
              className="grid grid-cols-5 gap-2 items-end">
              <div className="col-span-2 space-y-1">
                <Label className="text-xs">Clave</Label>
                <Input {...settingForm.register('key')} placeholder="feature.x" className="h-8 text-sm" />
              </div>
              <div className="col-span-2 space-y-1">
                <Label className="text-xs">Valor</Label>
                <Input {...settingForm.register('value')} placeholder="true" className="h-8 text-sm" />
              </div>
              <Button type="submit" size="sm" disabled={setSettingMut.isPending} className="h-8">
                {setSettingMut.isPending ? '…' : 'Añadir'}
              </Button>
            </form>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
