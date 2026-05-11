import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

const HUB_URL = 'http://localhost:5002/hubs/sessions';

let connection: HubConnection | null = null;

function buildConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => sessionStorage.getItem('token') ?? '',
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        const delays = [1000, 2000, 5000, 10000, 30000];
        return delays[Math.min(retryContext.previousRetryCount, delays.length - 1)];
      },
    })
    .configureLogging(LogLevel.Warning)
    .build();
}

export async function startSessionHub(): Promise<HubConnection> {
  if (!connection) {
    connection = buildConnection();
  }

  if (connection.state === 'Disconnected') {
    await connection.start();
  }

  return connection;
}

export async function joinAdminGroup(): Promise<void> {
  if (connection) {
    await connection.invoke('JoinAdminGroup');
  }
}

export async function stopSessionHub(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}

export function useSessionHub(
  onSessionRevoked?: (sessionId: string, reason: string) => void,
  onNewSession?: (session: unknown) => void
) {
  const start = async () => {
    const hub = await startSessionHub();

    if (onSessionRevoked) {
      hub.on('SessionRevoked', (payload: { sessionId: string; reason: string }) => {
        onSessionRevoked(payload.sessionId, payload.reason);
      });
    }

    if (onNewSession) {
      hub.on('NewSession', (session: unknown) => {
        onNewSession(session);
      });
    }

    return hub;
  };

  const stop = () => stopSessionHub();

  return { start, stop };
}
