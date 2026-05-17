import { useState } from "react";
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
  CreditCard,
  ArrowLeftRight,
  ChevronUp,
  ChevronDown,
  MoreHorizontal,
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
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

import { AccountPicker } from "@/components/pickers/AccountPicker";
import {
  bankingService,
  type BankAccountDto,
  type BankTransactionDto,
  type BankAccountStatus,
  type BankTransactionStatus,
} from "./bankingService";

const fmt = (n: number, c = "USD") =>
  new Intl.NumberFormat("es-MX", { style: "currency", currency: c }).format(n);

const ACC_STATUS: Record<BankAccountStatus, string> = {
  Active: "Activa",
  Closed: "Cerrada",
  Suspended: "Suspendida",
};
const TX_STATUS: Record<BankTransactionStatus, string> = {
  Pending: "Pendiente",
  Reconciled: "Reconciliada",
  Voided: "Anulada",
};
const TX_STATUS_VARIANT: Record<
  BankTransactionStatus,
  "default" | "success" | "secondary" | "outline"
> = {
  Pending: "default",
  Reconciled: "success",
  Voided: "outline",
};

const accountSchema = z.object({
  accountNumber: z.string().min(1, "Requerido"),
  bankName: z.string().min(1, "Requerido"),
  currencyCode: z.string().length(3, "3 letras"),
  iban: z.string().optional(),
  swift: z.string().optional(),
});
type AccountForm = z.infer<typeof accountSchema>;

const txSchema = z.object({
  transactionDate: z.string().min(1, "Requerido"),
  description: z.string().min(1, "Requerido"),
  amount: z.coerce.number().positive("> 0"),
  type: z.enum(["Debit", "Credit"]),
  referenceNumber: z.string().optional(),
});
type TxForm = z.infer<typeof txSchema>;

const aCol = createColumnHelper<BankAccountDto>();
const tCol = createColumnHelper<BankTransactionDto>();

export function BankingPage() {
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState("accounts");
  const [selectedAccountId, setSelectedAccountId] = useState<string | null>(
    null,
  );
  const [isAccountOpen, setIsAccountOpen] = useState(false);
  const [isTxOpen, setIsTxOpen] = useState(false);
  const [aSorting, setASorting] = useState<SortingState>([]);
  const [tSorting, setTSorting] = useState<SortingState>([]);

  const { data: accounts, isLoading: accLoading } = useQuery({
    queryKey: ["bank-accounts"],
    queryFn: () => bankingService.accounts.list(),
  });

  const { data: transactions, isLoading: txLoading } = useQuery({
    queryKey: ["bank-transactions", selectedAccountId],
    queryFn: () =>
      selectedAccountId
        ? bankingService.transactions.list(selectedAccountId)
        : Promise.resolve([]),
    enabled: !!selectedAccountId,
  });

  const createAccMut = useMutation({
    mutationFn: bankingService.accounts.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["bank-accounts"] });
      setIsAccountOpen(false);
      toast.success("Cuenta bancaria creada");
    },
    onError: () => toast.error("Error al crear cuenta"),
  });

  const addTxMut = useMutation({
    mutationFn: (v: TxForm) =>
      bankingService.transactions.add(selectedAccountId!, {
        ...v,
        referenceNumber: v.referenceNumber || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["bank-transactions"] });
      setIsTxOpen(false);
      toast.success("Transacción registrada");
    },
    onError: () => toast.error("Error al registrar transacción"),
  });

  const voidMut = useMutation({
    mutationFn: ({ txId }: { txId: string }) =>
      bankingService.transactions.void(selectedAccountId!, txId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["bank-transactions"] });
      toast.success("Transacción anulada");
    },
    onError: () => toast.error("Error al anular"),
  });

  const accountForm = useForm<AccountForm>({
    resolver: zodResolver(accountSchema),
    defaultValues: { currencyCode: "MXN" },
  });
  const [linkedAccountingAccountId, setLinkedAccountingAccountId] = useState<
    string | null
  >(null);
  const txForm = useForm<TxForm>({
    resolver: zodResolver(txSchema),
    defaultValues: { type: "Credit" },
  });

  const accountCols = [
    aCol.accessor("bankName", { header: "Banco" }),
    aCol.accessor("accountNumber", {
      header: "N° Cuenta",
      cell: (i) => <span className="font-mono text-sm">{i.getValue()}</span>,
    }),
    aCol.accessor("currencyCode", { header: "Moneda" }),
    aCol.accessor("balance", {
      header: "Saldo",
      cell: (i) => (
        <span className="font-medium">
          {fmt(i.getValue(), i.row.original.currencyCode)}
        </span>
      ),
    }),
    aCol.accessor("status", {
      header: "Estado",
      cell: (i) => (
        <Badge variant={i.getValue() === "Active" ? "success" : "secondary"}>
          {ACC_STATUS[i.getValue()]}
        </Badge>
      ),
    }),
    aCol.accessor("unreconciledCount", {
      header: "Sin reconciliar",
      cell: (i) =>
        i.getValue() > 0 ? (
          <Badge variant="destructive">{i.getValue()}</Badge>
        ) : (
          <span className="text-muted-foreground">0</span>
        ),
    }),
    aCol.display({
      id: "actions",
      header: "",
      cell: ({ row }) => (
        <Button
          size="sm"
          variant="outline"
          className="h-7 text-xs"
          onClick={() => {
            setSelectedAccountId(row.original.id);
            setActiveTab("transactions");
          }}
        >
          Ver movimientos →
        </Button>
      ),
    }),
  ];

  const txCols = [
    tCol.accessor("transactionDate", {
      header: "Fecha",
      cell: (i) => (
        <span className="text-sm">
          {new Date(i.getValue()).toLocaleDateString("es-MX")}
        </span>
      ),
    }),
    tCol.accessor("description", {
      header: "Descripción",
      cell: (i) => <span className="text-sm">{i.getValue()}</span>,
    }),
    tCol.accessor("type", {
      header: "Tipo",
      cell: (i) => (
        <Badge variant={i.getValue() === "Credit" ? "success" : "secondary"}>
          {i.getValue() === "Credit" ? "Abono" : "Cargo"}
        </Badge>
      ),
    }),
    tCol.accessor("amount", {
      header: "Monto",
      cell: (i) => <span className="font-medium">{fmt(i.getValue())}</span>,
    }),
    tCol.accessor("status", {
      header: "Estado",
      cell: (i) => (
        <Badge variant={TX_STATUS_VARIANT[i.getValue()]}>
          {TX_STATUS[i.getValue()]}
        </Badge>
      ),
    }),
    tCol.accessor("referenceNumber", {
      header: "Referencia",
      cell: (i) => (
        <span className="text-sm text-muted-foreground">
          {i.getValue() ?? "—"}
        </span>
      ),
    }),
    tCol.display({
      id: "actions",
      header: "",
      cell: ({ row }) =>
        row.original.status === "Pending" ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => voidMut.mutate({ txId: row.original.id })}
              >
                Anular
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        ) : null,
    }),
  ];

  const aTable = useReactTable({
    data: accounts ?? [],
    columns: accountCols,
    state: { sorting: aSorting },
    onSortingChange: setASorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });
  const tTable = useReactTable({
    data: transactions ?? [],
    columns: txCols,
    state: { sorting: tSorting },
    onSortingChange: setTSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  const selectedAccount = accounts?.find((a) => a.id === selectedAccountId);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Banca</h1>
          <p className="text-sm text-muted-foreground">
            Cuentas bancarias y movimientos
          </p>
        </div>
        {activeTab === "accounts" ? (
          <Button
            onClick={() => {
              accountForm.reset({ currencyCode: "MXN" });
              setIsAccountOpen(true);
            }}
          >
            <Plus className="mr-2 h-4 w-4" />
            Nueva cuenta
          </Button>
        ) : (
          selectedAccountId && (
            <Button
              onClick={() => {
                txForm.reset({ type: "Credit" });
                setIsTxOpen(true);
              }}
            >
              <Plus className="mr-2 h-4 w-4" />
              Agregar movimiento
            </Button>
          )
        )}
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="accounts">
            <CreditCard className="mr-2 h-4 w-4" />
            Cuentas
          </TabsTrigger>
          <TabsTrigger value="transactions" disabled={!selectedAccountId}>
            <ArrowLeftRight className="mr-2 h-4 w-4" />
            {selectedAccount
              ? `${selectedAccount.bankName} – ${selectedAccount.accountNumber}`
              : "Movimientos"}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="accounts" className="space-y-4">
          <SimpleTable
            table={aTable}
            columns={accountCols}
            isLoading={accLoading}
            empty="No hay cuentas bancarias"
          />
        </TabsContent>

        <TabsContent value="transactions" className="space-y-4">
          <SimpleTable
            table={tTable}
            columns={txCols}
            isLoading={txLoading}
            empty="No hay movimientos registrados"
          />
        </TabsContent>
      </Tabs>

      {/* Create Account Dialog */}
      <Dialog open={isAccountOpen} onOpenChange={setIsAccountOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Nueva Cuenta Bancaria</DialogTitle>
          </DialogHeader>
          <form
            onSubmit={accountForm.handleSubmit((v) =>
              createAccMut.mutate({
                ...v,
                iban: v.iban || undefined,
                swift: v.swift || undefined,
                linkedAccountingAccountId:
                  linkedAccountingAccountId || undefined,
              }),
            )}
            className="space-y-4"
          >
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Banco *</Label>
                <Input
                  {...accountForm.register("bankName")}
                  placeholder="BBVA"
                />
              </div>
              <div className="space-y-1.5">
                <Label>N° Cuenta *</Label>
                <Input
                  {...accountForm.register("accountNumber")}
                  placeholder="0123456789"
                />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label>Moneda *</Label>
                <Input
                  {...accountForm.register("currencyCode")}
                  maxLength={3}
                  className="uppercase"
                />
              </div>
              <div className="space-y-1.5">
                <Label>SWIFT</Label>
                <Input
                  {...accountForm.register("swift")}
                  placeholder="BBVAMXMM"
                />
              </div>
              <div className="space-y-1.5">
                <Label>IBAN</Label>
                <Input {...accountForm.register("iban")} placeholder="MX…" />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Cuenta contable vinculada</Label>
              <AccountPicker
                value={linkedAccountingAccountId}
                onChange={setLinkedAccountingAccountId}
                placeholder="(opcional) Seleccionar cuenta contable"
              />
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsAccountOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={createAccMut.isPending}>
                {createAccMut.isPending ? "Creando…" : "Crear"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Add Transaction Dialog */}
      <Dialog open={isTxOpen} onOpenChange={setIsTxOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Registrar Movimiento</DialogTitle>
          </DialogHeader>
          <form
            onSubmit={txForm.handleSubmit((v) => addTxMut.mutate(v))}
            className="space-y-4"
          >
            <div className="space-y-1.5">
              <Label>Fecha *</Label>
              <Input type="date" {...txForm.register("transactionDate")} />
            </div>
            <div className="space-y-1.5">
              <Label>Descripción *</Label>
              <Input
                {...txForm.register("description")}
                placeholder="Pago a proveedor"
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Monto *</Label>
                <Input
                  type="number"
                  step="0.01"
                  {...txForm.register("amount")}
                />
              </div>
              <div className="space-y-1.5">
                <Label>Tipo *</Label>
                <Select
                  onValueChange={(v) =>
                    txForm.setValue("type", v as "Debit" | "Credit")
                  }
                  defaultValue="Credit"
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Credit">Abono</SelectItem>
                    <SelectItem value="Debit">Cargo</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Referencia</Label>
              <Input
                {...txForm.register("referenceNumber")}
                placeholder="Opcional"
              />
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsTxOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={addTxMut.isPending}>
                {addTxMut.isPending ? "Guardando…" : "Registrar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function SimpleTable<T>({
  table,
  columns,
  isLoading,
  empty,
}: {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  table: ReturnType<typeof useReactTable<T>>;
  columns: unknown[];
  isLoading: boolean;
  empty: string;
}) {
  return (
    <div className="rounded-md border">
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
                {empty}
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
