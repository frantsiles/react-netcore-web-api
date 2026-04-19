import { useQuery } from '@tanstack/react-query';
import { userService } from '../services/userService';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { LogOut, Users, ShieldCheck } from 'lucide-react';

export function UsersPage() {
  const { user, logout } = useAuth();
  const { data: users, isLoading, error } = useQuery({
    queryKey: ['users'],
    queryFn: userService.getAll,
  });

  return (
    <div className="min-h-screen bg-background">

      {/* Header / Navbar */}
      <header className="sticky top-0 z-10 border-b bg-card shadow-sm">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <div className="flex items-center gap-2">
            <ShieldCheck className="h-6 w-6 text-primary" />
            <span className="font-semibold text-foreground">Demo App</span>
          </div>
          <div className="flex items-center gap-3">
            <span className="hidden text-sm text-muted-foreground sm:inline">
              Hola, <strong className="text-foreground">{user?.fullName}</strong>
            </span>
            <Button variant="outline" size="sm" onClick={logout}>
              <LogOut className="h-4 w-4" />
              Salir
            </Button>
          </div>
        </div>
      </header>

      {/* Contenido principal */}
      <main className="mx-auto max-w-5xl px-4 py-8 space-y-6">

        {/* Título de sección */}
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold text-foreground">Usuarios del sistema</h2>
        </div>

        {/* Estado: cargando */}
        {isLoading && (
          <div className="flex items-center gap-2 text-muted-foreground text-sm py-8">
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
            Cargando usuarios…
          </div>
        )}

        {/* Estado: error */}
        {error && (
          <div className="rounded-md bg-destructive/10 border border-destructive/20 px-4 py-3 text-sm text-destructive">
            No se pudo cargar la lista de usuarios. Verifica que el servidor esté activo.
          </div>
        )}

        {/* Tabla de usuarios */}
        {users && (
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Directorio</CardTitle>
              <CardDescription>{users.length} usuario{users.length !== 1 ? 's' : ''} registrado{users.length !== 1 ? 's' : ''}</CardDescription>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Nombre</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Email</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Roles</th>
                      <th className="px-6 py-3 text-left font-medium text-muted-foreground">Estado</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {users.map((u) => (
                      <tr key={u.id} className="hover:bg-muted/30 transition-colors">
                        <td className="px-6 py-4 font-medium text-foreground">
                          {u.firstName} {u.lastName}
                        </td>
                        <td className="px-6 py-4 text-muted-foreground">{u.email}</td>
                        <td className="px-6 py-4">
                          <div className="flex flex-wrap gap-1">
                            {u.roles.map((role) => (
                              <Badge key={role} variant="secondary">{role}</Badge>
                            ))}
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <Badge variant={u.isActive ? 'success' : 'outline'}>
                            {u.isActive ? 'Activo' : 'Inactivo'}
                          </Badge>
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
