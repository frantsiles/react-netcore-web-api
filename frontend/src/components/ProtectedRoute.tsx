import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
  requiredPermission?: string;
}

/**
 * Redirects to /login if the user is not authenticated.
 * Optionally checks for a specific permission and redirects to /unauthorized.
 */
export function ProtectedRoute({ children, requiredPermission }: Props) {
  const { isAuthenticated, hasPermission } = useAuth();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}
