import { useEffect, useRef } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { sessionService } from '../services/sessionService';
import { joinAdminGroup, startSessionHub, stopSessionHub } from '../services/signalRService';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { LogOut, ShieldCheck, Activity } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import type { AdminSession } from '../types/session';

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('es-AR', {
    dateStyle: 'short',
    timeStyle: 'short',
  });
}

export function AdminSessionsPage() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const hubStarted = useRef(false);

  const { data: sessions, isLoading, error } = useQuery({
    queryKey: ['admin-sessions'],
    queryFn: sessionService.getAllSessions,
  });

  useEffect(() => {
    if (hubStarted.current) return;
    hubStarted.current = true;

    startSessionHub().then(async (hub) => {
      await joinAdminGroup();

      hub.on('NewSession', () => {
        queryClient.invalidateQueries({ queryKey: ['admin-sessions'] });
      });

      hub.on('SessionRevoked', () => {
        queryClient.invalidateQueries({ queryKey: ['admin-sessions'] });
      });
    });

    return () => {
      stopSessionHub();
    };
  }, [queryClient]);

  const revokeSession = useMutation({
    mutationFn: sessionService.revokeSession,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-sessions'] }),
  });

  const revokeUserSessions = useMutation({
    mutationFn: (userId: string) => sessionService.revokeAllUserSessions(userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-sessions'] }),
  });

  const sessionsByUser = sessions?.reduce<Record<string, AdminSession[]>>((acc, s) => {
    if (!acc[s.userId]) acc[s.userId] = [];
    acc[s.userId].push(s);
    return acc;
  }, {});

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-10 border-b bg-card shadow-sm">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <div className="flex items-center gap-2">
            <ShieldCheck className="h-6 w-6 text-primary" />
            <span className="font-semibold text-foreground">Demo App</span>
            <Badge variant="secondary">Admin</Badge>
          </div>
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="sm" onClick={() => navigate('/users')}>
              Usuarios
            </Button>
            <Button variant="ghost" size="sm" onClick={() => navigate('/sessions')}>
              Mis sesiones
            </Button>
            <Button variant="outline" size="sm" onClick={() => void logout()}>
              <LogOut className="h-4 w-4" />
              Salir
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8 space-y-6">
        <div className="flex items-center gap-2">
          <Activity className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold text-foreground">Sesiones activas del sistema</h2>
          <Badge variant="outline" className="ml-2">
            {sessions?.length ?? 0} sesiones
          </Badge>
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

        {sessionsByUser && Object.entries(sessionsByUser).map(([userId, userSessions]) => {
          const first = userSessions[0];
          return (
            <Card key={userId}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="text-base">{first.userFullName}</CardTitle>
                    <CardDescription>{first.userEmail}</CardDescription>
                  </div>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => revokeUserSessions.mutate(userId)}
                    disabled={revokeUserSessions.isPending}
                  >
                    Revocar todas
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="p-0">
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="px-6 py-3 text-left font-medium text-muted-foreground">IP</th>
                        <th className="px-6 py-3 text-left font-medium text-muted-foreground">Creada</th>
                        <th className="px-6 py-3 text-left font-medium text-muted-foreground">Último uso</th>
                        <th className="px-6 py-3 text-left font-medium text-muted-foreground"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {userSessions.map((s) => (
                        <tr key={s.sessionId} className="hover:bg-muted/30 transition-colors">
                          <td className="px-6 py-4 text-muted-foreground">{s.ipAddress}</td>
                          <td className="px-6 py-4 text-muted-foreground">{formatDate(s.createdAt)}</td>
                          <td className="px-6 py-4 text-muted-foreground">{formatDate(s.lastUsedAt)}</td>
                          <td className="px-6 py-4">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => revokeSession.mutate(s.sessionId)}
                              disabled={revokeSession.isPending}
                            >
                              Revocar sesión
                            </Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </main>
    </div>
  );
}
