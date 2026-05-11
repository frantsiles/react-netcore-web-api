# ADR-008: Stateful Refresh Tokens + SignalR Session Management

**Estado:** Aceptado  
**Fecha:** 2026-05-11

---

## Contexto

La implementación original usaba JWTs estáticos sin mecanismo de refresh, sin posibilidad de revocar sesiones individuales ni visibilidad de qué sesiones estaban activas en el sistema. Los problemas concretos:

1. Un token comprometido era válido hasta su expiración (60 min), sin forma de invalidarlo.
2. No había panel de administración para ver o revocar sesiones de otros usuarios.
3. No había notificación en tiempo real cuando una sesión era revocada.

---

## Decisión

Implementar **refresh tokens stateful** almacenados en base de datos con **revocación en tiempo real vía SignalR**.

### Arquitectura de tokens

- **Access Token (JWT):** vida de 15 minutos, contiene claim `sid` (session ID).
- **Refresh Token:** 256 bits aleatorios (`RandomNumberGenerator`), almacenado como SHA-256 hex en la BD (nunca el valor raw). Vida de 30 días.
- **Token Rotation:** cada uso del refresh token revoca la sesión actual y crea una nueva. Un refresh token solo puede usarse una vez.

### Flujo de autenticación

```
Login → (AccessToken 15min, RefreshToken 30d, SessionId)
         ↓ JWT expira
Refresh → (nuevo AccessToken, nuevo RefreshToken, nueva Session)
         ↓ usuario sale
Logout  → revoca la Session del RefreshToken
```

### Entidad Session (Api.Domain)

Almacena: `UserId`, `RefreshTokenHash`, `DeviceInfo` (UserAgent + IP), `ExpiresAt`, `LastUsedAt`, `RevokedAt`, `RevokedBy`, `RevokedReason`.

`IsActive` es computed: `RevokedAt is null && ExpiresAt > DateTime.UtcNow`.

---

## Por qué stateful vs stateless

Los refresh tokens stateless (JWT de larga duración) no permiten:
- Revocar una sesión específica sin invalidar todas las del usuario.
- Mostrar un historial auditable de sesiones.
- Detectar robo de tokens (un token robado puede usarse hasta que expire).

La contraparte aceptada: el `AccessToken` (15 min) sigue siendo válido tras revocar la sesión hasta que expire. Este tradeoff es aceptable porque la ventana es corta.

---

## Por qué SignalR vs alternativas

| Alternativa | Descartada porque |
|---|---|
| **Polling** | Latencia inaceptable para UX de seguridad (admin revoca → usuario sigue activo 30s) |
| **SSE (Server-Sent Events)** | Unidireccional — insuficiente para el dashboard admin que necesita bidireccionalidad |
| **JWT blocklist en Redis** | Introduce Redis como dependencia sin resolver la notificación en tiempo real |
| **SignalR** ✓ | Bidireccional, Azure SignalR Service como drop-in para producción, WebSocket con fallback automático |

### Azure SignalR Service

La implementación usa `AddSignalR()` en desarrollo y detecta `AzureSignalR:ConnectionString` para hacer switch automático a Azure SignalR Service en producción. No hay cambio de código, solo configuración.

---

## Por qué React conecta directo al API (no vía BFF) para SignalR

El BFF es un proxy REST. WebSockets/SignalR requieren una conexión persistente y estado de conexión que complica la arquitectura del BFF sin beneficio real:
- El AccessToken que React ya tiene es suficiente para autenticar el WebSocket.
- El BFF no añade valor en una conexión persistente.
- Esta excepción al patrón BFF está documentada aquí y en el código.

---

## Consecuencias

**Positivas:**
- Revocación individual de sesiones en tiempo real.
- Panel de administración con visibilidad completa de sesiones activas.
- Audit trail completo (`RevokedBy`, `RevokedReason`).
- Token rotation dificulta el reuso de tokens robados.

**Negativas:**
- El AccessToken (15 min) sigue válido tras revocar la sesión. Aceptado.
- SignalR añade complejidad de infraestructura (conexiones persistentes, grupos).
- La BD crece con sesiones históricas — requiere limpieza periódica en producción.

---

## Implementación

| Componente | Cambio |
|---|---|
| `Api.Domain/Sessions/` | `Session`, `DeviceInfo`, `SessionRevokedReason`, `ISessionRepository` |
| `Api.Infrastructure/` | `SessionRepository`, `Sha256TokenHasher`, `AppDbContext` |
| `Api.Application/Auth/` | `LoginCommand` (+ refresh token), `RefreshTokenCommand`, `LogoutCommand` |
| `Api.Application/Sessions/` | `GetMySessionsQuery`, `GetAllActiveSessionsQuery`, `RevokeSessionCommand` y variantes |
| `Api.WebApi/Hubs/` | `SessionHub`, `SignalRSessionNotifier` |
| `Api.WebApi/Controllers/` | `AuthController` (refresh/logout), `SessionsController` |
| `BFF.Application/` | `LoginBffCommand` (actualizado), `RefreshTokenBffCommand`, `LogoutBffCommand`, session queries/commands |
| `BFF.Api/Controllers/` | `AuthController` (refresh/logout), `SessionsController` |
| `frontend/` | `AuthContext`, `api.ts` (interceptor refresh), `signalRService.ts`, `SessionsPage`, `AdminSessionsPage` |
