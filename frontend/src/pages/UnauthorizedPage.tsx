import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { ShieldOff, ArrowLeft } from 'lucide-react';

export function UnauthorizedPage() {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <div className="flex flex-col items-center gap-4 text-center max-w-sm">

        {/* Ícono de error */}
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
          <ShieldOff className="h-8 w-8 text-destructive" />
        </div>

        <div className="space-y-1">
          <h1 className="text-2xl font-bold text-foreground">Acceso denegado</h1>
          <p className="text-sm text-muted-foreground">
            No tienes los permisos necesarios para ver esta página.
          </p>
        </div>

        <Button variant="outline" onClick={() => navigate(-1)}>
          <ArrowLeft className="h-4 w-4" />
          Volver
        </Button>

      </div>
    </div>
  );
}
