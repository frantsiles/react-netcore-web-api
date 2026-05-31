import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  flexRender,
  createColumnHelper,
  type SortingState,
} from '@tanstack/react-table'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { Plus, Search, MoreHorizontal, UserX, UserCheck, Pencil, ChevronUp, ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

import { partiesService } from './partiesService'
import type { PartyDto, PartyRoleType } from '@/types/erp/parties'

// ── Schemas ───────────────────────────────────────────────────────────────────

const editSchema = z.object({
  legalName: z.string().min(2, 'Mínimo 2 caracteres'),
  tradeName: z.string().optional(),
  taxId:     z.string().optional(),
})

type EditForm = z.infer<typeof editSchema>

const registerSchema = z.object({
  legalName:     z.string().min(2, 'Mínimo 2 caracteres'),
  tradeName:     z.string().optional(),
  partyType:     z.enum(['Individual', 'Organization']),
  countryCode:   z.string().length(2, 'Código ISO de 2 letras'),
  firstRoleType: z.enum(['Customer', 'Supplier', 'Employee', 'Contact']),
  taxId:         z.string().optional(),
})

type RegisterForm = z.infer<typeof registerSchema>

// ── Helpers ───────────────────────────────────────────────────────────────────

const ROLE_LABELS: Record<string, string> = {
  Customer: 'Cliente',
  Supplier: 'Proveedor',
  Employee: 'Empleado',
  Contact:  'Contacto',
}

const TYPE_LABELS: Record<string, string> = {
  Individual:   'Individual',
  Organization: 'Organización',
}

function RoleBadge({ roleType }: { roleType: string }) {
  const colorMap: Record<string, 'default' | 'secondary' | 'outline' | 'success'> = {
    Customer: 'default',
    Supplier: 'secondary',
    Employee: 'success',
    Contact:  'outline',
  }
  return (
    <Badge variant={colorMap[roleType] ?? 'outline'}>
      {ROLE_LABELS[roleType] ?? roleType}
    </Badge>
  )
}

// ── Column helper ─────────────────────────────────────────────────────────────

const col = createColumnHelper<PartyDto>()

// ── Page ──────────────────────────────────────────────────────────────────────

export function PartiesPage() {
  const qc = useQueryClient()

  // Search state (applied vs. live input)
  const [appliedFilters, setAppliedFilters] = useState<{
    legalName?: string
    roleType?: PartyRoleType
    isActive?: boolean
  }>({ isActive: true })

  const [inputName,     setInputName]     = useState('')
  const [inputRole,     setInputRole]     = useState<string>('all')
  const [inputStatus,   setInputStatus]   = useState<string>('active')

  // Dialog state
  const [isRegisterOpen, setIsRegisterOpen] = useState(false)
  const [editTarget, setEditTarget] = useState<PartyDto | null>(null)

  // Sorting
  const [sorting, setSorting] = useState<SortingState>([])

  // ── Query ──────────────────────────────────────────────────────────────────

  const { data, isLoading } = useQuery({
    queryKey: ['parties', appliedFilters],
    queryFn: () => partiesService.search({
      legalName: appliedFilters.legalName || undefined,
      roleType:  appliedFilters.roleType,
      isActive:  appliedFilters.isActive,
      skip:  0,
      take: 100,
    }),
  })

  // ── Mutations ──────────────────────────────────────────────────────────────

  const registerMutation = useMutation({
    mutationFn: partiesService.register,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parties'] })
      setIsRegisterOpen(false)
      toast.success('Parte registrada correctamente')
    },
    onError: () => toast.error('Error al registrar la parte'),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => partiesService.deactivate(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parties'] })
      toast.success('Parte desactivada')
    },
    onError: () => toast.error('Error al desactivar'),
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => partiesService.reactivate(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parties'] })
      toast.success('Parte reactivada')
    },
    onError: () => toast.error('Error al reactivar'),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, body }: { id: string; body: { legalName: string; tradeName?: string; taxId?: string } }) =>
      partiesService.updateProfile(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['parties'] })
      setEditTarget(null)
      toast.success('Parte actualizada')
    },
    onError: () => toast.error('Error al actualizar'),
  })

  // ── Form ───────────────────────────────────────────────────────────────────

  const form = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      partyType:     'Organization',
      firstRoleType: 'Customer',
      countryCode:   'US',
    },
  })

  const editForm = useForm<EditForm>({
    resolver: zodResolver(editSchema),
  })

  const onEditSubmit = (values: EditForm) => {
    if (!editTarget) return
    updateMutation.mutate({
      id: editTarget.partyId,
      body: {
        legalName: values.legalName,
        tradeName: values.tradeName || undefined,
        taxId:     values.taxId || undefined,
      },
    })
  }

  const openEdit = (party: PartyDto) => {
    editForm.reset({
      legalName: party.legalName,
      tradeName: party.tradeName ?? '',
      taxId:     party.taxId ?? '',
    })
    setEditTarget(party)
  }

  const onSubmit = (values: RegisterForm) => {
    registerMutation.mutate({
      legalName:     values.legalName,
      tradeName:     values.tradeName || undefined,
      partyType:     values.partyType,
      countryCode:   values.countryCode.toUpperCase(),
      firstRoleType: values.firstRoleType,
      taxId:         values.taxId || undefined,
    })
  }

  const handleSearch = () => {
    setAppliedFilters({
      legalName: inputName || undefined,
      roleType:  inputRole !== 'all' ? (inputRole as PartyRoleType) : undefined,
      isActive:  inputStatus === 'all' ? undefined : inputStatus === 'active',
    })
  }

  // ── Table columns ──────────────────────────────────────────────────────────

  const columns = [
    col.accessor('legalName', {
      header: 'Nombre Legal',
      cell: info => (
        <div>
          <p className="font-medium">{info.getValue()}</p>
          {info.row.original.tradeName && (
            <p className="text-xs text-muted-foreground">{info.row.original.tradeName}</p>
          )}
        </div>
      ),
    }),
    col.accessor('partyType', {
      header: 'Tipo',
      cell: info => (
        <span className="text-sm text-muted-foreground">
          {TYPE_LABELS[info.getValue()] ?? info.getValue()}
        </span>
      ),
    }),
    col.accessor('roles', {
      header: 'Roles',
      enableSorting: false,
      cell: info => (
        <div className="flex flex-wrap gap-1">
          {info.getValue()
            .filter(r => r.status === 'Active')
            .map(r => <RoleBadge key={r.roleType} roleType={r.roleType} />)}
        </div>
      ),
    }),
    col.accessor('countryCode', {
      header: 'País',
      cell: info => <span className="font-mono text-sm">{info.getValue()}</span>,
    }),
    col.accessor('taxId', {
      header: 'RFC / RUC',
      cell: info => <span className="text-sm">{info.getValue() ?? '—'}</span>,
    }),
    col.accessor('isActive', {
      header: 'Estado',
      cell: info => info.getValue()
        ? <Badge variant="success">Activo</Badge>
        : <Badge variant="secondary">Inactivo</Badge>,
    }),
    col.display({
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => openEdit(row.original)}>
              <Pencil className="mr-2 h-4 w-4" />
              Editar
            </DropdownMenuItem>
            {row.original.isActive ? (
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => deactivateMutation.mutate(row.original.partyId)}
              >
                <UserX className="mr-2 h-4 w-4" />
                Desactivar
              </DropdownMenuItem>
            ) : (
              <DropdownMenuItem
                onClick={() => reactivateMutation.mutate(row.original.partyId)}
              >
                <UserCheck className="mr-2 h-4 w-4" />
                Reactivar
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    }),
  ]

  const table = useReactTable({
    data: data ?? [],
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Clientes y Proveedores</h1>
          <p className="text-sm text-muted-foreground">
            Gestiona clientes, proveedores, empleados y contactos
          </p>
        </div>
        <Button onClick={() => { form.reset(); setIsRegisterOpen(true) }}>
          <Plus className="mr-2 h-4 w-4" />
          Registrar
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex-1 min-w-[200px]">
          <Input
            placeholder="Buscar por nombre..."
            value={inputName}
            onChange={e => setInputName(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleSearch()}
          />
        </div>
        <Select value={inputRole} onValueChange={setInputRole}>
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Rol" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos los roles</SelectItem>
            <SelectItem value="Customer">Cliente</SelectItem>
            <SelectItem value="Supplier">Proveedor</SelectItem>
            <SelectItem value="Employee">Empleado</SelectItem>
            <SelectItem value="Contact">Contacto</SelectItem>
          </SelectContent>
        </Select>
        <Select value={inputStatus} onValueChange={setInputStatus}>
          <SelectTrigger className="w-[140px]">
            <SelectValue placeholder="Estado" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="active">Activos</SelectItem>
            <SelectItem value="inactive">Inactivos</SelectItem>
            <SelectItem value="all">Todos</SelectItem>
          </SelectContent>
        </Select>
        <Button variant="outline" onClick={handleSearch}>
          <Search className="mr-2 h-4 w-4" />
          Buscar
        </Button>
      </div>

      {/* Table */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map(hg => (
              <TableRow key={hg.id}>
                {hg.headers.map(header => (
                  <TableHead
                    key={header.id}
                    className={header.column.getCanSort() ? 'cursor-pointer select-none' : ''}
                    onClick={header.column.getToggleSortingHandler()}
                  >
                    <div className="flex items-center gap-1">
                      {flexRender(header.column.columnDef.header, header.getContext())}
                      {header.column.getIsSorted() === 'asc'  && <ChevronUp   className="h-3 w-3" />}
                      {header.column.getIsSorted() === 'desc' && <ChevronDown className="h-3 w-3" />}
                    </div>
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {columns.map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
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

      {/* Edit Dialog */}
      <Dialog open={!!editTarget} onOpenChange={open => !open && setEditTarget(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Editar Parte</DialogTitle>
          </DialogHeader>
          <form onSubmit={editForm.handleSubmit(onEditSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="edit-legalName">Nombre Legal *</Label>
              <Input id="edit-legalName" {...editForm.register('legalName')} />
              {editForm.formState.errors.legalName && (
                <p className="text-xs text-destructive">{editForm.formState.errors.legalName.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="edit-tradeName">Nombre Comercial</Label>
              <Input id="edit-tradeName" {...editForm.register('tradeName')} placeholder="Opcional" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="edit-taxId">RFC / RUC / Tax ID</Label>
              <Input id="edit-taxId" {...editForm.register('taxId')} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setEditTarget(null)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={updateMutation.isPending}>
                {updateMutation.isPending ? 'Guardando…' : 'Guardar cambios'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Register Dialog */}
      <Dialog open={isRegisterOpen} onOpenChange={setIsRegisterOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Registrar Parte</DialogTitle>
          </DialogHeader>

          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            {/* LegalName */}
            <div className="space-y-1.5">
              <Label htmlFor="legalName">Nombre Legal *</Label>
              <Input
                id="legalName"
                {...form.register('legalName')}
                placeholder="Razón social o nombre completo"
              />
              {form.formState.errors.legalName && (
                <p className="text-xs text-destructive">{form.formState.errors.legalName.message}</p>
              )}
            </div>

            {/* TradeName */}
            <div className="space-y-1.5">
              <Label htmlFor="tradeName">Nombre Comercial</Label>
              <Input
                id="tradeName"
                {...form.register('tradeName')}
                placeholder="Nombre de marca (opcional)"
              />
            </div>

            {/* PartyType + CountryCode */}
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Tipo *</Label>
                <Select
                  value={form.watch('partyType')}
                  onValueChange={v => form.setValue('partyType', v as 'Individual' | 'Organization')}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Organization">Organización</SelectItem>
                    <SelectItem value="Individual">Individual</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="countryCode">País (ISO) *</Label>
                <Input
                  id="countryCode"
                  {...form.register('countryCode')}
                  placeholder="MX, US, ES…"
                  maxLength={2}
                  className="uppercase"
                />
                {form.formState.errors.countryCode && (
                  <p className="text-xs text-destructive">{form.formState.errors.countryCode.message}</p>
                )}
              </div>
            </div>

            {/* FirstRoleType */}
            <div className="space-y-1.5">
              <Label>Rol Inicial *</Label>
              <Select
                value={form.watch('firstRoleType')}
                onValueChange={v => form.setValue('firstRoleType', v as PartyRoleType)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Customer">Cliente</SelectItem>
                  <SelectItem value="Supplier">Proveedor</SelectItem>
                  <SelectItem value="Employee">Empleado</SelectItem>
                  <SelectItem value="Contact">Contacto</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* TaxId */}
            <div className="space-y-1.5">
              <Label htmlFor="taxId">RFC / RUC / Tax ID</Label>
              <Input
                id="taxId"
                {...form.register('taxId')}
                placeholder="Número fiscal (opcional)"
              />
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsRegisterOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={registerMutation.isPending}>
                {registerMutation.isPending ? 'Guardando…' : 'Registrar'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
