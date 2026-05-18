import { NavLink, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { navGroups } from './nav-items'
import { Separator } from '@/components/ui/separator'
import { Building2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface SidebarProps {
  collapsed?: boolean
  onClose?: () => void
}

export function Sidebar({ collapsed = false, onClose }: SidebarProps) {
  const location = useLocation()

  return (
    <aside
      className={cn(
        'flex h-screen flex-col border-r bg-card transition-all duration-300',
        collapsed ? 'w-16' : 'w-60'
      )}
    >
      {/* Logo */}
      <div className="flex h-16 items-center gap-2 px-4 border-b">
        <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary shrink-0">
          <Building2 className="h-5 w-5 text-primary-foreground" />
        </div>
        {!collapsed && (
          <span className="font-semibold text-sm tracking-tight flex-1">Demo ERP</span>
        )}
        {/* Close button on mobile */}
        {onClose && !collapsed && (
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 md:hidden"
            onClick={onClose}
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto px-2 py-3 space-y-4">
        {navGroups.map((group) => (
          <div key={group.label}>
            {!collapsed && (
              <p className="mb-1 px-2 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                {group.label}
              </p>
            )}
            <ul className="space-y-0.5">
              {group.items.map((item) => {
                const isActive = location.pathname.startsWith(item.href)
                return (
                  <li key={item.href}>
                    <NavLink
                      to={item.href}
                      onClick={onClose}
                      className={cn(
                        'flex items-center gap-3 rounded-md px-2 py-2 text-sm font-medium transition-colors',
                        isActive
                          ? 'bg-primary/10 text-primary'
                          : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                      )}
                      title={collapsed ? item.title : undefined}
                    >
                      <item.icon className="h-4 w-4 shrink-0" />
                      {!collapsed && <span>{item.title}</span>}
                    </NavLink>
                  </li>
                )
              })}
            </ul>
            {!collapsed && <Separator className="mt-3" />}
          </div>
        ))}
      </nav>
    </aside>
  )
}
