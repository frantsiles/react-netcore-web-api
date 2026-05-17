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
import { Plus, Search, MoreHorizontal, Star, Package2, ChevronUp, ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
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

import { catalogService } from './catalogService'
import type { CatalogItemDto, PriceListDto, ItemType } from '@/types/erp/catalog'

// ── Schemas ───────────────────────────────────────────────────────────────────

const itemSchema = z.object({
  sku:             z.string().min(1, 'Requerido'),
  name:            z.string().min(2, 'Mínimo 2 caracteres'),
  description:     z.string().optional(),
  itemType:        z.enum(['Product', 'Service']),
  unitOfMeasure:   z.string().min(1, 'Requerido'),
  taxCategory:     z.string().min(1, 'Requerido'),
  defaultCurrency: z.string().length(3, 'Código de 3 letras'),
  countryCode:     z.string().length(2, 'Código de 2 letras'),
  trackInventory:  z.boolean().default(false),
})
type ItemForm = z.infer<typeof itemSchema>

const priceListSchema = z.object({
  name:         z.string().min(2, 'Mínimo 2 caracteres'),
  currencyCode: z.string().length(3, 'Código de 3 letras'),
  validFrom:    z.string().min(1, 'Requerido'),
  validTo:      z.string().optional(),
})
type PriceListForm = z.infer<typeof priceListSchema>

// ── Column helpers ────────────────────────────────────────────────────────────

const itemCol  = createColumnHelper<CatalogItemDto>()
const plCol    = createColumnHelper<PriceListDto>()

// ── Page ──────────────────────────────────────────────────────────────────────

export function CatalogPage() {
  const qc = useQueryClient()
  const [activeTab, setActiveTab] = useState('items')

  // Items filters
  const [appliedItemFilters, setAppliedItemFilters] = useState<{
    name?: string; sku?: string; itemType?: ItemType; isActive?: boolean
  }>({ isActive: true })
  const [inputName,     setInputName]     = useState('')
  const [inputSku,      setInputSku]      = useState('')
  const [inputType,     setInputType]     = useState('all')
  const [inputStatus,   setInputStatus]   = useState('active')

  // Dialog state
  const [isItemDialogOpen,  setIsItemDialogOpen]  = useState(false)
  const [isPLDialogOpen,    setIsPLDialogOpen]    = useState(false)

  // Sorting
  const [itemSorting, setItemSorting] = useState<SortingState>([])
  const [plSorting,   setPlSorting]   = useState<SortingState>([])

  // ── Queries ────────────────────────────────────────────────────────────────

  const { data: items, isLoading: itemsLoading } = useQuery({
    queryKey: ['catalog-items', appliedItemFilters],
    queryFn: () => catalogService.items.search({
      name:     appliedItemFilters.name,
      sku:      appliedItemFilters.sku,
      itemType: appliedItemFilters.itemType,
      isActive: appliedItemFilters.isActive,
      skip: 0, take: 100,
    }),
  })

  const { data: priceLists, isLoading: plLoading } = useQuery({
    queryKey: ['price-lists'],
    queryFn:  catalogService.priceLists.list,
  })

  // ── Mutations ──────────────────────────────────────────────────────────────

  const createItemMutation = useMutation({
    mutationFn: catalogService.items.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['catalog-items'] })
      setIsItemDialogOpen(false)
      toast.success('Artículo creado')
    },
    onError: () => toast.error('Error al crear el artículo'),
  })

  const deactivateItemMutation = useMutation({
    mutationFn: (id: string) => catalogService.items.deactivate(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['catalog-items'] })
      toast.success('Artículo desactivado')
    },
    onError: () => toast.error('Error al desactivar'),
  })

  const createPLMutation = useMutation({
    mutationFn: catalogService.priceLists.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['price-lists'] })
      setIsPLDialogOpen(false)
      toast.success('Lista de precios creada')
    },
    onError: () => toast.error('Error al crear la lista'),
  })

  const setDefaultPLMutation = useMutation({
    mutationFn: (id: string) => catalogService.priceLists.setDefault(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['price-lists'] })
      toast.success('Lista de precios predeterminada actualizada')
    },
    onError: () => toast.error('Error al actualizar'),
  })

  // ── Forms ──────────────────────────────────────────────────────────────────

  const itemForm = useForm<ItemForm>({
    resolver: zodResolver(itemSchema),
    defaultValues: {
      itemType: 'Product', defaultCurrency: 'USD',
      countryCode: 'US', trackInventory: false,
    },
  })

  const plForm = useForm<PriceListForm>({
    resolver: zodResolver(priceListSchema),
    defaultValues: { currencyCode: 'USD', validFrom: new Date().toISOString().slice(0, 10) },
  })

  const handleItemSearch = () => setAppliedItemFilters({
    name:     inputName  || undefined,
    sku:      inputSku   || undefined,
    itemType: inputType  !== 'all' ? (inputType as ItemType) : undefined,
    isActive: inputStatus === 'all' ? undefined : inputStatus === 'active',
  })

  const onItemSubmit = (v: ItemForm) => {
    createItemMutation.mutate({
      sku:             v.sku.toUpperCase(),
      name:            v.name,
      description:     v.description || undefined,
      itemType:        v.itemType,
      unitOfMeasure:   v.unitOfMeasure.toUpperCase(),
      taxCategory:     v.taxCategory,
      defaultCurrency: v.defaultCurrency.toUpperCase(),
      countryCode:     v.countryCode.toUpperCase(),
      trackInventory:  v.trackInventory,
    })
  }

  const onPLSubmit = (v: PriceListForm) => {
    createPLMutation.mutate({
      name:         v.name,
      currencyCode: v.currencyCode.toUpperCase(),
      validFrom:    v.validFrom,
      validTo:      v.validTo || undefined,
    })
  }

  // ── Table columns ──────────────────────────────────────────────────────────

  const itemColumns = [
    itemCol.accessor('sku', {
      header: 'SKU',
      cell: info => <span className="font-mono text-sm font-medium">{info.getValue()}</span>,
    }),
    itemCol.accessor('name', {
      header: 'Nombre',
      cell: info => (
        <div>
          <p className="font-medium">{info.getValue()}</p>
          {info.row.original.description && (
            <p className="text-xs text-muted-foreground truncate max-w-[200px]">
              {info.row.original.description}
            </p>
          )}
        </div>
      ),
    }),
    itemCol.accessor('itemType', {
      header: 'Tipo',
      cell: info => (
        <Badge variant={info.getValue() === 'Product' ? 'default' : 'secondary'}>
          {info.getValue() === 'Product' ? 'Producto' : 'Servicio'}
        </Badge>
      ),
    }),
    itemCol.accessor('unitOfMeasure', {
      header: 'UOM',
      cell: info => <span className="font-mono text-xs">{info.getValue()}</span>,
    }),
    itemCol.accessor('taxCategory', {
      header: 'Cat. fiscal',
      cell: info => <span className="text-sm text-muted-foreground">{info.getValue()}</span>,
    }),
    itemCol.accessor('trackInventory', {
      header: 'Inventario',
      cell: info => info.getValue()
        ? <Badge variant="outline" className="text-xs">Sí</Badge>
        : <span className="text-xs text-muted-foreground">No</span>,
    }),
    itemCol.accessor('isActive', {
      header: 'Estado',
      cell: info => info.getValue()
        ? <Badge variant="success">Activo</Badge>
        : <Badge variant="secondary">Inactivo</Badge>,
    }),
    itemCol.display({
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
            {row.original.isActive && (
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => deactivateItemMutation.mutate(row.original.catalogItemId)}
              >
                Desactivar
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    }),
  ]

  const plColumns = [
    plCol.accessor('name', {
      header: 'Nombre',
      cell: info => (
        <div className="flex items-center gap-2">
          {info.row.original.isDefault && (
            <Star className="h-3.5 w-3.5 fill-yellow-400 text-yellow-400" />
          )}
          <span className="font-medium">{info.getValue()}</span>
        </div>
      ),
    }),
    plCol.accessor('currencyCode', {
      header: 'Moneda',
      cell: info => <span className="font-mono text-sm">{info.getValue()}</span>,
    }),
    plCol.accessor('validFrom', {
      header: 'Vigencia desde',
      cell: info => <span className="text-sm">{info.getValue()}</span>,
    }),
    plCol.accessor('validTo', {
      header: 'Vigencia hasta',
      cell: info => <span className="text-sm">{info.getValue() ?? '—'}</span>,
    }),
    plCol.accessor('status', {
      header: 'Estado',
      cell: info => (
        <Badge variant={info.getValue() === 'Active' ? 'success' : 'secondary'}>
          {info.getValue() === 'Active' ? 'Activa' : 'Inactiva'}
        </Badge>
      ),
    }),
    plCol.display({
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
            {!row.original.isDefault && (
              <DropdownMenuItem
                onClick={() => setDefaultPLMutation.mutate(row.original.priceListId)}
              >
                <Star className="mr-2 h-4 w-4" />
                Poner como predeterminada
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    }),
  ]

  const itemTable = useReactTable({
    data: items ?? [],
    columns: itemColumns,
    state: { sorting: itemSorting },
    onSortingChange: setItemSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  const plTable = useReactTable({
    data: priceLists ?? [],
    columns: plColumns,
    state: { sorting: plSorting },
    onSortingChange: setPlSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Catálogo</h1>
          <p className="text-sm text-muted-foreground">Artículos y listas de precios</p>
        </div>
        {activeTab === 'items' ? (
          <Button onClick={() => { itemForm.reset(); setIsItemDialogOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />
            Nuevo artículo
          </Button>
        ) : (
          <Button onClick={() => { plForm.reset(); setIsPLDialogOpen(true) }}>
            <Plus className="mr-2 h-4 w-4" />
            Nueva lista
          </Button>
        )}
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="items">
            <Package2 className="mr-2 h-4 w-4" />
            Artículos
          </TabsTrigger>
          <TabsTrigger value="pricelists">
            <Star className="mr-2 h-4 w-4" />
            Listas de precios
          </TabsTrigger>
        </TabsList>

        {/* ── Items Tab ── */}
        <TabsContent value="items" className="space-y-4">
          <div className="flex flex-wrap items-end gap-3">
            <Input
              placeholder="Buscar por nombre..."
              value={inputName}
              onChange={e => setInputName(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleItemSearch()}
              className="flex-1 min-w-[160px]"
            />
            <Input
              placeholder="SKU..."
              value={inputSku}
              onChange={e => setInputSku(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleItemSearch()}
              className="w-[120px]"
            />
            <Select value={inputType} onValueChange={setInputType}>
              <SelectTrigger className="w-[140px]">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="Product">Productos</SelectItem>
                <SelectItem value="Service">Servicios</SelectItem>
              </SelectContent>
            </Select>
            <Select value={inputStatus} onValueChange={setInputStatus}>
              <SelectTrigger className="w-[130px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="active">Activos</SelectItem>
                <SelectItem value="inactive">Inactivos</SelectItem>
                <SelectItem value="all">Todos</SelectItem>
              </SelectContent>
            </Select>
            <Button variant="outline" onClick={handleItemSearch}>
              <Search className="mr-2 h-4 w-4" />
              Buscar
            </Button>
          </div>

          <DataTable table={itemTable} columns={itemColumns} isLoading={itemsLoading} />
        </TabsContent>

        {/* ── Price Lists Tab ── */}
        <TabsContent value="pricelists" className="space-y-4">
          <DataTable table={plTable} columns={plColumns} isLoading={plLoading} />
        </TabsContent>
      </Tabs>

      {/* ── Create Item Dialog ── */}
      <Dialog open={isItemDialogOpen} onOpenChange={setIsItemDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Nuevo Artículo</DialogTitle>
          </DialogHeader>
          <form onSubmit={itemForm.handleSubmit(onItemSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="sku">SKU *</Label>
                <Input id="sku" {...itemForm.register('sku')}
                  placeholder="PROD-001" className="uppercase" />
                {itemForm.formState.errors.sku && (
                  <p className="text-xs text-destructive">{itemForm.formState.errors.sku.message}</p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label>Tipo *</Label>
                <Select
                  value={itemForm.watch('itemType')}
                  onValueChange={v => itemForm.setValue('itemType', v as ItemType)}
                >
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Product">Producto</SelectItem>
                    <SelectItem value="Service">Servicio</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="item-name">Nombre *</Label>
              <Input id="item-name" {...itemForm.register('name')} placeholder="Nombre del artículo" />
              {itemForm.formState.errors.name && (
                <p className="text-xs text-destructive">{itemForm.formState.errors.name.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="desc">Descripción</Label>
              <Input id="desc" {...itemForm.register('description')} placeholder="Descripción (opcional)" />
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="uom">UOM *</Label>
                <Input id="uom" {...itemForm.register('unitOfMeasure')}
                  placeholder="PZA" className="uppercase" />
                {itemForm.formState.errors.unitOfMeasure && (
                  <p className="text-xs text-destructive">{itemForm.formState.errors.unitOfMeasure.message}</p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="currency">Moneda *</Label>
                <Input id="currency" {...itemForm.register('defaultCurrency')}
                  placeholder="USD" maxLength={3} className="uppercase" />
                {itemForm.formState.errors.defaultCurrency && (
                  <p className="text-xs text-destructive">{itemForm.formState.errors.defaultCurrency.message}</p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="item-country">País *</Label>
                <Input id="item-country" {...itemForm.register('countryCode')}
                  placeholder="US" maxLength={2} className="uppercase" />
                {itemForm.formState.errors.countryCode && (
                  <p className="text-xs text-destructive">{itemForm.formState.errors.countryCode.message}</p>
                )}
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Categoría fiscal *</Label>
              <Select
                value={itemForm.watch('taxCategory')}
                onValueChange={v => itemForm.setValue('taxCategory', v)}
              >
                <SelectTrigger><SelectValue placeholder="Seleccionar..." /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Standard">Estándar</SelectItem>
                  <SelectItem value="ZeroRated">Tasa cero</SelectItem>
                  <SelectItem value="Exempt">Exento</SelectItem>
                </SelectContent>
              </Select>
              {itemForm.formState.errors.taxCategory && (
                <p className="text-xs text-destructive">{itemForm.formState.errors.taxCategory.message}</p>
              )}
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="trackInv"
                {...itemForm.register('trackInventory')}
                className="h-4 w-4 rounded border-input"
              />
              <Label htmlFor="trackInv" className="cursor-pointer">
                Controlar inventario
              </Label>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsItemDialogOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={createItemMutation.isPending}>
                {createItemMutation.isPending ? 'Guardando…' : 'Crear artículo'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Create Price List Dialog ── */}
      <Dialog open={isPLDialogOpen} onOpenChange={setIsPLDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Nueva Lista de Precios</DialogTitle>
          </DialogHeader>
          <form onSubmit={plForm.handleSubmit(onPLSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="pl-name">Nombre *</Label>
              <Input id="pl-name" {...plForm.register('name')} placeholder="Ej. Lista General 2025" />
              {plForm.formState.errors.name && (
                <p className="text-xs text-destructive">{plForm.formState.errors.name.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="pl-currency">Moneda *</Label>
              <Input id="pl-currency" {...plForm.register('currencyCode')}
                placeholder="USD" maxLength={3} className="uppercase w-28" />
              {plForm.formState.errors.currencyCode && (
                <p className="text-xs text-destructive">{plForm.formState.errors.currencyCode.message}</p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="pl-from">Vigencia desde *</Label>
                <Input id="pl-from" type="date" {...plForm.register('validFrom')} />
                {plForm.formState.errors.validFrom && (
                  <p className="text-xs text-destructive">{plForm.formState.errors.validFrom.message}</p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="pl-to">Vigencia hasta</Label>
                <Input id="pl-to" type="date" {...plForm.register('validTo')} />
              </div>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsPLDialogOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={createPLMutation.isPending}>
                {createPLMutation.isPending ? 'Guardando…' : 'Crear lista'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}

// ── Shared DataTable component ────────────────────────────────────────────────

function DataTable<T>({
  table,
  columns,
  isLoading,
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
            Array.from({ length: 4 }).map((_, i) => (
              <TableRow key={i}>
                {(columns as unknown[]).map((_, j) => (
                  <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                ))}
              </TableRow>
            ))
          ) : table.getRowModel().rows.length === 0 ? (
            <TableRow>
              <TableCell
                colSpan={columns.length as number}
                className="h-24 text-center text-muted-foreground"
              >
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
