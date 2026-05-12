# ADR-009: Outbox Pattern + Idempotency Key para User Commands

**Estado:** Aceptado
**Fecha:** 2026-05-12

---

## Contexto

La Fase 2 introduce comandos mutantes sobre el agregado `User` (crear,
desactivar, cambiar de rol) que deben publicar eventos al bus
(`UserCreated`, `UserDeleted`, `UserRoleChanged`) para que el Worker
los consuma. Hasta ahora, ningún servicio del API publicaba al bus.

Dos problemas concretos había que resolver antes de exponer los
endpoints:

1. **Pérdida silenciosa de eventos.** Si un handler hace
   `SaveChangesAsync` y luego `IPublishEndpoint.Publish`, una caída del
   bus entre ambas operaciones deja la BD en un estado que el resto del
   sistema nunca observa. El reverso —publicar antes de commit— deja
   eventos huérfanos cuando la BD revierte.
2. **Reintentos de red duplicando recursos.** Un POST con
   `connection reset` en el cliente puede repetirse, creando dos
   usuarios o disparando dos eventos por la misma intención.

---

## Decisión

### Outbox transaccional sobre MassTransit + EF Core

Usar `MassTransit.EntityFrameworkCore` con el Outbox nativo:

```csharp
x.AddEntityFrameworkOutbox<AppDbContext>(o =>
{
    o.UsePostgres();
    o.UseBusOutbox();
});
```

y declarar las tres tablas requeridas en `OnModelCreating`:

```csharp
builder.AddInboxStateEntity();
builder.AddOutboxStateEntity();
builder.AddOutboxMessageEntity();
```

Cuando un handler publica dentro de la unidad de trabajo de EF, el
mensaje se inserta en `OutboxMessage` en la misma transacción que el
agregado. El background dispatcher de MassTransit lee la tabla y
publica al broker — garantía at-least-once sin pérdida.

Los handlers no conocen MassTransit: dependen de `IEventPublisher`
(definida en Application) cuya implementación concreta
`MassTransitEventPublisher` vive en Infrastructure.

### Idempotency Key como middleware

Un `IdempotencyMiddleware` intercepta peticiones mutantes
(POST/PUT/PATCH/DELETE) con el header `X-Idempotency-Key`. La cache,
indexada por `{Method}:{Path}:{key}`, devuelve la respuesta original
sin re-ejecutar el pipeline. Sólo se persisten respuestas 2xx, así
los errores transitorios no se congelan en cache.

`IIdempotencyCache` se implementa en memoria sobre `IMemoryCache` con
TTL de 24 horas.

---

## Por qué Outbox nativo de MassTransit vs implementación propia

| Aspecto | MassTransit Outbox | Implementación manual |
|---|---|---|
| Battle-tested | sí, miles de proyectos | no |
| Transaccional con EF | nativo (mismo DbContext) | trabajo de plomería |
| Dispatcher en background | incluido y configurable | hay que escribirlo |
| Cleanup de mensajes despachados | gestionado por MassTransit | hay que escribirlo |
| Curva de aprendizaje | API conocida si ya usas MassTransit | toda la complejidad propia |

No hay ventaja en reinventar la rueda. La librería ya integra con
Npgsql (`o.UsePostgres()`) y con el mismo `IPublishEndpoint` que
exponemos vía `IEventPublisher`.

### Tradeoff aceptado

Las tablas `OutboxMessage`/`InboxState`/`OutboxState` crecen con el
volumen de eventos. MassTransit limpia mensajes despachados
automáticamente; aún así, en producción real conviene monitorizar el
tamaño de las tres tablas y ajustar el intervalo de cleanup si la BD
se llena.

---

## Por qué Idempotency Key en middleware vs por handler

Si la idempotencia se aplica handler por handler, hay que duplicar
lógica (lookup, captura de response, cache write) en cada comando que
mute estado. Un middleware:

- Cubre todos los endpoints mutantes con una sola pieza de código.
- Decide en función del método HTTP, no del comando MediatR, así
  futuros endpoints heredan el comportamiento sin tocarlos.
- Mantiene los handlers libres de cross-cutting concerns.

### Tradeoff aceptado

`InMemoryIdempotencyCache` no sobrevive a reinicios del proceso, por lo
que un cliente que reintenta tras un reinicio del API perdería el
hit y crearía el recurso por segunda vez. Para esta demo es
aceptable: la ventana de reinicio es corta y el caso de uso es
"red poco confiable", no "API caído". En producción real, sustituir
por `IDistributedCache` apuntando a Redis es un cambio de una línea
(`services.AddStackExchangeRedisCache(...)`) en `DependencyInjection`.

---

## Alternativas descartadas

| Alternativa | Descartada porque |
|---|---|
| **Publicación directa sin Outbox** | Pierde eventos si el bus está caído justo al final de la transacción |
| **Implementación manual del Outbox** | Reinventa lo que MassTransit ya resuelve, sin ganancia |
| **Outbox con polling SQL artesanal** | El dispatcher de MassTransit ya lo hace con backoff y locks |
| **Idempotencia por handler (decorador MediatR)** | Mismo efecto pero acoplado al pipeline de MediatR; un cliente que llama un endpoint que no pasa por MediatR queda sin protección |
| **Redis IDistributedCache** desde el día 1 | Introduce dependencia de infraestructura para un demo; el cambio es trivial cuando se necesite |

---

## Consecuencias

**Positivas:**

- Cero pérdida de eventos: si la BD se compromete, el evento se
  publicará tarde o temprano.
- Reintentos seguros: dos POST idénticos con la misma key devuelven
  la misma respuesta sin duplicar usuarios.
- Handlers limpios: sin conocimiento del bus ni de la cache.

**Negativas:**

- Tres tablas extra en la BD que crecen y requieren monitorización.
- Latencia ligeramente superior: la publicación no es síncrona al
  broker, sino al outbox y luego al broker.
- La cache en memoria no es válida para entornos multi-instancia: si
  hay más de una réplica del API detrás del Gateway, una idempotency
  key vista por la instancia A no la verá la instancia B. Para esto
  hace falta Redis.

---

## Implementación

| Componente | Cambio |
|---|---|
| `src/Shared/Shared.Messages/` | Proyecto nuevo con los records `UserCreated`/`UserDeleted`/`UserRoleChanged` reutilizados por API y Worker |
| `Api.Application/Common/Interfaces/` | `IEventPublisher`, `IIdempotencyCache`, `IdempotencyEntry` |
| `Api.Application/Users/Commands/` | `CreateUser`, `DeleteUser`, `ChangeUserRole` (command + validator + handler) |
| `Api.Domain/Users/` | `User.ChangeRole(Role)` |
| `Api.Infrastructure/Messaging/` | `MassTransitEventPublisher` |
| `Api.Infrastructure/Caching/` | `InMemoryIdempotencyCache` |
| `Api.Infrastructure/DependencyInjection.cs` | `AddMessaging` (MassTransit + Outbox EF + transports), `IIdempotencyCache` |
| `Api.Infrastructure/Persistence/AppDbContext.cs` | `AddInboxStateEntity`/`AddOutboxStateEntity`/`AddOutboxMessageEntity` |
| `Api.Infrastructure/Persistence/Migrations/` | `AddMassTransitOutbox` |
| `Api.WebApi/Middleware/IdempotencyMiddleware.cs` | Header `X-Idempotency-Key` para POST/PUT/PATCH/DELETE |
| `Api.WebApi/Controllers/UsersController.cs` | `POST /api/users`, `DELETE /api/users/{id}`, `PATCH /api/users/{id}/role` (`[Authorize(Roles="Admin")]`) |
| `BFF.Application/Users/` | `CreateUserBffCommand`, `DeleteUserBffCommand`, `ChangeUserRoleBffCommand` |
| `BFF.Api/Controllers/UsersController.cs` | Proxies `POST/DELETE/PATCH /bff/users` |
| `BFF.Infrastructure/Services/ApiClient.cs` | Sobrecarga `PatchAsync<TRequest,TResponse>` |
