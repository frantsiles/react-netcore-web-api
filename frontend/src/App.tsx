import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AppLayout } from './components/layout/AppLayout';
import { LoginPage } from './pages/LoginPage';
import { UsersPage } from './pages/UsersPage';
import { SessionsPage } from './pages/SessionsPage';
import { AdminSessionsPage } from './pages/AdminSessionsPage';
import { UnauthorizedPage } from './pages/UnauthorizedPage';
import { AssistantPage } from './pages/AssistantPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { PartiesPage } from './features/parties/PartiesPage';
import { CatalogPage } from './features/catalog/CatalogPage';
import { SalesPage } from './features/sales/SalesPage';
import { PurchasingPage } from './features/purchasing/PurchasingPage';
import { InventoryPage } from './features/inventory/InventoryPage';
import { AccountingPage } from './features/accounting/AccountingPage';
import { BankingPage } from './features/banking/BankingPage';
import { TaxPage } from './features/tax/TaxPage';
import { ReportsPage } from './features/reports/ReportsPage';
import { ApprovalsPage } from './features/approvals/ApprovalsPage';
import { HRPage } from './features/hr/HRPage';

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
              <Route path="/sales" element={<SalesPage />} />
              <Route path="/purchasing" element={<PurchasingPage />} />
              <Route path="/inventory" element={<InventoryPage />} />

              {/* Finanzas */}
              <Route path="/accounting" element={<AccountingPage />} />
              <Route path="/banking" element={<BankingPage />} />
              <Route path="/tax" element={<TaxPage />} />
              <Route path="/reports" element={<ReportsPage />} />

              {/* Organización */}
              <Route path="/approvals" element={<ApprovalsPage />} />
              <Route path="/hr" element={<HRPage />} />

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
