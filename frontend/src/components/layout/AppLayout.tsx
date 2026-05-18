import { useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'
import { Toaster } from 'sonner'
import { navGroups } from './nav-items'

function useBreadcrumb() {
  const location = useLocation()
  for (const group of navGroups) {
    for (const item of group.items) {
      if (location.pathname.startsWith(item.href)) return item.title
    }
  }
  return ''
}

export function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const breadcrumb = useBreadcrumb()

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {/* Mobile backdrop */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Sidebar — hidden on mobile unless drawer is open */}
      <div
        className={[
          'fixed inset-y-0 left-0 z-50 md:relative md:block md:z-auto transition-transform duration-300',
          mobileOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0',
        ].join(' ')}
      >
        <Sidebar
          collapsed={collapsed}
          onClose={() => setMobileOpen(false)}
        />
      </div>

      <div className="flex flex-1 flex-col overflow-hidden min-w-0">
        <Topbar
          onToggleSidebar={() => {
            // On mobile open/close drawer; on desktop collapse/expand
            if (window.innerWidth < 768) setMobileOpen(o => !o)
            else setCollapsed(c => !c)
          }}
          breadcrumb={breadcrumb}
        />
        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          <Outlet />
        </main>
      </div>

      <Toaster richColors position="top-right" />
    </div>
  )
}
