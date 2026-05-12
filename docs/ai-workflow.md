# Ingeniería AI-augmented: cómo se construyó este proyecto

Este documento describe el rol real que jugó la IA en el desarrollo de este repositorio. No es un ejercicio de marketing — es un registro honesto de qué funcionó, qué no, y dónde el juicio humano fue irreemplazable.

---

## La filosofía de trabajo

> La IA amplifica la velocidad de ejecución. El ingeniero toma las decisiones.

Usar IA de forma efectiva no significa delegar el pensamiento — significa eliminar la fricción entre "sé lo que quiero construir" y "tengo el código funcionando". La diferencia entre un developer AI-augmented y uno que solo usa autocompletado está en saber **cuándo confiar, cuándo cuestionar y cuándo descartar** la salida del modelo.

Este proyecto usó dos herramientas principales:

| Herramienta | Rol | Casos de uso |
|-------------|-----|-------------|
| **Claude Code** (CLI) | Agente principal | Scaffolding, arquitectura, refactoring, infra, documentación |
| **GitHub Copilot** (IDE) | Asistente inline | Completado de boilerplate, tests unitarios, expresiones LINQ |

---

## El workflow real por fase

### Fase 1 — Diseño de arquitectura

Antes de escribir una línea de código, usé Claude Code para **validar decisiones arquitectónicas**, no para generarlas. El proceso:

1. Describía el problema de negocio y las restricciones (demo pública, zero external deps en local, demuestra cloud-native).
2. Pedía un análisis de opciones con pros/contras explícitos.
3. Tomaba la decisión yo, documentaba el razonamiento.

**Ejemplo concreto:** Para el patrón BFF, la pregunta no fue "qué debo usar", sino "en un contexto donde React nunca debe saber que existe un API independiente, y donde el equipo de frontend puede evolucionar sin tocar el contrato backend, ¿qué trade-offs tiene BFF vs API Gateway con políticas CORS?". La respuesta ayudó a articular mejor lo que ya intuía — y generó el esqueleto de [ADR-001](adr/ADR-001-bff-pattern.md).

### Fase 2 — Scaffolding de proyectos .NET

El scaffolding en .NET con DDD + CQRS tiene mucho boilerplate repetitivo: crear los proyectos, referenciar los layers correctos en el `.slnx`, crear los handlers MediatR, los DTOs, los validadores FluentValidation. Claude Code generó el 80% de ese scaffolding.

**Prompt tipo:**
```
Crea un handler MediatR para el query GetUsers en el proyecto Api.Application. 
Sigue el patrón del handler CreateUser que ya existe: record Request, record Response, 
clase Handler que implementa IRequestHandler. El repositorio es IUserRepository.
```

**Dónde el AI se equivocó:**
- Generó `IMediator.Send()` en los controllers en lugar de usar el patrón de inyección directa de `ISender`. Corrección: cambié todos los controllers para inyectar `ISender` (interfaz más acotada).
- Puso la lógica de BCrypt en el handler en lugar de en un servicio de infraestructura. Corrección: extraje `IPasswordHasher` al layer de Application como interfaz, implementación en Infrastructure.

### Fase 3 — Frontend React

El frontend fue la fase donde el AI-assisted fue más visible. Copilot completaba componentes Radix UI y Tailwind v4 con alta precisión porque el patrón es muy estructurado. Claude Code ayudó con:

- **Configuración de Vite proxy**: el primer intento hardcodeaba la URL del BFF. Detecté el problema, expliqué la restricción ("nunca URLs hardcodeadas, debe funcionar en Codespaces"), y Claude regeneró usando rutas relativas.
- **AuthContext con sessionStorage**: generado correctamente en el primer intento, incluyendo el interceptor Axios para el 401.
- **TanStack Query**: Copilot completaba los `useQuery` hooks con el staleTime y los queryKeys correctos una vez que vio el patrón del primer hook.

### Fase 4 — Infraestructura como código

Esta fue la fase de mayor valor del AI-augmented workflow. Bicep, Kustomize y Docker multi-stage son herramientas con sintaxis densa y documentación extensa — el modelo tiene conocimiento profundo de todos ellos.

**Ejemplo: Bicep para AKS con AcrPull role assignment**

La asignación de roles RBAC entre AKS y ACR en Bicep requiere sintaxis específica con `principalId` del kubelet identity. Claude Code generó el módulo correctamente, pero **omitió** la dependencia explícita entre el role assignment y el AKS deployment — lo que habría causado un race condition en deploys paralelos. Añadí `dependsOn` manualmente después de revisar el módulo.

**Ejemplo: OTel Collector pipeline**

La configuración YAML del OTel Collector para rutar traces → Tempo, metrics → Prometheus y logs → Loki tiene mucha friccación de sintaxis. El modelo generó la pipeline completa en un solo intento. Solo ajusté los `batch` processors para reducir la frecuencia de flush en el entorno de desarrollo.

**Ejemplo: Kustomize overlays**

Generados completamente por Claude Code: base + overlays local y azure. La única corrección fue que el overlay azure necesitaba el prefijo de imagen de ACR con el registry completo (`<acr-name>.azurecr.io/`), que el modelo dejó como placeholder sin advertir. Ahora está documentado.

### Fase 5 — Tests

**Unit tests (xUnit + Moq + FluentAssertions):**
Copilot generaba el 90% del test body una vez que tenía el Arrange. Lo más valioso: los casos edge que el developer hubiera omitido por obvios (null inputs, empty collections, boundary values). Revisé cada test para asegurar que probaba comportamiento y no implementación.

**Integration tests (WebApplicationFactory):**
Claude Code ayudó a configurar el `CustomWebApplicationFactory` con la base de datos InMemory aislada por test. El primer intento compartía el estado entre tests — lo detecté porque los tests fallaban en orden aleatorio. Corrección: mover el `SeedData()` dentro del `IClassFixture` con scope correcto.

**E2E (Playwright):**
Los Page Object Models los generó Claude Code a partir de la descripción de las páginas. Los assertions fueron los más revisados — el modelo tendía a usar `toBeVisible()` donde `toContainText()` era más semánticamente correcto.

### Fase 6 — Observabilidad

La integración de OpenTelemetry en los `Program.cs` de cada servicio fue generada casi enteramente por Claude Code siguiendo la instrucción:

```
Añade OTel SDK al Program.cs del Api.WebApi. Necesito: trazas HTTP con AddAspNetCoreInstrumentation, 
métricas de runtime con AddRuntimeInstrumentation, y que el endpoint se lea de la config 
"Otel:Endpoint". Serilog debe enviar también al OTel Collector usando el sink OpenTelemetry.
```

La configuración fue correcta. Lo que ajusté: el `ResourceBuilder.CreateDefault()` para que el `service.name` en cada servicio fuera diferente (importante para el Service Graph en Grafana).

---

## Lecciones sobre cuándo confiar vs. corregir

### Confiar sin revisar (patrón establecido + bien documentado)
- Boilerplate MediatR handlers
- Configuración JWT Bearer en ASP.NET Core
- Completado de componentes React con Radix UI
- Comandos Docker y kubectl
- Expresiones LINQ sobre colecciones

### Revisar siempre (lógica de negocio + contratos)
- Validadores FluentValidation (el modelo no conoce tus reglas de negocio)
- Contratos de mensajes MassTransit (los records que cruzan servicios)
- RBAC y permisos en Bicep (los role assignments tienen gotchas)
- Tests de integración (el scope del estado compartido es difícil de razonar para el modelo)

### Descartar y rehacer (juicio arquitectónico)
- Decisiones de layering (el modelo tiende a poner lógica en la capa equivocada si no se le da contexto suficiente)
- Nombres de conceptos de dominio (el modelo usa nombres genéricos; los nombres de dominio los define el developer)
- Estrategias de retry/circuit breaker (requieren conocer el SLA real, que el modelo no tiene)

---

## Qué demuestra este workflow

1. **Lectura crítica de código generado** — saber que el código compila no significa que sea correcto.
2. **Prompts precisos** — la calidad del output es directamente proporcional a la precisión del contexto que se proporciona.
3. **Arquitectura primero** — el AI es más útil cuando el diseño ya está resuelto. Usarlo para diseñar es un antipatrón.
4. **Velocidad real** — las fases de scaffolding e infra que habrían tomado días se completaron en horas, sin sacrificar calidad gracias a la revisión sistemática.
5. **Documentación AI-assisted** — incluidos los ADRs de este repositorio, que se generaron a partir de las notas de decisión que fui tomando durante el desarrollo.

---

## Herramientas y configuración

### Claude Code

Configurado via [`CLAUDE.md`](../CLAUDE.md) en la raíz del repo. Define:
- Arquitectura del sistema para que el modelo tenga contexto completo
- Comandos de desarrollo para que pueda ejecutarlos
- Convención de commits para que los genere correctamente
- Skill `/infra` para ayuda contextual con Docker/K8s/Azure

El skill `/infra` es un comando personalizado en [`.claude/commands/infra.md`](../.claude/commands/infra.md) que le da al modelo instrucciones específicas sobre cómo ayudar con infraestructura en este proyecto.

### GitHub Copilot

Configurado en [`.vscode/settings.json`](../.vscode/settings.json) para usar las instrucciones de commit del repositorio. Copilot Chat tiene acceso al mismo estándar de mensajes de commit que Claude Code.

### El loop de trabajo típico

```
1. Developer: describe el problema en lenguaje natural
2. Claude/Copilot: genera primera propuesta
3. Developer: revisa, identifica los problemas
4. Developer: da feedback preciso sobre los problemas
5. Claude/Copilot: itera
6. Developer: acepta, ajusta manualmente, o descarta
7. Developer: hace commit con mensaje que explica el porqué
```

El paso 3 y 6 son intransferibles al modelo. Son el trabajo de ingeniería.

---

## Fase 7 — Session Management con SignalR

Esta fase implementó refresh tokens stateful + revocación en tiempo real vía SignalR. La complejidad técnica fue la mayor del proyecto hasta ahora.

### Qué generó Claude Code

**Domain layer completo:** `Session`, `DeviceInfo` (value object), `ISessionRepository`. El modelo respetó el patrón de `Entity` y `ValueObject` existentes sin desviaciones. Primer intento correcto.

**Application layer:** Los 7 handlers (LoginCommand actualizado, RefreshTokenCommand, LogoutCommand, GetMySessionsQuery, GetAllActiveSessionsQuery, RevokeSessionCommand y variantes). Correctos en estructura. El único ajuste necesario fue en `RefreshTokenCommandHandler`: el modelo originalmente mantenía la misma `DeviceInfo` del IP de la sesión vieja — revisé que era correcto preservar el IP pero actualizar el UserAgent desde el request actual.

**Infrastructure:** `SessionRepository`, `Sha256TokenHasher`, configuración de `AppDbContext`. Correctos en primer intento. El modelo sabía configurar `OwnsOne` para el value object y el `HasConversion<string>` para el enum.

**SignalR + WebApi:** El modelo resolvió correctamente el problema de dependencia circular (Infrastructure no puede referenciar WebApi) ubicando `SignalRSessionNotifier` en el proyecto WebApi. Este es exactamente el tipo de juicio arquitectónico donde el modelo sorprendió positivamente.

**BFF:** Los nuevos proxies y la extensión de `IApiClient` con `PatchAsync`/`DeleteAsync` fueron generados correctamente. El modelo detectó que la `ApiClient` tenía código duplicado para crear clientes autorizados y lo refactorizó en un método privado.

**Frontend:** El interceptor de refresh en `api.ts` con manejo de cola de requests pendientes durante el refresh fue el componente más complejo. El modelo implementó el patrón correcto de `isRefreshing` + `pendingQueue` sin necesitar correcciones.

### Dónde el juicio humano fue necesario

**1. Decisión de arquitectura SignalR:** El modelo propuso inicialmente poner `SignalRSessionNotifier` en Infrastructure. Yo detecté la dependencia circular e indiqué el problema — el modelo resolvió correctamente ubicándolo en WebApi. La detección del problema fue mía; la solución, del modelo.

**2. Token rotation vs revocación:** El modelo propuso almacenar el hash del access token para validación extra. Rechacé esto — añade complejidad sin beneficio real dado que los access tokens duran solo 15 minutos.

**3. Scope de `ISessionNotifier`:** El modelo propuso `Singleton`. Corrección: debe ser `Scoped` porque depende de `IHubContext<T>` que tiene lifetime de request.

**4. Test de `IsActive_WhenExpired`:** El dominio previene crear sesiones con expiración pasada (invariante correcto). El modelo generó un test que violaba este invariante. Solución: usar reflection para simular la expiración en el test. Este caso evidencia que el modelo no siempre razona sobre las invariantes de dominio al generar tests.

**5. CORS para SignalR:** SignalR requiere `AllowCredentials()` además de los orígenes. El modelo lo omitió en la primera versión del `Program.cs`. Detectado al revisar la configuración.

### Velocidad real

La implementación completa (Domain → Infrastructure → Application → WebApi → BFF → Frontend → Tests → Docs) tomó aproximadamente 2 horas de trabajo AI-augmented. La estimación sin AI: 2-3 días de desarrollo. La diferencia: el modelo elimina el tiempo de búsqueda de documentación y el boilerplate, permitiendo enfocarse en las decisiones de diseño que realmente importan.

---

## Fase 8 — Outbox Pattern + User Commands

Esta fase añadió los tres comandos mutantes sobre `User` (crear, desactivar, cambiar rol) con publicación de eventos al bus garantizada por Outbox Pattern y deduplicación de reintentos vía Idempotency Key.

### Qué generó Claude Code

**Shared.Messages como proyecto nuevo:** El modelo extrajo los records `UserCreated`/`UserDeleted`/`UserRoleChanged` desde `Worker.Service/Messages` a un proyecto compartido sin acoplar a librerías externas. La actualización de los `using` en los consumers del Worker fue automática y correcta.

**Interfaces de Application:** `IEventPublisher` (genérica sobre `where TMessage : class`) e `IIdempotencyCache` con su record `IdempotencyEntry` quedaron en `Common/Interfaces`, replicando el patrón ya establecido para `IPasswordHasher`/`IJwtTokenGenerator`. El modelo no intentó meter dependencias de MassTransit en Application — respetó la separación de capas sin recordatorios.

**Handlers de los tres commands:** Estructura correcta y consistente (validator + handler + command record). El modelo aplicó la regla de "no permitir borrar al último Admin" cargando todos los usuarios y filtrando por rol activo. Es un check correcto aunque no óptimo en N grande; aceptado por simplicidad.

**MassTransit + Outbox en Infrastructure:** `AddEntityFrameworkOutbox<AppDbContext>` con `UsePostgres()` y `UseBusOutbox()`, más las tres llamadas `AddInboxStateEntity`/`AddOutboxStateEntity`/`AddOutboxMessageEntity` en `OnModelCreating`. El modelo replicó la lógica de transports del Worker (RabbitMQ vs Azure Service Bus por `MessageBus:Transport`) sin que tuviera que dictarse línea por línea.

**IdempotencyMiddleware:** Captura de la respuesta vía `MemoryStream` swap del `Response.Body`, restauración en `finally`, cacheo sólo para 2xx. El modelo identificó por su cuenta que cachear errores transitorios sería un anti-patrón.

**Tests con MassTransit TestHarness:** Para integración, sustituir `AddMassTransit` por `AddMassTransitTestHarness` requiere remover los descriptores ya registrados. El modelo generó el filtrado de descriptores por prefijo de namespace `MassTransit` correctamente al primer intento.

### Dónde el juicio humano fue necesario

**1. Versión de paquetes MassTransit:** El blueprint pedía "mismo major que el Worker". Verifiqué `Worker.Service.csproj` antes de continuar — el modelo necesita la verificación explícita o produce versiones desactualizadas si infiere desde su conocimiento general.

**2. Migración EF Core con runtime mismatch:** El entorno tiene .NET 10 pero los proyectos targetean `net9.0`. `dotnet ef` falla sin `DOTNET_ROLL_FORWARD=Major`. El modelo no anticipa este tipo de fricciones de entorno; las descubrí ejecutando y le pasé la solución.

**3. Idempotencia en pruebas:** El test `PostWithIdempotencyKey_CalledTwice_ShouldReturn201BothTimes` debe verificar que la segunda llamada devuelve la respuesta de la primera, no que reejecuta el handler. La forma de afirmar esto (`secondBody.Should().Be(firstBody)`) la propuse explícitamente — el modelo había generado primero un test que sólo verificaba el status code, lo cual no probaba la propiedad de idempotencia.

**4. `User.ChangeRole` semantics:** El modelo propuso `AddRole` (acumulativo) en lugar de reemplazar el rol. Corregí a la semántica del blueprint: limpiar y agregar, dado que el caso de uso supone un único rol activo. Esto evidencia que el modelo no infiere el contrato del dominio sin que se lo digan explícitamente.

**5. Outbox + InMemory en tests:** La primera versión del factory de integración no removía los registros de MassTransit antes de añadir el test harness, lo que dejaba dos buses configurados. Tuve que indicar el filtrado de descriptores. El modelo asumió que `AddMassTransitTestHarness` "tomaría precedencia", lo cual no es el comportamiento real.

### Velocidad real

Implementación completa (Shared.Messages + Application + Infrastructure + Migración + Middleware + Controllers + BFF + Tests + Docs) en aproximadamente 90 minutos AI-augmented. La parte más rápida fue el boilerplate de los tres handlers (validator + handler + DI); lo más lento fue depurar el entorno de tests con MassTransit y EF en memoria. Estimación manual: 1 día y medio.
