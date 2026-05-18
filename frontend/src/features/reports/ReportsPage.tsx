import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  TrendingUp, TrendingDown, DollarSign, ShoppingCart,
  Package, Clock, BarChart3, FileSpreadsheet, Scale,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

import { reportsService, type BalanceSheetSectionDto } from './reportsService'

const fmt = (n: number) => new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' }).format(n)
const fmtPct = (n: number) => `${n.toFixed(1)}%`

// ── KPI Card ──────────────────────────────────────────────────────────────────

function KpiCard({ label, value, icon: Icon, sub }: {
  label: string; value: string; icon: React.ComponentType<{ className?: string }>; sub?: string
}) {
  return (
    <div className="rounded-lg border bg-card p-4 space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-sm text-muted-foreground">{label}</span>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </div>
      <div className="text-2xl font-bold">{value}</div>
      {sub && <div className="text-xs text-muted-foreground">{sub}</div>}
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function ReportsPage() {
  const [activeTab, setActiveTab] = useState('kpi')
  const [fiscalPeriod, setFiscalPeriod] = useState(() => {
    const now = new Date()
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
  })
  const [pnlFrom, setPnlFrom] = useState(() => {
    const now = new Date()
    return `${now.getFullYear()}-01-01`
  })
  const [pnlTo, setPnlTo] = useState(() => {
    const now = new Date()
    return now.toISOString().slice(0, 10)
  })
  const [tbPeriod, setTbPeriod] = useState(fiscalPeriod)
  const [runTb, setRunTb] = useState(false)
  const [runPnl, setRunPnl] = useState(false)
  const [runBs, setRunBs] = useState(false)

  const { data: kpi, isLoading: kpiLoading } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: reportsService.kpi,
    enabled: activeTab === 'kpi',
  })

  const { data: tb, isLoading: tbLoading } = useQuery({
    queryKey: ['trial-balance', tbPeriod],
    queryFn: () => reportsService.trialBalance(tbPeriod),
    enabled: runTb,
  })

  const { data: pnl, isLoading: pnlLoading } = useQuery({
    queryKey: ['pnl', pnlFrom, pnlTo],
    queryFn: () => reportsService.profitAndLoss(pnlFrom, pnlTo),
    enabled: runPnl,
  })

  const { data: bs, isLoading: bsLoading } = useQuery({
    queryKey: ['balance-sheet'],
    queryFn: () => reportsService.balanceSheet(),
    enabled: runBs,
  })

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Reportes</h1>
        <p className="text-sm text-muted-foreground">Indicadores financieros y operativos</p>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="kpi"><BarChart3 className="mr-2 h-4 w-4" />KPI Dashboard</TabsTrigger>
          <TabsTrigger value="bs"><Scale className="mr-2 h-4 w-4" />Balance General</TabsTrigger>
          <TabsTrigger value="pnl"><TrendingUp className="mr-2 h-4 w-4" />PyG</TabsTrigger>
          <TabsTrigger value="tb"><FileSpreadsheet className="mr-2 h-4 w-4" />Balanza</TabsTrigger>
        </TabsList>

        {/* ── KPI Dashboard ── */}
        <TabsContent value="kpi" className="space-y-6">
          {kpiLoading ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-24 w-full rounded-lg" />)}
            </div>
          ) : kpi ? (
            <>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <KpiCard label="Ingresos totales" value={fmt(kpi.totalRevenue)} icon={DollarSign} />
                <KpiCard label="Margen bruto" value={fmtPct(kpi.grossMarginPct)} icon={TrendingUp}
                  sub={`COGS: ${fmt(kpi.totalCogs)}`} />
                <KpiCard label="Saldo de caja" value={fmt(kpi.cashBalance)} icon={DollarSign} />
                <KpiCard label="Por cobrar" value={fmt(kpi.totalReceivables)} icon={TrendingUp} />
                <KpiCard label="Por pagar" value={fmt(kpi.totalPayables)} icon={TrendingDown} />
                <KpiCard label="OC abiertas" value={String(kpi.openPurchaseOrders)} icon={ShoppingCart} />
                <KpiCard label="Órdenes de venta" value={String(kpi.openSalesOrders)} icon={Package} />
                <KpiCard label="Aprobaciones pend." value={String(kpi.openApprovalRequests)} icon={Clock}
                  sub={kpi.lowStockItems > 0 ? `${kpi.lowStockItems} art. bajo stock` : undefined} />
              </div>
              <p className="text-xs text-muted-foreground">
                Actualizado: {new Date(kpi.asOf).toLocaleString('es-MX')}
              </p>
            </>
          ) : (
            <p className="text-sm text-muted-foreground">No se pudo cargar el dashboard.</p>
          )}
        </TabsContent>

        {/* ── Balance General ── */}
        <TabsContent value="bs" className="space-y-4">
          <div className="flex gap-3 items-end">
            <p className="text-sm text-muted-foreground">Balance a fecha de hoy.</p>
            <Button onClick={() => setRunBs(true)}>Generar</Button>
          </div>
          {runBs && (
            bsLoading ? <Skeleton className="h-64 w-full" /> : bs ? (
              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <h3 className="font-medium">Balance General — {new Date(bs.asOf).toLocaleDateString('es-MX')}</h3>
                  <Badge variant={bs.isBalanced ? 'success' : 'destructive'}>
                    {bs.isBalanced ? 'Balanceado' : 'Desbalanceado'}
                  </Badge>
                </div>
                {([bs.assets, bs.liabilities, bs.equity] as BalanceSheetSectionDto[]).map((section, i) => (
                  <div key={i} className="rounded-md border">
                    <div className="px-4 py-2 bg-muted/50 font-medium text-sm">{section.section}</div>
                    <Table>
                      <TableBody>
                        {section.lines.map((line, j) => (
                          <TableRow key={j}>
                            <TableCell className="font-mono text-xs text-muted-foreground w-24">{line.accountNumber}</TableCell>
                            <TableCell>{line.accountName}</TableCell>
                            <TableCell className="text-right font-medium">{fmt(line.amount)}</TableCell>
                          </TableRow>
                        ))}
                        <TableRow className="bg-muted/30 font-semibold">
                          <TableCell colSpan={2} className="text-right pr-8">Total {section.section}</TableCell>
                          <TableCell className="text-right">{fmt(section.total)}</TableCell>
                        </TableRow>
                      </TableBody>
                    </Table>
                  </div>
                ))}
                <div className="rounded-lg border bg-card p-4 grid grid-cols-3 gap-4">
                  <div>
                    <div className="text-xs text-muted-foreground">Total Activos</div>
                    <div className="text-xl font-bold">{fmt(bs.assets.total)}</div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground">Total Pasivos</div>
                    <div className="text-xl font-bold">{fmt(bs.liabilities.total)}</div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground">Capital</div>
                    <div className="text-xl font-bold">{fmt(bs.equity.total)}</div>
                  </div>
                </div>
              </div>
            ) : <p className="text-sm text-muted-foreground">No se pudo cargar el balance.</p>
          )}
        </TabsContent>

        {/* ── Trial Balance ── */}
        <TabsContent value="tb" className="space-y-4">
          <div className="flex gap-3 items-end">
            <div className="space-y-1.5">
              <Label>Período fiscal</Label>
              <Input value={fiscalPeriod} onChange={e => setFiscalPeriod(e.target.value)} placeholder="2026-05" className="w-36" />
            </div>
            <Button onClick={() => { setTbPeriod(fiscalPeriod); setRunTb(true) }}>Generar</Button>
          </div>

          {runTb && (
            tbLoading ? <Skeleton className="h-64 w-full" /> : tb ? (
              <div className="space-y-3">
                <div className="flex items-center gap-3">
                  <h3 className="font-medium">Balanza de comprobación — {tb.fiscalPeriod}</h3>
                  <Badge variant={tb.isBalanced ? 'success' : 'destructive'}>
                    {tb.isBalanced ? 'Balanceada' : 'Desbalanceada'}
                  </Badge>
                </div>
                <div className="rounded-md border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Cuenta</TableHead>
                        <TableHead>Nombre</TableHead>
                        <TableHead>Tipo</TableHead>
                        <TableHead className="text-right">Débitos</TableHead>
                        <TableHead className="text-right">Créditos</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {tb.lines.map((line, i) => (
                        <TableRow key={i}>
                          <TableCell className="font-mono text-sm">{line.accountNumber}</TableCell>
                          <TableCell>{line.accountName}</TableCell>
                          <TableCell><Badge variant="secondary">{line.accountType}</Badge></TableCell>
                          <TableCell className="text-right">{line.debitBalance > 0 ? fmt(line.debitBalance) : '—'}</TableCell>
                          <TableCell className="text-right">{line.creditBalance > 0 ? fmt(line.creditBalance) : '—'}</TableCell>
                        </TableRow>
                      ))}
                      <TableRow className="font-medium bg-muted/50">
                        <TableCell colSpan={3}>Total</TableCell>
                        <TableCell className="text-right">{fmt(tb.totalDebits)}</TableCell>
                        <TableCell className="text-right">{fmt(tb.totalCredits)}</TableCell>
                      </TableRow>
                    </TableBody>
                  </Table>
                </div>
              </div>
            ) : <p className="text-sm text-muted-foreground">No hay datos para ese período.</p>
          )}
        </TabsContent>

        {/* ── Profit & Loss ── */}
        <TabsContent value="pnl" className="space-y-4">
          <div className="flex gap-3 items-end flex-wrap">
            <div className="space-y-1.5">
              <Label>Desde</Label>
              <Input type="date" value={pnlFrom} onChange={e => setPnlFrom(e.target.value)} className="w-40" />
            </div>
            <div className="space-y-1.5">
              <Label>Hasta</Label>
              <Input type="date" value={pnlTo} onChange={e => setPnlTo(e.target.value)} className="w-40" />
            </div>
            <Button onClick={() => setRunPnl(true)}>Generar</Button>
          </div>

          {runPnl && (
            pnlLoading ? <Skeleton className="h-64 w-full" /> : pnl ? (
              <div className="space-y-4">
                {pnl.sections.map((section, i) => (
                  <div key={i} className="rounded-md border">
                    <div className="px-4 py-2 bg-muted/50 font-medium text-sm">{section.section}</div>
                    <Table>
                      <TableBody>
                        {section.lines.map((line, j) => (
                          <TableRow key={j}>
                            <TableCell className="font-mono text-xs text-muted-foreground">{line.accountNumber}</TableCell>
                            <TableCell>{line.accountName}</TableCell>
                            <TableCell className="text-right font-medium">{fmt(line.amount)}</TableCell>
                          </TableRow>
                        ))}
                        <TableRow className="bg-muted/30 font-medium">
                          <TableCell colSpan={2} className="text-right pr-8">Total {section.section}</TableCell>
                          <TableCell className="text-right">{fmt(section.total)}</TableCell>
                        </TableRow>
                      </TableBody>
                    </Table>
                  </div>
                ))}
                <div className="rounded-lg border bg-card p-4 grid grid-cols-3 gap-4">
                  <div>
                    <div className="text-xs text-muted-foreground">Utilidad bruta</div>
                    <div className="text-xl font-bold">{fmt(pnl.grossProfit)}</div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground">Utilidad operativa</div>
                    <div className="text-xl font-bold">{fmt(pnl.operatingIncome)}</div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground">Utilidad neta</div>
                    <div className={`text-xl font-bold ${pnl.netIncome >= 0 ? 'text-green-700' : 'text-red-600'}`}>
                      {fmt(pnl.netIncome)}
                    </div>
                  </div>
                </div>
              </div>
            ) : <p className="text-sm text-muted-foreground">No hay datos para ese período.</p>
          )}
        </TabsContent>
      </Tabs>
    </div>
  )
}
