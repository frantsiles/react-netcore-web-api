import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AppLayout } from './components/layout/AppLayout';
import { ComingSoon } from './components/layout/ComingSoon';
import { LoginPage } from './pages/LoginPage';
import { UsersPage } from './pages/UsersPage';
import { SessionsPage } from './pages/SessionsPage';
import { AdminSessionsPage } from './pages/AdminSessionsPage';
import { UnauthorizedPage } from './pages/UnauthorizedPage';
import { AssistantPage } from './pages/AssistantPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { PartiesPage } from './features/parties/PartiesPage';
import { CatalogPage } from './features/catalog/CatalogPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/unauthorized" element={<UnauthorizedPage />} />

            {/* ERP shell — all authenticated routes */}
            <Route
              element={
                <ProtectedRoute>
                  <AppLayout />
                </ProtectedRoute>
              }
            >
              <Route path="/dashboard" element={<DashboardPage />} />

              {/* Negocio */}
              <Route path="/parties" element={<PartiesPage />} />
              <Route path="/catalog" element={<CatalogPage />} />
              <Route path="/sales" element={<ComingSoon module="Ventas" />} />
              <Route path="/purchasing" element={<ComingSoon module="Compras" />} />
              <Route path="/inventory" element={<ComingSoon module="Inventario" />} />

              {/* Finanzas */}
              <Route path="/accounting" element={<ComingSoon module="Contabilidad" />} />
              <Route path="/banking" element={<ComingSoon module="Banca" />} />
              <Route path="/tax" element={<ComingSoon module="Impuestos" />} />
              <Route path="/reports" element={<ComingSoon module="Reportes" />} />

              {/* Organización */}
              <Route path="/approvals" element={<ComingSoon module="Aprobaciones" />} />
              <Route path="/hr" element={<ComingSoon module="Recursos Humanos" />} />

              {/* Admin (dentro del shell) */}
              <Route
                path="/users"
                element={
                  <ProtectedRoute requiredPermission="users:read">
                    <UsersPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/sessions" element={<SessionsPage />} />
              <Route
                path="/admin/sessions"
                element={
                  <ProtectedRoute requiredPermission="sessions:admin">
                    <AdminSessionsPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/assistant" element={<AssistantPage />} />
            </Route>

            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}
