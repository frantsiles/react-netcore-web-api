# ADR-002 — DDD + CQRS + MediatR en el Backend API

| Campo | Valor |
|-------|-------|
| Estado | Aceptado |
| Fecha | 2025-04 |
| Ámbito | Arquitectura interna del Backend API |

---

## Contexto

El Backend API es el núcleo de lógica de negocio del sistema. Necesita una estructura interna que:
- Permita testear la lógica de negocio de forma aislada (sin levantar HTTP).
- Haga explícita la separación entre "qué piden los clientes" (Application) y "cómo se almacena" (Infrastructure).
- Escale con el equipo: un desarrollador nuevo debe poder añadir un caso de uso sin entender toda la base de código.

## Decisión

Estructurar el API con **Domain-Driven Design** en cuatro layers y **CQRS** implementado con **MediatR**:

```
Api.Domain       — Entidades, Value Objects, interfaces de repositorio
Api.Application  — Commands, Queries, Handlers MediatR, validadores FluentValidation
Api.Infrastructure — Implementaciones de repositorios (EF Core), JWT, BCrypt
Api.WebApi       — Controllers HTTP: solo orquesta, nunca contiene lógica
```

Regla de dependencia: cada layer solo puede referenciar al layer anterior. `Api.WebApi` no referencia `Api.Infrastructure` directamente.

Los controllers son deliberadamente delgados:
```csharp
[HttpGet]
public async Task<IActionResult> GetUsers(
    [FromQuery] GetUsersQuery query, ISender sender)
    => Ok(await sender.Send(query));
```

## Consecuencias

**Positivas:**
- Los handlers MediatR son clases POCO testeables con `new Handler(mockRepo)` sin necesidad de levantar el servidor.
- FluentValidation en los Commands/Queries centraliza la validación — los controllers no validan nada.
- El pipeline de MediatR permite añadir behaviors transversales (logging, validación automática) sin tocar los handlers.
- La separación Domain/Infrastructure permite cambiar EF Core InMemory por PostgreSQL sin tocar `Api.Application`.

**Negativas:**
- Para un CRUD simple, la indirección (controller → mediator → handler → repository) puede parecer excesiva.
- MediatR añade una dependencia y un nivel de indirección que hace el debugging inicial menos obvio (el stack trace no muestra la cadena de llamadas directamente).

## Alternativas descartadas

**Controllers gordos (Minimal API o MVC tradicional):** Para un proyecto de esta escala habría sido viable. Se descartó porque el objetivo es demostrar patrones de producción, no el camino más corto.

**REPR pattern (Request-Endpoint-Response) con FastEndpoints:** Alternativa moderna interesante, pero MediatR es más establecido en el ecosistema .NET empresarial y el objetivo es legibilidad para cualquier desarrollador .NET senior.
