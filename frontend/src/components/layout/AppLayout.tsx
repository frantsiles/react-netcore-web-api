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
  const breadcrumb = useBreadcrumb()

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      <Sidebar collapsed={collapsed} />

      <div className="flex flex-1 flex-col overflow-hidden">
        <Topbar
          onToggleSidebar={() => setCollapsed(c => !c)}
          breadcrumb={breadcrumb}
        />
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>

      <Toaster richColors position="top-right" />
    </div>
  )
}
