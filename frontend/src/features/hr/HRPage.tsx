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
import { Plus, Users, Building2, MoreHorizontal, ChevronUp, ChevronDown } from 'lucide-react'

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

import { hrService, type EmployeeDto, type DepartmentDto, type EmployeeStatus } from './hrService'

const STATUS_LABELS: Record<EmployeeStatus, string> = { Active: 'Activo', OnLeave: 'Permiso', Terminated: 'Terminado' }
const STATUS_VARIANT: Record<EmployeeStatus, 'success' | 'default' | 'secondary'> = {
  Active: 'success', OnLeave: 'default', Terminated: 'secondary',
}

const hireSchema = z.object({
  firstName:      z.string().min(1, 'Requerido'),
  lastName:       z.string().min(1, 'Requerido'),
  email:          z.string().email('Email inválido'),
  phone:          z.string().optional(),
  departmentId:   z.string().uuid('UUID requerido'),
  jobTitle:       z.string().min(1, 'Requerido'),
  employmentType: z.enum(['FullTime', 'PartTime', 'Contractor', 'Temporary']),
  hireDate:       z.string().min(1, 'Requerido'),
  managerEmployeeId: z.string().optional(),
})
type HireForm = z.infer<typeof hireSchema>

const terminateSchema = z.object({
  terminationDate: z.string().min(1, 'Requerido'),
  reason:          z.string().min(3, 'Mínimo 3 caracteres'),
})
type TerminateForm = z.infer<typeof terminateSchema>

const deptSchema = z.object({
  code:               z.string().min(1, 'Requerido'),
  name:               z.string().min(1, 'Requerido'),
  parentDepartmentId: z.string().optional(),
  costCenter:         z.string().optional(),
})
type DeptForm = z.infer<typeof deptSchema>

const eCol = createColumnHelper<EmployeeDto>()
const dCol = createColumnHelper<DepartmentDto>()

export function HRPage() {
  const qc = useQueryClient()
  const [activeTab, setActiveTab] = useState('employees')
  const [statusFilter, setStatusFilter] = useState('all')
  const [eSorting, setESorting] = useState<SortingState>([])
  const [dSorting, setDSorting] = useState<SortingState>([])

  const [isHireOpen,      setIsHireOpen]      = useState(false)
  const [isTerminateOpen, setIsTerminateOpen]  = useState(false)
  const [isDeptOpen,      setIsDeptOpen]       = useState(false)
  const [selectedEmpId,   setSelectedEmpId]    = useState<string | null>(null)

  const { data: employees, isLoading: empLoading } = useQuery({
    queryKey: ['employees', statusFilter],
    queryFn: () => hrService.employees.list(undefined, statusFilter !== 'all' ? statusFilter : undefined),
  })

  const { data: departments, isLoading: deptLoading } = useQuery({
    queryKey: ['departments'],
    queryFn: () => hrService.departments.list(),
  })

  const hireMut = useMutation({
    mutationFn: hrService.employees.hire,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['employees'] }); setIsHireOpen(false); toast.success('Empleado contratado') },
    onError: () => toast.error('Error al contratar'),
  })

  const terminateMut = useMutation({
    mutationFn: ({ id, terminationDate, reason }: { id: string; terminationDate: string; reason: string }) =>
      hrService.employees.terminate(id, terminationDate, reason),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['employees'] }); setIsTerminateOpen(false); toast.success('Empleado terminado') },
    onError: () => toast.error('Error al terminar'),
  })

  const createDeptMut = useMutation({
    mutationFn: hrService.departments.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['departments'] }); setIsDeptOpen(false); toast.success('Departamento creado') },
    onError: () => toast.error('Error al crear departamento'),
  })

  const hireForm      = useForm<HireForm>({      resolver: zodResolver(hireSchema),     defaultValues: { employmentType: 'FullTime' } })
  const terminateForm = useForm<TerminateForm>({ resolver: zodResolver(terminateSchema) })
  const deptForm      = useForm<DeptForm>({      resolver: zodResolver(deptSchema) })

  const openTerminateDialog = (id: string) => {
    setSelectedEmpId(id)
    terminateForm.reset()
    setIsTerminateOpen(true)
  }

  const getDeptName = (deptId: string) => departments?.find(d => d.id === deptId)?.name ?? deptId.slice(0, 8) + '…'

  const empCols = [
    eCol.accessor('employeeNumber', { header: 'N° Emp', cell: i => <span className="font-mono text-sm">{i.getValue()}</span> }),
    eCol.accessor('fullName', { header: 'Nombre' }),
    eCol.accessor('jobTitle', { header: 'Puesto', cell: i => <span className="text-sm">{i.getValue()}</span> }),
    eCol.accessor('departmentId', { header: 'Departamento', cell: i => <span className="text-sm">{getDeptName(i.getValue())}</span> }),
    eCol.accessor('employmentType', { header: 'Tipo', cell: i => <Badge variant="secondary">{i.getValue()}</Badge> }),
    eCol.accessor('status', { header: 'Estado', cell: i => <Badge variant={STATUS_VARIANT[i.getValue()]}>{STATUS_LABELS[i.getValue()]}</Badge> }),
    eCol.accessor('hireDate', { header: 'Contratado', cell: i => <span className="text-sm">{i.getValue()}</span> }),
    eCol.display({
      id: 'actions', header: '',
      cell: ({ row }) => row.original.status === 'Active' ? (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8"><MoreHorizontal className="h-4 w-4" /></Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuSeparator />
            <DropdownMenuItem className="text-destructive" onClick={() => openTerminateDialog(row.original.id)}>Terminar contrato</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ) : null,
    }),
  ]

  const deptCols = [
    dCol.accessor('code', { header: 'Código', cell: i => <span className="font-mono text-sm">{i.getValue()}</span> }),
    dCol.accessor('name', { header: 'Nombre' }),
    dCol.accessor('costCenter', { header: 'Centro de costo', cell: i => <span className="text-sm text-muted-foreground">{i.getValue() ?? '—'}</span> }),
    dCol.accessor('isActive', { header: 'Estado', cell: i => <Badge variant={i.getValue() ? 'success' : 'secondary'}>{i.getValue() ? 'Activo' : 'Inactivo'}</Badge> }),
  ]

  const eTable = useReactTable({ data: employees ?? [], columns: empCols, state: { sorting: eSorting }, onSortingChange: setESorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })
  const dTable = useReactTable({ data: departments ?? [], columns: deptCols, state: { sorting: dSorting }, onSortingChange: setDSorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Recursos Humanos</h1>
          <p className="text-sm text-muted-foreground">Empleados y estructura organizacional</p>
        </div>
        {activeTab === 'employees' ? (
          <Button onClick={() => { hireForm.reset({ employmentType: 'FullTime' }); setIsHireOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />Contratar empleado
          </Button>
        ) : (
          <Button onClick={() => { deptForm.reset(); setIsDeptOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />Nuevo departamento
          </Button>
        )}
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="employees"><Users className="mr-2 h-4 w-4" />Empleados</TabsTrigger>
          <TabsTrigger value="departments"><Building2 className="mr-2 h-4 w-4" />Departamentos</TabsTrigger>
        </TabsList>

        <TabsContent value="employees" className="space-y-4">
          <div className="flex gap-3">
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-[160px]"><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="Active">Activos</SelectItem>
                <SelectItem value="OnLeave">En permiso</SelectItem>
                <SelectItem value="Terminated">Terminados</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <SimpleTable table={eTable} columns={empCols} isLoading={empLoading} empty="No hay empleados registrados" />
        </TabsContent>

        <TabsContent value="departments" className="space-y-4">
          <SimpleTable table={dTable} columns={deptCols} isLoading={deptLoading} empty="No hay departamentos registrados" />
        </TabsContent>
      </Tabs>

      {/* Hire Dialog */}
      <Dialog open={isHireOpen} onOpenChange={setIsHireOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Contratar Empleado</DialogTitle></DialogHeader>
          <form onSubmit={hireForm.handleSubmit(v => hireMut.mutate({ ...v, phone: v.phone || undefined, managerEmployeeId: v.managerEmployeeId || undefined }))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Nombre *</Label>
                <Input {...hireForm.register('firstName')} placeholder="Juan" />
              </div>
              <div className="space-y-1.5">
                <Label>Apellido *</Label>
                <Input {...hireForm.register('lastName')} placeholder="García" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Email *</Label>
                <Input type="email" {...hireForm.register('email')} placeholder="juan@empresa.com" />
                {hireForm.formState.errors.email && <p className="text-xs text-destructive">{hireForm.formState.errors.email.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Teléfono</Label>
                <Input {...hireForm.register('phone')} placeholder="+52 55 1234 5678" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>ID Departamento *</Label>
                <Input {...hireForm.register('departmentId')} placeholder="UUID del departamento" />
                {hireForm.formState.errors.departmentId && <p className="text-xs text-destructive">{hireForm.formState.errors.departmentId.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Puesto *</Label>
                <Input {...hireForm.register('jobTitle')} placeholder="Analista" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Tipo de empleo *</Label>
                <Select onValueChange={v => hireForm.setValue('employmentType', v as HireForm['employmentType'])} defaultValue="FullTime">
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="FullTime">Tiempo completo</SelectItem>
                    <SelectItem value="PartTime">Medio tiempo</SelectItem>
                    <SelectItem value="Contractor">Contratista</SelectItem>
                    <SelectItem value="Temporary">Temporal</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Fecha de contratación *</Label>
                <Input type="date" {...hireForm.register('hireDate')} />
              </div>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsHireOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={hireMut.isPending}>{hireMut.isPending ? 'Contratando…' : 'Contratar'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Terminate Dialog */}
      <Dialog open={isTerminateOpen} onOpenChange={setIsTerminateOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader><DialogTitle>Terminar Contrato</DialogTitle></DialogHeader>
          <form onSubmit={terminateForm.handleSubmit(v => terminateMut.mutate({ id: selectedEmpId!, ...v }))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Fecha de término *</Label>
              <Input type="date" {...terminateForm.register('terminationDate')} />
            </div>
            <div className="space-y-1.5">
              <Label>Motivo *</Label>
              <Input {...terminateForm.register('reason')} placeholder="Renuncia voluntaria" />
              {terminateForm.formState.errors.reason && <p className="text-xs text-destructive">{terminateForm.formState.errors.reason.message}</p>}
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsTerminateOpen(false)}>Cancelar</Button>
              <Button type="submit" variant="destructive" disabled={terminateMut.isPending}>
                {terminateMut.isPending ? 'Procesando…' : 'Terminar contrato'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Create Department Dialog */}
      <Dialog open={isDeptOpen} onOpenChange={setIsDeptOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader><DialogTitle>Nuevo Departamento</DialogTitle></DialogHeader>
          <form onSubmit={deptForm.handleSubmit(v => createDeptMut.mutate({ ...v, parentDepartmentId: v.parentDepartmentId || undefined, costCenter: v.costCenter || undefined }))} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Código *</Label>
                <Input {...deptForm.register('code')} placeholder="TI" />
              </div>
              <div className="space-y-1.5">
                <Label>Nombre *</Label>
                <Input {...deptForm.register('name')} placeholder="Tecnología" />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Centro de costo</Label>
              <Input {...deptForm.register('costCenter')} placeholder="CC-TI-001" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsDeptOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createDeptMut.isPending}>{createDeptMut.isPending ? 'Creando…' : 'Crear'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function SimpleTable<T>({ table, columns, isLoading, empty }: {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  table: ReturnType<typeof useReactTable<T>>
  columns: unknown[]; isLoading: boolean; empty: string
}) {
  return (
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
            ? Array.from({ length: 4 }).map((_, i) => <TableRow key={i}>{(columns as unknown[]).map((_, j) => <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>)}</TableRow>)
            : table.getRowModel().rows.length === 0
              ? <TableRow><TableCell colSpan={columns.length as number} className="h-24 text-center text-muted-foreground">{empty}</TableCell></TableRow>
              : table.getRowModel().rows.map(row => (
                  <TableRow key={row.id}>
                    {row.getVisibleCells().map(cell => <TableCell key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</TableCell>)}
                  </TableRow>
                ))
          }
        </TableBody>
      </Table>
    </div>
  )
}
