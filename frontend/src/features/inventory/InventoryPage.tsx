import { useState, useMemo } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  flexRender,
  createColumnHelper,
  type SortingState,
} from "@tanstack/react-table";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import {
  Plus,
  MoreHorizontal,
  Warehouse,
  PackageSearch,
  ChevronUp,
  ChevronDown,
  AlertTriangle,
  FileDown,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import api from "@/services/api";
import { inventoryService } from "./inventoryService";
import type { InventoryItemDto, WarehouseDto } from "@/types/erp/inventory";
import { CatalogItemPicker } from "@/components/pickers/CatalogItemPicker";
import { WarehousePicker } from "@/components/pickers/WarehousePicker";

// ── Schemas ───────────────────────────────────────────────────────────────────

const receiveSchema = z.object({
  catalogItemId: z.string().uuid("UUID requerido"),
  warehouseId: z.string().uuid("UUID requerido"),
  sku: z.string().min(1, "Requerido"),
  quantity: z.number().positive("> 0"),
  referenceNumber: z.string().optional(),
  notes: z.string().optional(),
  reorderPoint: z.number().min(0).optional(),
});
type ReceiveForm = z.infer<typeof receiveSchema>;

const adjustSchema = z.object({
  delta: z.number().refine((v) => v !== 0, "No puede ser 0"),
  reason: z.string().min(3, "Mínimo 3 caracteres"),
  notes: z.string().optional(),
});
type AdjustForm = z.infer<typeof adjustSchema>;

const warehouseSchema = z.object({
  code: z.string().min(1, "Requerido"),
  name: z.string().min(1, "Requerido"),
  address: z.string().optional(),
});
type WarehouseForm = z.infer<typeof warehouseSchema>;

// ── Column helpers ────────────────────────────────────────────────────────────

const iCol = createColumnHelper<InventoryItemDto>();
const wCol = createColumnHelper<WarehouseDto>();

// ── Page ──────────────────────────────────────────────────────────────────────

export function InventoryPage() {
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState("items");
  const [skuFilter, setSkuFilter] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [iSorting, setISorting] = useState<SortingState>([]);
  const [wSorting, setWSorting] = useState<SortingState>([]);

  const [isReceiveOpen, setIsReceiveOpen] = useState(false);
  const [isAdjustOpen, setIsAdjustOpen] = useState(false);
  const [isWHOpen, setIsWHOpen] = useState(false);
  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);

  // ── Queries ────────────────────────────────────────────────────────────────

  const { data: items, isLoading: itemsLoading } = useQuery({
    queryKey: ["inventory", skuFilter, lowStockOnly],
    queryFn: () =>
      inventoryService.items.search({
        sku: skuFilter || undefined,
        belowReorderPoint: lowStockOnly || undefined,
        skip: 0,
        take: 100,
      }),
  });

  const { data: warehouses, isLoading: whLoading } = useQuery({
    queryKey: ["warehouses"],
    queryFn: inventoryService.warehouses.list,
  });

  // ── Mutations ──────────────────────────────────────────────────────────────

  const receiveMut = useMutation({
    mutationFn: inventoryService.items.receive,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory"] });
      setIsReceiveOpen(false);
      toast.success("Stock recibido");
    },
    onError: () => toast.error("Error al recibir stock"),
  });

  const adjustMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: AdjustForm }) =>
      inventoryService.items.adjust(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory"] });
      setIsAdjustOpen(false);
      toast.success("Ajuste registrado");
    },
    onError: () => toast.error("Error al ajustar stock"),
  });

  const createWHMut = useMutation({
    mutationFn: inventoryService.warehouses.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["warehouses"] });
      setIsWHOpen(false);
      toast.success("Almacén creado");
    },
    onError: () => toast.error("Error al crear almacén"),
  });

  // ── Forms ──────────────────────────────────────────────────────────────────

  const receiveForm = useForm<ReceiveForm>({
    resolver: zodResolver(receiveSchema),
    defaultValues: { quantity: 1 },
  });
  const adjustForm = useForm<AdjustForm>({
    resolver: zodResolver(adjustSchema),
  });
  const whForm = useForm<WarehouseForm>({
    resolver: zodResolver(warehouseSchema),
  });

  const openAdjustDialog = (id: string) => {
    setSelectedItemId(id);
    adjustForm.reset();
    setIsAdjustOpen(true);
  };

  const warehouseMap = useMemo(
    () => new Map(warehouses?.map((w) => [w.id, w.name]) ?? []),
    [warehouses],
  );

  // ── Columns ───────────────────────────────────────────────────────────────

  const itemColumns = useMemo(
    () => [
      iCol.accessor("sku", {
        header: "SKU",
        cell: (i) => (
          <span className="font-mono text-sm font-medium">{i.getValue()}</span>
        ),
      }),
      iCol.accessor("warehouseId", {
        header: "Almacén",
        cell: (i) => {
          const name = warehouseMap.get(i.getValue());
          return (
            <span className="text-sm">
              {name ?? i.getValue().slice(0, 8) + "…"}
            </span>
          );
        },
      }),
      iCol.accessor("quantityOnHand", {
        header: "En existencia",
        cell: (i) => <span className="font-medium">{i.getValue()}</span>,
      }),
      iCol.accessor("quantityReserved", {
        header: "Reservado",
        cell: (i) => (
          <span className="text-sm text-muted-foreground">{i.getValue()}</span>
        ),
      }),
      iCol.accessor("quantityAvailable", {
        header: "Disponible",
        cell: (i) => (
          <span className="font-medium text-green-700 dark:text-green-400">
            {i.getValue()}
          </span>
        ),
      }),
      iCol.accessor("reorderPoint", {
        header: "Punto reorden",
        cell: (i) =>
          i.getValue() != null ? (
            <span className="text-sm">{i.getValue()}</span>
          ) : (
            <span className="text-muted-foreground">—</span>
          ),
      }),
      iCol.accessor("isLowStock", {
        header: "Alerta",
        cell: (i) =>
          i.getValue() ? (
            <Badge variant="destructive" className="gap-1">
              <AlertTriangle className="h-3 w-3" />
              Bajo stock
            </Badge>
          ) : null,
      }),
      iCol.display({
        id: "actions",
        header: "",
        cell: ({ row }) => (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={() => openAdjustDialog(row.original.id)}
              >
                Ajustar
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => {
                  const reason = prompt("Motivo del castigo:");
                  if (reason)
                    inventoryService.items
                      .writeOff(row.original.id, { quantity: 1, reason })
                      .then(() => {
                        qc.invalidateQueries({ queryKey: ["inventory"] });
                        toast.success("Castigo registrado");
                      })
                      .catch(() => toast.error("Error al registrar castigo"));
                }}
              >
                Castigo/Merma
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        ),
      }),
    ],
    [warehouseMap],
  );

  const warehouseColumns = useMemo(
    () => [
      wCol.accessor("code", {
        header: "Código",
        cell: (i) => (
          <span className="font-mono text-sm font-medium">{i.getValue()}</span>
        ),
      }),
      wCol.accessor("name", { header: "Nombre" }),
      wCol.accessor("address", {
        header: "Dirección",
        cell: (i) => (
          <span className="text-sm text-muted-foreground">
            {i.getValue() ?? "—"}
          </span>
        ),
      }),
      wCol.accessor("status", {
        header: "Estado",
        cell: (i) => (
          <Badge variant={i.getValue() === "Active" ? "success" : "secondary"}>
            {i.getValue() === "Active" ? "Activo" : "Inactivo"}
          </Badge>
        ),
      }),
    ],
    [],
  );

  const itemTable = useReactTable({
    data: items ?? [],
    columns: itemColumns,
    state: { sorting: iSorting },
    onSortingChange: setISorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  const whTable = useReactTable({
    data: warehouses ?? [],
    columns: warehouseColumns,
    state: { sorting: wSorting },
    onSortingChange: setWSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Inventario</h1>
          <p className="text-sm text-muted-foreground">
            Existencias por almacén
          </p>
        </div>
        <div className="flex gap-2">
          {activeTab === "items" && (
            <Button variant="outline" onClick={async () => {
              const resp = await api.get('/bff/reports/inventory-position/export', { responseType: 'blob' })
              const url = URL.createObjectURL(resp.data)
              const a = document.createElement('a'); a.href = url
              a.download = `Inventario-${new Date().toISOString().slice(0,10)}.xlsx`; a.click()
              URL.revokeObjectURL(url)
            }}>
              <FileDown className="mr-2 h-4 w-4" />Exportar Excel
            </Button>
          )}
          {activeTab === "items" ? (
            <Button
              onClick={() => {
                receiveForm.reset({ quantity: 1 });
                setIsReceiveOpen(true);
              }}
            >
              <Plus className="mr-2 h-4 w-4" />
              Recibir stock
            </Button>
          ) : (
            <Button
              onClick={() => {
                whForm.reset();
                setIsWHOpen(true);
              }}
            >
              <Plus className="mr-2 h-4 w-4" />
              Nuevo almacén
            </Button>
          )}
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="items">
            <PackageSearch className="mr-2 h-4 w-4" />
            Existencias
          </TabsTrigger>
          <TabsTrigger value="warehouses">
            <Warehouse className="mr-2 h-4 w-4" />
            Almacenes
          </TabsTrigger>
        </TabsList>

        <TabsContent value="items" className="space-y-4">
          <div className="flex gap-3 items-center">
            <Input
              placeholder="Buscar por SKU…"
              value={skuFilter}
              onChange={(e) => setSkuFilter(e.target.value)}
              className="w-64"
            />
            <label className="flex items-center gap-2 text-sm cursor-pointer">
              <input
                type="checkbox"
                checked={lowStockOnly}
                onChange={(e) => setLowStockOnly(e.target.checked)}
                className="rounded"
              />
              Solo bajo stock
            </label>
          </div>
          <SimpleTable
            table={itemTable}
            columns={itemColumns}
            isLoading={itemsLoading}
            emptyMessage="No hay existencias registradas"
          />
        </TabsContent>

        <TabsContent value="warehouses" className="space-y-4">
          <SimpleTable
            table={whTable}
            columns={warehouseColumns}
            isLoading={whLoading}
            emptyMessage="No hay almacenes registrados"
          />
        </TabsContent>
      </Tabs>

      {/* ── Receive Dialog ── */}
      <Dialog open={isReceiveOpen} onOpenChange={setIsReceiveOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Recibir Stock</DialogTitle>
          </DialogHeader>
          <form
            onSubmit={receiveForm.handleSubmit((v) =>
              receiveMut.mutate({
                ...v,
                referenceNumber: v.referenceNumber || undefined,
                notes: v.notes || undefined,
                reorderPoint: v.reorderPoint || undefined,
              }),
            )}
            className="space-y-4"
          >
            <div className="space-y-1.5">
              <Label>Artículo *</Label>
              <CatalogItemPicker
                value={receiveForm.watch("catalogItemId") ?? ""}
                onChange={(id, sku) => {
                  receiveForm.setValue("catalogItemId", id, {
                    shouldValidate: true,
                  });
                  receiveForm.setValue("sku", sku, { shouldValidate: true });
                }}
              />
              {receiveForm.formState.errors.catalogItemId && (
                <p className="text-xs text-destructive">
                  {receiveForm.formState.errors.catalogItemId.message}
                </p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label>Almacén *</Label>
              <WarehousePicker
                value={receiveForm.watch("warehouseId") ?? ""}
                onChange={(id) =>
                  receiveForm.setValue("warehouseId", id, {
                    shouldValidate: true,
                  })
                }
              />
              {receiveForm.formState.errors.warehouseId && (
                <p className="text-xs text-destructive">
                  {receiveForm.formState.errors.warehouseId.message}
                </p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>SKU *</Label>
                <Input
                  {...receiveForm.register("sku")}
                  placeholder="PROD-001"
                  readOnly
                  className="bg-muted"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Cantidad *</Label>
                <Input
                  type="number"
                  step="0.01"
                  {...receiveForm.register("quantity", { valueAsNumber: true })}
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>N° Referencia</Label>
                <Input
                  {...receiveForm.register("referenceNumber")}
                  placeholder="OC-12345"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Punto reorden</Label>
                <Input
                  type="number"
                  step="0.01"
                  {...receiveForm.register("reorderPoint", {
                    valueAsNumber: true,
                  })}
                />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Notas</Label>
              <Input
                {...receiveForm.register("notes")}
                placeholder="Opcional"
              />
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsReceiveOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={receiveMut.isPending}>
                {receiveMut.isPending ? "Guardando…" : "Recibir"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Adjust Dialog ── */}
      <Dialog open={isAdjustOpen} onOpenChange={setIsAdjustOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Ajustar Stock</DialogTitle>
          </DialogHeader>
          <form
            onSubmit={adjustForm.handleSubmit((v) =>
              adjustMut.mutate({ id: selectedItemId!, body: v }),
            )}
            className="space-y-4"
          >
            <div className="space-y-1.5">
              <Label>Delta (positivo = entrada, negativo = salida) *</Label>
              <Input
                type="number"
                step="0.01"
                {...adjustForm.register("delta", { valueAsNumber: true })}
                placeholder="Ej: 10 o -5"
              />
              {adjustForm.formState.errors.delta && (
                <p className="text-xs text-destructive">
                  {adjustForm.formState.errors.delta.message}
                </p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label>Motivo *</Label>
              <Input
                {...adjustForm.register("reason")}
                placeholder="Describe el motivo del ajuste"
              />
              {adjustForm.formState.errors.reason && (
                <p className="text-xs text-destructive">
                  {adjustForm.formState.errors.reason.message}
                </p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label>Notas</Label>
              <Input {...adjustForm.register("notes")} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsAdjustOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={adjustMut.isPending}>
                {adjustMut.isPending ? "Guardando…" : "Ajustar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Create Warehouse Dialog ── */}
      <Dialog open={isWHOpen} onOpenChange={setIsWHOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Nuevo Almacén</DialogTitle>
          </DialogHeader>
          <form
            onSubmit={whForm.handleSubmit((v) =>
              createWHMut.mutate({ ...v, address: v.address || undefined }),
            )}
            className="space-y-4"
          >
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Código *</Label>
                <Input {...whForm.register("code")} placeholder="CDMX-01" />
                {whForm.formState.errors.code && (
                  <p className="text-xs text-destructive">
                    {whForm.formState.errors.code.message}
                  </p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label>Nombre *</Label>
                <Input
                  {...whForm.register("name")}
                  placeholder="Almacén Ciudad de México"
                />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Dirección</Label>
              <Input {...whForm.register("address")} placeholder="Opcional" />
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsWHOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={createWHMut.isPending}>
                {createWHMut.isPending ? "Creando…" : "Crear"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// ── Shared table ──────────────────────────────────────────────────────────────

function SimpleTable<T>({
  table,
  columns,
  isLoading,
  emptyMessage,
}: {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  table: ReturnType<typeof useReactTable<T>>;
  columns: unknown[];
  isLoading: boolean;
  emptyMessage: string;
}) {
  return (
    <div className="rounded-md border overflow-x-auto">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((hg) => (
            <TableRow key={hg.id}>
              {hg.headers.map((h) => (
                <TableHead
                  key={h.id}
                  className={
                    h.column.getCanSort() ? "cursor-pointer select-none" : ""
                  }
                  onClick={h.column.getToggleSortingHandler()}
                >
                  <div className="flex items-center gap-1">
                    {flexRender(h.column.columnDef.header, h.getContext())}
                    {h.column.getIsSorted() === "asc" && (
                      <ChevronUp className="h-3 w-3" />
                    )}
                    {h.column.getIsSorted() === "desc" && (
                      <ChevronDown className="h-3 w-3" />
                    )}
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
                  <TableCell key={j}>
                    <Skeleton className="h-5 w-full" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : table.getRowModel().rows.length === 0 ? (
            <TableRow>
              <TableCell
                colSpan={columns.length as number}
                className="h-24 text-center text-muted-foreground"
              >
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            table.getRowModel().rows.map((row) => (
              <TableRow key={row.id}>
                {row.getVisibleCells().map((cell) => (
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
  );
}
