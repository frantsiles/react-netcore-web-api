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
import { Plus, FileText, DollarSign, ChevronUp, ChevronDown, MoreHorizontal, FileDown, Stamp } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
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
import { SalesOrderPicker } from "@/components/pickers/SalesOrderPicker";

// ── Types ────────────────────────────────────────────────────────────────────

interface InvoiceLine {
  id: string;
  sku: string;
  description: string;
  quantity: number;
  unitPrice: number;
  currencyCode: string;
  discountPercent: number;
  lineTotal: number;
}

interface Invoice {
  id: string;
  invoiceNumber: string;
  customerId: string;
  customerName: string;
  originSalesOrderId: string | null;
  status: string;
  issueDate: string;
  dueDate: string;
  currencyCode: string;
  subtotal: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  notes: string | null;
  lines: InvoiceLine[];
  createdAt: string;
  updatedAt: string;
}

// ── Schemas ──────────────────────────────────────────────────────────────────

const convertSchema = z.object({
  salesOrderId: z.string().uuid("Must be a valid UUID"),
  dueDate: z.string().min(1, "Due date is required"),
  notes: z.string().optional(),
});

const paymentSchema = z.object({
  amount: z.number().positive("Amount must be positive"),
  currencyCode: z.string().length(3, "3-letter ISO code"),
  paidAt: z.string().min(1, "Date is required"),
  reference: z.string().optional(),
});

type ConvertForm = z.infer<typeof convertSchema>;
type PaymentForm = z.infer<typeof paymentSchema>;

// ── Status badges ─────────────────────────────────────────────────────────────

const statusColor: Record<string, string> = {
  Draft:         "secondary",
  Issued:        "default",
  PartiallyPaid: "outline",
  Paid:          "success",
  Cancelled:     "destructive",
  Voided:        "destructive",
};

function StatusBadge({ status }: { status: string }) {
  return <Badge variant={(statusColor[status] ?? "secondary") as Parameters<typeof Badge>[0]["variant"]}>{status}</Badge>;
}

const feStatusColor: Record<string, string> = {
  Accepted:  "success",
  Submitted: "outline",
  Signed:    "outline",
  Pending:   "secondary",
  Rejected:  "destructive",
  Error:     "destructive",
};

function FeBadge({ status }: { status: string }) {
  const label: Record<string, string> = {
    Accepted: "Timbrada", Submitted: "En proceso",
    Signed: "Firmada", Pending: "Pendiente",
    Rejected: "Rechazada", Error: "Error",
  };
  return (
    <Badge variant={(feStatusColor[status] ?? "secondary") as Parameters<typeof Badge>[0]["variant"]}
      className="text-xs">
      {label[status] ?? status}
    </Badge>
  );
}

// ── Column helper ─────────────────────────────────────────────────────────────

const col = createColumnHelper<Invoice>();

// ── Page ──────────────────────────────────────────────────────────────────────

export function InvoicingPage() {
  const qc = useQueryClient();
  const [sorting, setSorting] = useState<SortingState>([]);
  const [convertOpen, setConvertOpen] = useState(false);
  const [paymentOpen, setPaymentOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [feStatuses, setFeStatuses] = useState<Record<string, string>>({});

  const { data: invoices = [], isLoading } = useQuery<Invoice[]>({
    queryKey: ["invoices"],
    queryFn: async () => {
      const res = await api.get("/bff/invoicing/invoices");
      return res.data;
    },
  });

  const convertMut = useMutation({
    mutationFn: async (data: ConvertForm) => {
      const res = await api.post("/bff/invoicing/invoices/convert", data);
      return res.data;
    },
    onSuccess: () => {
      toast.success("Invoice created from sales order");
      qc.invalidateQueries({ queryKey: ["invoices"] });
      setConvertOpen(false);
      convertForm.reset();
      setOrderCurrency(undefined);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? "Error";
      toast.error(msg);
    },
  });

  const issueMut = useMutation({
    mutationFn: async (id: string) => {
      const res = await api.post(`/bff/invoicing/invoices/${id}/issue`, {});
      return res.data;
    },
    onSuccess: () => {
      toast.success("Invoice issued");
      qc.invalidateQueries({ queryKey: ["invoices"] });
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? "Error";
      toast.error(msg);
    },
  });

  const paymentMut = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: PaymentForm }) => {
      const res = await api.post(`/bff/invoicing/invoices/${id}/payments`, data);
      return res.data;
    },
    onSuccess: () => {
      toast.success("Payment recorded");
      qc.invalidateQueries({ queryKey: ["invoices"] });
      setPaymentOpen(false);
      paymentForm.reset();
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? "Error";
      toast.error(msg);
    },
  });

  const cancelMut = useMutation({
    mutationFn: async (id: string) => {
      const res = await api.post(`/bff/invoicing/invoices/${id}/cancel`,
        { reason: "Cancelled by user" });
      return res.data;
    },
    onSuccess: () => {
      toast.success("Invoice cancelled");
      qc.invalidateQueries({ queryKey: ["invoices"] });
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? "Error";
      toast.error(msg);
    },
  });

  const timbreMut = useMutation({
    mutationFn: async (id: string) => {
      const res = await api.post(`/bff/fiscal/cr/invoices/${id}/timbre`, {});
      return res.data as { status: string; haciendaEstado: string | null };
    },
    onSuccess: (data, id) => {
      setFeStatuses(prev => ({ ...prev, [id]: data.status }));
      const estado = data.haciendaEstado ?? data.status;
      toast.success(`Timbre enviado — ${estado}`);
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? "Error al timbrar";
      toast.error(msg);
    },
  });

  const downloadPdf = async (path: string, filename: string) => {
    const resp = await api.get(path, { responseType: 'blob' })
    const url = URL.createObjectURL(resp.data)
    const a = document.createElement('a')
    a.href = url; a.download = filename; a.click()
    URL.revokeObjectURL(url)
  }

  const convertForm = useForm<ConvertForm>({ resolver: zodResolver(convertSchema) });
  const [orderCurrency, setOrderCurrency] = useState<string | undefined>();
  const paymentForm = useForm<PaymentForm>({
    resolver: zodResolver(paymentSchema),
    defaultValues: { currencyCode: "USD", paidAt: new Date().toISOString().slice(0, 10) },
  });

  const columns = [
    col.accessor("invoiceNumber", { header: "Invoice #" }),
    col.accessor("customerName", { header: "Cliente", cell: i => <span className="text-sm">{i.getValue()}</span> }),
    col.accessor("status", {
      header: "Status",
      cell: info => <StatusBadge status={info.getValue()} />,
    }),
    col.display({
      id: "feStatus",
      header: "FE",
      cell: ({ row }) => {
        const s = feStatuses[row.original.id];
        return s ? <FeBadge status={s} /> : <span className="text-muted-foreground text-xs">—</span>;
      },
    }),
    col.accessor("issueDate", { header: "Issue Date" }),
    col.accessor("dueDate", { header: "Due Date" }),
    col.accessor("totalAmount", {
      header: "Total",
      cell: info => `${info.getValue().toFixed(2)} ${info.row.original.currencyCode}`,
    }),
    col.accessor("balanceDue", {
      header: "Balance Due",
      cell: info => `${info.getValue().toFixed(2)} ${info.row.original.currencyCode}`,
    }),
    col.display({
      id: "actions",
      header: "",
      cell: ({ row }) => {
        const inv = row.original;
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm"><MoreHorizontal className="h-4 w-4" /></Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {inv.status === "Draft" && (
                <DropdownMenuItem onClick={() => issueMut.mutate(inv.id)}>
                  Issue Invoice
                </DropdownMenuItem>
              )}
              {(inv.status === "Issued" || inv.status === "PartiallyPaid") && (
                <DropdownMenuItem onClick={() => {
                  setSelectedId(inv.id);
                  setPaymentOpen(true);
                }}>
                  Record Payment
                </DropdownMenuItem>
              )}
              {inv.status === "Issued" && !feStatuses[inv.id] && (
                <DropdownMenuItem onClick={() => timbreMut.mutate(inv.id)}
                  disabled={timbreMut.isPending}>
                  <Stamp className="mr-2 h-4 w-4" />Timbrar (FE-CR)
                </DropdownMenuItem>
              )}
              {inv.status !== "Paid" && inv.status !== "Cancelled" && inv.status !== "Voided" && (
                <DropdownMenuItem
                  className="text-destructive"
                  onClick={() => cancelMut.mutate(inv.id)}>
                  Cancel
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => downloadPdf(`/bff/invoicing/invoices/${inv.id}/pdf`, `FAC-${inv.invoiceNumber}.pdf`)}>
                <FileDown className="mr-2 h-4 w-4" />Descargar PDF
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        );
      },
    }),
  ];

  const table = useReactTable({
    data: invoices,
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <FileText className="h-6 w-6" /> Invoicing
          </h1>
          <p className="text-muted-foreground text-sm">Manage invoices and payments</p>
        </div>
        <Button onClick={() => setConvertOpen(true)}>
          <Plus className="h-4 w-4 mr-1" /> Convert Order to Invoice
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <Table>
            <TableHeader>
              {table.getHeaderGroups().map(hg => (
                <TableRow key={hg.id}>
                  {hg.headers.map(h => (
                    <TableHead key={h.id} onClick={h.column.getToggleSortingHandler()}
                      className={h.column.getCanSort() ? "cursor-pointer select-none" : ""}>
                      <span className="flex items-center gap-1">
                        {flexRender(h.column.columnDef.header, h.getContext())}
                        {h.column.getIsSorted() === "asc" && <ChevronUp className="h-3 w-3" />}
                        {h.column.getIsSorted() === "desc" && <ChevronDown className="h-3 w-3" />}
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
                    No invoices found
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
      )}

      {/* Convert Order to Invoice */}
      <Dialog open={convertOpen} onOpenChange={(open) => {
        setConvertOpen(open);
        if (!open) { convertForm.reset(); setOrderCurrency(undefined); }
      }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5" /> Convert Sales Order to Invoice
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={convertForm.handleSubmit(d => convertMut.mutate(d))} className="space-y-4">
            <div className="space-y-2">
              <Label>Sales Order</Label>
              <SalesOrderPicker
                value={convertForm.watch("salesOrderId") ?? ""}
                onChange={(id, order) => {
                  convertForm.setValue("salesOrderId", id, { shouldValidate: true });
                  setOrderCurrency(order?.currencyCode);
                }}
              />
              {convertForm.formState.errors.salesOrderId && (
                <p className="text-xs text-destructive">{convertForm.formState.errors.salesOrderId.message}</p>
              )}
              {orderCurrency && (
                <p className="text-xs text-muted-foreground">Moneda: {orderCurrency}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label>Due Date</Label>
              <Input type="date" {...convertForm.register("dueDate")} />
              {convertForm.formState.errors.dueDate && (
                <p className="text-xs text-destructive">{convertForm.formState.errors.dueDate.message}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label>Notes</Label>
              <Input placeholder="Optional notes" {...convertForm.register("notes")} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setConvertOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={convertMut.isPending}>
                {convertMut.isPending ? "Creating…" : "Create Invoice"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Record Payment */}
      <Dialog open={paymentOpen} onOpenChange={setPaymentOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <DollarSign className="h-5 w-5" /> Record Payment
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={paymentForm.handleSubmit(d => {
            if (selectedId) paymentMut.mutate({ id: selectedId, data: d });
          })} className="space-y-4">
            <div className="space-y-2">
              <Label>Amount</Label>
              <Input type="number" step="0.01" {...paymentForm.register("amount", { valueAsNumber: true })} />
              {paymentForm.formState.errors.amount && (
                <p className="text-xs text-destructive">{paymentForm.formState.errors.amount.message}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label>Currency Code</Label>
              <Input maxLength={3} placeholder="USD" {...paymentForm.register("currencyCode")} />
            </div>
            <div className="space-y-2">
              <Label>Payment Date</Label>
              <Input type="date" {...paymentForm.register("paidAt")} />
            </div>
            <div className="space-y-2">
              <Label>Reference</Label>
              <Input placeholder="Check #, transfer ID, etc." {...paymentForm.register("reference")} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setPaymentOpen(false)}>Cancel</Button>
              <Button type="submit" disabled={paymentMut.isPending}>
                {paymentMut.isPending ? "Recording…" : "Record Payment"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
