import { useRef, useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { askAssistant } from '../services/assistantService';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { LogOut, Bot, ShieldCheck, Users, Monitor, Send, X } from 'lucide-react';

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
}

const EXAMPLE_CHIPS = [
  { label: '👥 Gestionar usuarios', question: '¿Cuántos usuarios activos hay en el sistema?' },
  { label: '🔐 Ver sesiones', question: '¿Cuántas sesiones activas hay ahora mismo?' },
  { label: '📊 Estado del sistema', question: 'Dame un resumen del estado del sistema.' },
];

export function AssistantPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const cancelRef = useRef<(() => void) | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = useCallback(() => {
    const question = input.trim();
    if (!question || isStreaming) return;

    const userMsg: Message = { id: crypto.randomUUID(), role: 'user', content: question };
    const assistantId = crypto.randomUUID();
    const assistantMsg: Message = { id: assistantId, role: 'assistant', content: '' };

    setMessages(prev => [...prev, userMsg, assistantMsg]);
    setInput('');
    setIsStreaming(true);

    cancelRef.current = askAssistant(
      question,
      chunk => {
        setMessages(prev =>
          prev.map(m =>
            m.id === assistantId ? { ...m, content: m.content + chunk } : m,
          ),
        );
      },
      () => {
        setIsStreaming(false);
        cancelRef.current = null;
      },
      err => {
        setMessages(prev =>
          prev.map(m =>
            m.id === assistantId
              ? { ...m, content: `Error: ${err.message}` }
              : m,
          ),
        );
        setIsStreaming(false);
        cancelRef.current = null;
      },
    );
  }, [input, isStreaming]);

  const handleCancel = () => {
    cancelRef.current?.();
    setIsStreaming(false);
    cancelRef.current = null;
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Header */}
      <header className="sticky top-0 z-10 border-b bg-card shadow-sm">
        <div className="mx-auto flex max-w-3xl items-center justify-between px-4 py-3">
          <div className="flex items-center gap-2">
            <ShieldCheck className="h-6 w-6 text-primary" />
            <span className="font-semibold text-foreground">Demo App</span>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={() => navigate('/users')}>
              <Users className="h-4 w-4" />
              Usuarios
            </Button>
            <Button variant="ghost" size="sm" onClick={() => navigate('/sessions')}>
              <Monitor className="h-4 w-4" />
              Sesiones
            </Button>
            <Button variant="outline" size="sm" onClick={() => void logout()}>
              <LogOut className="h-4 w-4" />
              Salir
            </Button>
          </div>
        </div>
      </header>

      {/* Chat area */}
      <main className="flex-1 overflow-y-auto px-4 py-6">
        <div className="mx-auto max-w-3xl space-y-4">
          {messages.length === 0 && (
            <div className="flex flex-col items-center gap-3 pt-16 text-muted-foreground">
              <Bot className="h-12 w-12" />
              <p className="text-lg font-medium text-foreground">Asistente IA</p>
              <p className="text-sm text-center max-w-sm">
                Pregúntame sobre los usuarios o sesiones del sistema.
                Puedo consultar datos reales y realizar acciones.
              </p>
            </div>
          )}

          {messages.map(msg => (
            <div
              key={msg.id}
              className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
            >
              <Card
                className={`max-w-[80%] px-4 py-3 text-sm whitespace-pre-wrap break-words ${
                  msg.role === 'user'
                    ? 'bg-primary text-primary-foreground'
                    : 'bg-muted text-foreground'
                }`}
              >
                {msg.content}
                {msg.role === 'assistant' && isStreaming && msg === messages[messages.length - 1] && (
                  <span className="inline-block w-1.5 h-4 bg-current ml-0.5 animate-pulse" />
                )}
              </Card>
            </div>
          ))}
          <div ref={bottomRef} />
        </div>
      </main>

      {/* Input area */}
      <div className="border-t bg-card px-4 py-4">
        <div className="mx-auto max-w-3xl space-y-3">
          {/* Chips */}
          <div className="flex flex-wrap gap-2">
            {EXAMPLE_CHIPS.map(chip => (
              <button
                key={chip.label}
                onClick={() => setInput(chip.question)}
                disabled={isStreaming}
                className="rounded-full border px-3 py-1 text-xs text-muted-foreground hover:bg-muted disabled:opacity-50 transition-colors"
              >
                {chip.label}
              </button>
            ))}
          </div>

          {/* Textarea + buttons */}
          <div className="flex gap-2 items-end">
            <textarea
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              disabled={isStreaming}
              placeholder="Escribe tu pregunta… (Enter para enviar, Shift+Enter para nueva línea)"
              rows={2}
              className="flex-1 resize-none rounded-md border bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring disabled:opacity-50"
            />
            {isStreaming ? (
              <Button variant="outline" size="icon" onClick={handleCancel} title="Cancelar">
                <X className="h-4 w-4" />
              </Button>
            ) : (
              <Button size="icon" onClick={handleSend} disabled={!input.trim()} title="Enviar">
                <Send className="h-4 w-4" />
              </Button>
            )}
          </div>

          <p className="text-xs text-muted-foreground text-center">
            Conectado como <strong>{user?.fullName}</strong> · Agente con acceso a datos reales del sistema
          </p>
        </div>
      </div>
    </div>
  );
}
