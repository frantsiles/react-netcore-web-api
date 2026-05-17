import { cn } from '@/lib/utils'
import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import type { LucideIcon } from 'lucide-react'

interface KpiCardProps {
  title: string
  value: string
  subtitle?: string
  icon: LucideIcon
  trend?: 'up' | 'down' | 'neutral'
  className?: string
  loading?: boolean
}

export function KpiCard({ title, value, subtitle, icon: Icon, trend, className, loading }: KpiCardProps) {
  if (loading) {
    return (
      <Card className="p-5">
        <Skeleton className="h-4 w-24 mb-3" />
        <Skeleton className="h-8 w-32 mb-2" />
        <Skeleton className="h-3 w-20" />
      </Card>
    )
  }

  const trendColor =
    trend === 'up' ? 'text-green-600' :
    trend === 'down' ? 'text-red-500' :
    'text-muted-foreground'

  return (
    <Card className={cn('p-5', className)}>
      <div className="flex items-start justify-between">
        <div className="flex-1 min-w-0">
          <p className="text-sm text-muted-foreground truncate">{title}</p>
          <p className="mt-1 text-2xl font-bold tabular-nums">{value}</p>
          {subtitle && (
            <p className={cn('mt-1 text-xs', trendColor)}>{subtitle}</p>
          )}
        </div>
        <div className="ml-3 flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
          <Icon className="h-5 w-5 text-primary" />
        </div>
      </div>
    </Card>
  )
}
