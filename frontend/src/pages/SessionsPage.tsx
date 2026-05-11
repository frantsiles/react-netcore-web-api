import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { sessionService } from '../services/sessionService';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { LogOut, Monitor, ShieldCheck } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

function parseUserAgent(ua: string): string {
  if (!ua) return 'Dispositivo desconocido';
  if (/Mobile|Android|iPhone|iPad/.test(ua)) return 'Dispositivo móvil';
  if (/Chrome/.test(ua)) return 'Chrome';
  if (/Firefox/.test(ua)) return 'Firefox';
  if (/Safari/.test(ua)) return 'Safari';
  if (/Edge/.test(ua)) return 'Edge';
  return 'Navegador desconocido';
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('es-AR', {
    dateStyle: 'short',
    timeStyle: 'short',
  });
}

export function SessionsPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: sessions, isLoading, error } = useQuery({
    queryKey: ['my-sessions'],
    queryFn: sessionService.getMySessions,
  });

  const revoke = useMutation({
    mutationFn: sessionService.revokeSession,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-sessions'] }),
  });

  const revokeAll = useMutation({
    mutationFn: sessionService.revokeAllMySessions,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-sessions'] }),
  });

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-10 border-b bg-card shadow-sm">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <div className="flex items-center gap-2">
            <ShieldCheck className="h-6 w-6 text-primary" />
            <span className="font-semibold text-foreground">Demo App</span>
          </div>
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="sm" onClick={() => navigate('/users')}>
              Usuarios
            </Button>
            <Button variant="outline" size="sm" onClick={() => void logout()}>
              <LogOut className="h-4 w-4" />
              Salir
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 py-8 space-y-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Monitor className="h-5 w-5 text-muted-foreground" />
            <h2 className="text-xl font-semibold text-foreground">Mis sesiones activas</h2>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => revokeAll.mutate()}
            disabled={revokeAll.isPending}
          >
            Cerrar todas las otras sesiones
          </Button>
        </div>

        {isLoading && (
          <div className="flex items-center gap-2 text-muted-foreground text-sm py-8">
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
            Cargando sesiones…
          </div>
        )}

        {error && (
          <div className="rounded-md bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
            No se pudo cargar la lista de sesiones.
          </div>
        )}

        {sessions && (
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Sesiones</CardTitle>
              <CardDescription>
                Sesión actual: <strong>{user?.email}</strong>
              </CardDescription>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Dispositivo</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">IP</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Creada</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Último uso</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {sessions.map((s) => (
                      <tr key={s.sessionId} className="hover:bg-muted/30 transition-colors">
                        <td className="px-6 py-4 font-medium text-foreground">
                          <div className="flex items-center gap-2">
                            {parseUserAgent(s.userAgent)}
                            {s.isCurrent && (
                              <Badge variant="secondary">Esta sesión</Badge>
                            )}
                          </div>
                        </td>
                        <td className="px-6 py-4 text-muted-foreground">{s.ipAddress}</td>
                        <td className="px-6 py-4 text-muted-foreground">{formatDate(s.createdAt)}</td>
                        <td className="px-6 py-4 text-muted-foreground">{formatDate(s.lastUsedAt)}</td>
                        <td className="px-6 py-4">
                          {!s.isCurrent && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => revoke.mutate(s.sessionId)}
                              disabled={revoke.isPending}
                            >
                              Revocar
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        )}
      </main>
    </div>
  );
}
