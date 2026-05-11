import { createContext, useContext, useState, type ReactNode } from 'react';
import type { AuthUser, LoginRequest } from '../types/auth';
import { authService } from '../services/authService';

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  hasPermission: (permission: string) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const stored = sessionStorage.getItem('user');
    return stored ? JSON.parse(stored) : null;
  });

  const login = async (credentials: LoginRequest) => {
    const response = await authService.login(credentials);
    const authUser: AuthUser = {
      email: response.email,
      fullName: response.fullName,
      permissions: response.permissions,
      sessionId: response.sessionId,
    };
    sessionStorage.setItem('token', response.token);
    sessionStorage.setItem('refreshToken', response.refreshToken);
    sessionStorage.setItem('user', JSON.stringify(authUser));
    setUser(authUser);
  };

  const logout = async () => {
    const refreshToken = sessionStorage.getItem('refreshToken');
    if (refreshToken) {
      try {
        await authService.logout(refreshToken);
      } catch {
        // Best-effort — always clear local state
      }
    }
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('refreshToken');
    sessionStorage.removeItem('user');
    setUser(null);
  };

  const hasPermission = (permission: string) =>
    user?.permissions.includes(permission) ?? false;

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, login, logout, hasPermission }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
