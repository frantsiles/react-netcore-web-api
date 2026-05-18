import { useQuery } from '@tanstack/react-query'
import {
  AreaChart, Area, BarChart, Bar,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import {
  TrendingUp, TrendingDown, DollarSign, ShoppingCart,
  ShoppingBag, Package, AlertTriangle, CheckSquare, Banknote,
  Users,
} from 'lucide-react'
import { KpiCard } from './KpiCard'
import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { reportingService } from './reportingService'

const fmt = (n: number, currency = false) =>
  currency
    ? new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n)
    : new Intl.NumberFormat('es-MX').format(n)

const pct = (n: number) => `${n.toFixed(1)}%`

export function DashboardPage() {
  const { data: kpi, isLoading } = useQuery({
    queryKey: ['kpi-dashboard'],
    queryFn: reportingService.getKpiDashboard,
    refetchInterval: 60_000,
  })

  const { data: timeSeries } = useQuery({
    queryKey: ['revenue-time-series'],
    queryFn: () => reportingService.getRevenueTimeSeries(6),
    refetchInterval: 300_000,
  })

  const revenueData = (timeSeries?.revenue ?? []).map(d => ({
    mes: d.month, ingresos: d.revenue, costos: d.cogs,
  }))

  const orderData = (timeSeries?.orders ?? []).map(d => ({
    mes: d.month, ventas: d.salesOrders, compras: d.purchaseOrders,
  }))

  return (
    <div className="space-y-6">
      {/* KPI Grid */}
      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-5">
        <KpiCard
          loading={isLoading}
          title="Ingresos Totales"
          value={kpi ? fmt(kpi.totalRevenue, true) : '—'}
          subtitle={kpi ? `Margen bruto: ${pct(kpi.grossMarginPct)}` : undefined}
          icon={TrendingUp}
          trend="up"
        />
        <KpiCard
          loading={isLoading}
          title="Cuentas por Cobrar"
          value={kpi ? fmt(kpi.totalReceivables, true) : '—'}
          subtitle="Por cobrar"
          icon={DollarSign}
        />
        <KpiCard
          loading={isLoading}
          title="Cuentas por Pagar"
          value={kpi ? fmt(kpi.totalPayables, true) : '—'}
          subtitle="Por pagar"
          icon={TrendingDown}
          trend="down"
        />
        <KpiCard
          loading={isLoading}
          title="Saldo Bancario"
          value={kpi ? fmt(kpi.cashBalance, true) : '—'}
          icon={Banknote}
          trend="neutral"
        />
        <KpiCard
          loading={isLoading}
          title="Stock Bajo"
          value={kpi ? String(kpi.lowStockItems) : '—'}
          subtitle="productos bajo mínimo"
          icon={AlertTriangle}
          trend={kpi && kpi.lowStockItems > 0 ? 'down' : 'neutral'}
        />
      </div>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-2 md:grid-cols-4">
        <KpiCard
          loading={isLoading}
          title="OC Abiertas"
          value={kpi ? String(kpi.openPurchaseOrders) : '—'}
          icon={ShoppingBag}
        />
        <KpiCard
          loading={isLoading}
          title="OV Abiertas"
          value={kpi ? String(kpi.openSalesOrders) : '—'}
          icon={ShoppingCart}
        />
        <KpiCard
          loading={isLoading}
          title="Aprobaciones Pendientes"
          value={kpi ? String(kpi.openApprovalRequests) : '—'}
          icon={CheckSquare}
          trend={kpi && kpi.openApprovalRequests > 0 ? 'down' : 'neutral'}
        />
        <KpiCard
          loading={isLoading}
          title="Inventario"
          value={kpi ? String(kpi.lowStockItems) + ' alertas' : '—'}
          icon={Package}
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card className="p-5">
          <h3 className="mb-4 text-sm font-semibold">Ingresos vs Costos (6 meses)</h3>
          {isLoading ? (
            <Skeleton className="h-52 w-full" />
          ) : (
            <ResponsiveContainer width="100%" height={210}>
              <AreaChart data={revenueData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                <defs>
                  <linearGradient id="ingresosGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="hsl(221,83%,53%)" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="hsl(221,83%,53%)" stopOpacity={0} />
                  </linearGradient>
                  <linearGradient id="costosGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="hsl(0,84%,60%)" stopOpacity={0.2} />
                    <stop offset="95%" stopColor="hsl(0,84%,60%)" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(214,32%,91%)" />
                <XAxis dataKey="mes" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} tickFormatter={v => `$${(v / 1000).toFixed(0)}k`} />
                <Tooltip formatter={(v) => fmt(v as number, true)} />
                <Legend />
                <Area type="monotone" dataKey="ingresos" name="Ingresos" stroke="hsl(221,83%,53%)" fill="url(#ingresosGrad)" strokeWidth={2} />
                <Area type="monotone" dataKey="costos" name="Costos" stroke="hsl(0,84%,60%)" fill="url(#costosGrad)" strokeWidth={2} />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </Card>

        <Card className="p-5">
          <h3 className="mb-4 text-sm font-semibold">Órdenes por mes</h3>
          {isLoading ? (
            <Skeleton className="h-52 w-full" />
          ) : (
            <ResponsiveContainer width="100%" height={210}>
              <BarChart data={orderData} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(214,32%,91%)" />
                <XAxis dataKey="mes" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip />
                <Legend />
                <Bar dataKey="ventas" name="Ventas" fill="hsl(221,83%,53%)" radius={[3, 3, 0, 0]} />
                <Bar dataKey="compras" name="Compras" fill="hsl(142,71%,45%)" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </Card>
      </div>

      {/* Quick links */}
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        {[
          { label: 'Nueva cotización', href: '/sales/quotes/new', icon: ShoppingCart },
          { label: 'Nueva orden de compra', href: '/purchasing/new', icon: ShoppingBag },
          { label: 'Registrar pago', href: '/banking', icon: Banknote },
          { label: 'Ver empleados', href: '/hr/employees', icon: Users },
        ].map(({ label, href, icon: Icon }) => (
          <a
            key={href}
            href={href}
            className="flex items-center gap-2 rounded-lg border bg-card px-3 py-2.5 text-sm font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          >
            <Icon className="h-4 w-4 shrink-0 text-primary" />
            {label}
          </a>
        ))}
      </div>
    </div>
  )
}
