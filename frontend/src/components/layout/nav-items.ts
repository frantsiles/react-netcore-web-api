import {
  LayoutDashboard,
  Users2,
  Package,
  ShoppingCart,
  Warehouse,
  ShoppingBag,
  BookOpen,
  Landmark,
  PercentSquare,
  CheckSquare,
  BarChart3,
  Users,
} from 'lucide-react'

export interface NavItem {
  title: string
  href: string
  icon: React.ComponentType<{ className?: string }>
  children?: NavItem[]
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

export const navGroups: NavGroup[] = [
  {
    label: 'General',
    items: [
      { title: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },
    ],
  },
  {
    label: 'Negocio',
    items: [
      { title: 'Clientes y Proveedores', href: '/parties', icon: Users2 },
      { title: 'Catálogo', href: '/catalog', icon: Package },
      { title: 'Ventas', href: '/sales', icon: ShoppingCart },
      { title: 'Compras', href: '/purchasing', icon: ShoppingBag },
      { title: 'Inventario', href: '/inventory', icon: Warehouse },
    ],
  },
  {
    label: 'Finanzas',
    items: [
      { title: 'Contabilidad', href: '/accounting', icon: BookOpen },
      { title: 'Banca', href: '/banking', icon: Landmark },
      { title: 'Impuestos', href: '/tax', icon: PercentSquare },
      { title: 'Reportes', href: '/reports', icon: BarChart3 },
    ],
  },
  {
    label: 'Organización',
    items: [
      { title: 'Aprobaciones', href: '/approvals', icon: CheckSquare },
      { title: 'Recursos Humanos', href: '/hr', icon: Users },
    ],
  },
]
