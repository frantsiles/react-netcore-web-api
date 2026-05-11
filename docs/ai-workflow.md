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
