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
import { Plus, Users, Building2, MoreHorizontal, ChevronUp, ChevronDown, ListChecks, CheckCircle2, ChevronRight, Download } from 'lucide-react'

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
import { payrollService, type PayrollRunDto } from './payrollService'

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

const runSchema = z.object({
  periodType:  z.enum(['Monthly', 'Biweekly']),
  periodStart: z.string().min(1, 'Requerido'),
  periodEnd:   z.string().min(1, 'Requerido'),
  currencyCode: z.string().length(3, '3 letras').default('CRC'),
})
type RunForm = z.infer<typeof runSchema>

const eCol = createColumnHelper<EmployeeDto>()
const dCol = createColumnHelper<DepartmentDto>()
const pCol = createColumnHelper<PayrollRunDto>()

export function HRPage() {
  const qc = useQueryClient()
  const [activeTab, setActiveTab] = useState('employees')
  const [statusFilter, setStatusFilter] = useState('all')
  const [eSorting, setESorting] = useState<SortingState>([])
  const [dSorting, setDSorting] = useState<SortingState>([])

  const [isHireOpen,      setIsHireOpen]      = useState(false)
  const [isTerminateOpen, setIsTerminateOpen]  = useState(false)
  const [isDeptOpen,      setIsDeptOpen]       = useState(false)
  const [isRunOpen,       setIsRunOpen]        = useState(false)
  const [selectedEmpId,   setSelectedEmpId]    = useState<string | null>(null)
  const [selectedRun,     setSelectedRun]      = useState<PayrollRunDto | null>(null)

  const { data: payrollRuns, isLoading: runLoading } = useQuery({
    queryKey: ['payrollRuns'],
    queryFn: () => payrollService.runs.list(),
    enabled: activeTab === 'payroll',
  })

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

  const createRunMut = useMutation({
    mutationFn: payrollService.runs.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['payrollRuns'] }); setIsRunOpen(false); toast.success('Planilla generada') },
    onError: () => toast.error('Error al generar planilla'),
  })

  const confirmRunMut = useMutation({
    mutationFn: (id: string) => payrollService.runs.confirm(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['payrollRuns'] }); toast.success('Planilla confirmada') },
    onError: () => toast.error('Error al confirmar planilla'),
  })

  const hireForm      = useForm<HireForm>({      resolver: zodResolver(hireSchema),     defaultValues: { employmentType: 'FullTime' } })
  const terminateForm = useForm<TerminateForm>({ resolver: zodResolver(terminateSchema) })
  const deptForm      = useForm<DeptForm>({      resolver: zodResolver(deptSchema) })
  const runForm       = useForm<RunForm>({       resolver: zodResolver(runSchema),      defaultValues: { periodType: 'Monthly', currencyCode: 'CRC' } })

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

  const RUN_STATUS: Record<string, string> = { Draft: 'Borrador', Confirmed: 'Confirmada', Paid: 'Pagada' }
  const RUN_VARIANT: Record<string, 'default' | 'success' | 'secondary'> = { Draft: 'secondary', Confirmed: 'default', Paid: 'success' }
  const fmt = (n: number) => n.toLocaleString('es-CR', { style: 'currency', currency: 'CRC', minimumFractionDigits: 0 })

  const runCols = [
    pCol.accessor('runNumber', { header: 'N° Planilla', cell: i => <span className="font-mono text-sm">{i.getValue()}</span> }),
    pCol.accessor('periodType', { header: 'Período', cell: i => <span className="text-sm">{i.getValue() === 'Monthly' ? 'Mensual' : 'Quincenal'}</span> }),
    pCol.accessor('periodStart', { header: 'Inicio', cell: i => <span className="text-sm">{i.getValue()}</span> }),
    pCol.accessor('periodEnd', { header: 'Fin', cell: i => <span className="text-sm">{i.getValue()}</span> }),
    pCol.accessor('employeeCount', { header: 'Empleados', cell: i => <span className="text-sm text-center">{i.getValue()}</span> }),
    pCol.accessor('totalNet', { header: 'Total Neto', cell: i => <span className="text-sm font-medium">{fmt(i.getValue())}</span> }),
    pCol.accessor('totalEmployerCost', { header: 'Costo Patronal', cell: i => <span className="text-sm text-muted-foreground">{fmt(i.getValue())}</span> }),
    pCol.accessor('status', { header: 'Estado', cell: i => <Badge variant={RUN_VARIANT[i.getValue()]}>{RUN_STATUS[i.getValue()] ?? i.getValue()}</Badge> }),
    pCol.display({
      id: 'actions', header: '',
      cell: ({ row }) => (
        <div className="flex gap-1">
          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => setSelectedRun(row.original)} title="Ver detalle">
            <ChevronRight className="h-4 w-4" />
          </Button>
          {row.original.status === 'Draft' && (
            <Button variant="ghost" size="icon" className="h-8 w-8 text-green-600" onClick={() => confirmRunMut.mutate(row.original.id)} title="Confirmar">
              <CheckCircle2 className="h-4 w-4" />
            </Button>
          )}
        </div>
      ),
    }),
  ]

  const eTable = useReactTable({ data: employees ?? [], columns: empCols, state: { sorting: eSorting }, onSortingChange: setESorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })
  const dTable = useReactTable({ data: departments ?? [], columns: deptCols, state: { sorting: dSorting }, onSortingChange: setDSorting, getCoreRowModel: getCoreRowModel(), getSortedRowModel: getSortedRowModel() })
  const pTable = useReactTable({ data: payrollRuns ?? [], columns: runCols, getCoreRowModel: getCoreRowModel() })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Recursos Humanos</h1>
          <p className="text-sm text-muted-foreground">Empleados y estructura organizacional</p>
        </div>
        {activeTab === 'employees' && (
          <Button onClick={() => { hireForm.reset({ employmentType: 'FullTime' }); setIsHireOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />Contratar empleado
          </Button>
        )}
        {activeTab === 'departments' && (
          <Button onClick={() => { deptForm.reset(); setIsDeptOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />Nuevo departamento
          </Button>
        )}
        {activeTab === 'payroll' && (
          <Button onClick={() => { runForm.reset({ periodType: 'Monthly', currencyCode: 'CRC' }); setIsRunOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />Correr planilla
          </Button>
        )}
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="employees"><Users className="mr-2 h-4 w-4" />Empleados</TabsTrigger>
          <TabsTrigger value="departments"><Building2 className="mr-2 h-4 w-4" />Departamentos</TabsTrigger>
          <TabsTrigger value="payroll"><ListChecks className="mr-2 h-4 w-4" />Planilla</TabsTrigger>
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

        <TabsContent value="payroll" className="space-y-4">
          <SimpleTable table={pTable} columns={runCols} isLoading={runLoading} empty="No hay planillas. Haga clic en 'Correr planilla' para generar la primera." />
        </TabsContent>
      </Tabs>

      {/* Payroll Run Detail Sheet */}
      {selectedRun && (
        <Dialog open={!!selectedRun} onOpenChange={() => setSelectedRun(null)}>
          <DialogContent className="sm:max-w-4xl max-h-[80vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>{selectedRun.runNumber} — Detalle de planilla</DialogTitle>
            </DialogHeader>
            <div className="grid grid-cols-3 gap-4 text-sm mb-4">
              <div><span className="text-muted-foreground">Período: </span>{selectedRun.periodStart} → {selectedRun.periodEnd}</div>
              <div><span className="text-muted-foreground">Empleados: </span>{selectedRun.employeeCount}</div>
              <div><span className="text-muted-foreground">Estado: </span><Badge variant={RUN_VARIANT[selectedRun.status]}>{RUN_STATUS[selectedRun.status]}</Badge></div>
              <div><span className="text-muted-foreground">Total Bruto: </span><strong>{selectedRun.totalGross.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</strong></div>
              <div><span className="text-muted-foreground">Total Neto: </span><strong>{selectedRun.totalNet.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</strong></div>
              <div><span className="text-muted-foreground">Costo Total: </span><strong>{selectedRun.totalEmployerCost.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</strong></div>
            </div>
            <div className="rounded-md border overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Empleado</TableHead>
                    <TableHead className="text-right">Bruto</TableHead>
                    <TableHead className="text-right">CCSS</TableHead>
                    <TableHead className="text-right">Renta</TableHead>
                    <TableHead className="text-right">B.Popular</TableHead>
                    <TableHead className="text-right">Neto</TableHead>
                    <TableHead className="text-right">C.Patronal</TableHead>
                    <TableHead className="w-10"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {selectedRun.entries.map(e => (
                    <TableRow key={e.id}>
                      <TableCell><div className="font-medium text-sm">{e.employeeName}</div><div className="text-xs text-muted-foreground">{e.employeeNumber}</div></TableCell>
                      <TableCell className="text-right text-sm">{e.totalGross.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</TableCell>
                      <TableCell className="text-right text-sm text-red-600">{e.ccssEmployee.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</TableCell>
                      <TableCell className="text-right text-sm text-red-600">{e.incomeTax.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</TableCell>
                      <TableCell className="text-right text-sm text-red-600">{e.bancoPopular.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</TableCell>
                      <TableCell className="text-right text-sm font-semibold">{e.netPay.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</TableCell>
                      <TableCell className="text-right text-sm text-muted-foreground">{e.totalEmployerContribution.toLocaleString('es-CR', { minimumFractionDigits: 2 })}</TableCell>
                      <TableCell>
                        <Button
                          variant="ghost" size="icon" className="h-7 w-7"
                          title="Descargar recibo"
                          onClick={() => payrollService.runs.downloadPaystub(
                            selectedRun.id, e.id,
                            `recibo-${selectedRun.runNumber}-${e.employeeNumber}.pdf`
                          )}
                        >
                          <Download className="h-3.5 w-3.5" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            <DialogFooter>
              {selectedRun.status === 'Draft' && (
                <Button onClick={() => { confirmRunMut.mutate(selectedRun.id); setSelectedRun(null) }} disabled={confirmRunMut.isPending}>
                  <CheckCircle2 className="mr-2 h-4 w-4" />Confirmar planilla
                </Button>
              )}
              <Button variant="outline" onClick={() => setSelectedRun(null)}>Cerrar</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}

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

      {/* Create Payroll Run Dialog */}
      <Dialog open={isRunOpen} onOpenChange={setIsRunOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader><DialogTitle>Correr Planilla</DialogTitle></DialogHeader>
          <form onSubmit={runForm.handleSubmit(v => createRunMut.mutate(v))} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Tipo de período *</Label>
              <Select onValueChange={v => runForm.setValue('periodType', v as RunForm['periodType'])} defaultValue="Monthly">
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Monthly">Mensual</SelectItem>
                  <SelectItem value="Biweekly">Quincenal</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Inicio del período *</Label>
                <Input type="date" {...runForm.register('periodStart')} />
              </div>
              <div className="space-y-1.5">
                <Label>Fin del período *</Label>
                <Input type="date" {...runForm.register('periodEnd')} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Moneda</Label>
              <Input {...runForm.register('currencyCode')} placeholder="CRC" maxLength={3} className="uppercase" />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsRunOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createRunMut.isPending}>{createRunMut.isPending ? 'Generando…' : 'Generar planilla'}</Button>
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
