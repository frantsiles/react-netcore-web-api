import { Construction } from 'lucide-react'
import { Card } from '@/components/ui/card'

interface ComingSoonProps {
  module: string
}

export function ComingSoon({ module }: ComingSoonProps) {
  return (
    <div className="flex h-full items-center justify-center">
      <Card className="max-w-sm p-10 text-center">
        <Construction className="mx-auto mb-4 h-12 w-12 text-muted-foreground" />
        <h2 className="text-lg font-semibold">{module}</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Este módulo está en construcción. Pronto estará disponible.
        </p>
      </Card>
    </div>
  )
}
