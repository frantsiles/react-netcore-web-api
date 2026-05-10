# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Arquitectura general

```
React (5173) → YARP Gateway (5000) → BFF (5001) → Backend API (5002)
                                                         ↓
                                                   RabbitMQ (5672)
                                                         ↓
                                                  Worker Service
                                    Azure Functions (event-driven / timer)
```

- El frontend nunca llama directamente al Backend API; las peticiones pasan por BFF.
- El Vite dev server proxea `/bff/*` a `localhost:5001` (sin CORS ni URLs hardcodeadas).
- El Backend API emite JWTs; el BFF los valida con el mismo secreto compartido.
- El Backend API sigue DDD (Domain → Application → Infrastructure → WebApi) con MediatR + CQRS.
- El BFF tiene su propia estructura en capas idéntica al API.
- YARP Gateway actúa como punto de entrada único en Docker/Kubernetes.
- Worker Service consume eventos de RabbitMQ (local) o Azure Service Bus (Azure) vía MassTransit.
- Azure Functions expone endpoints HTTP, timers y triggers de Service Bus (isolated worker .NET 9).

## Observabilidad

**Stack**: OpenTelemetry SDK (.NET) → OTel Collector → Prometheus + Loki + Tempo → Grafana

- Todos los servicios .NET envían trazas, métricas y logs al OTel Collector via OTLP gRPC.
- Endpoint OTel en desarrollo local: `http://localhost:4317` (config: `Otel:Endpoint` en appsettings).
- Endpoint OTel en Docker/K8s: `http://otel-collector:4317` (via env var `OTEL_EXPORTER_OTLP_ENDPOINT`).
- Grafana UI en Docker: `http://localhost:3001` (admin/admin).
- Serilog formatea los logs como JSON estructurado y los envía al OTel Collector.

## Comandos de desarrollo

### Arrancar todos los servicios (modo legacy, sin Docker)

```bash
./start.sh          # lanza los 3 servicios originales en paralelo
./stop.sh           # mata procesos en puertos 5173, 5001 y 5002
```

### Servicios individuales

```bash
dotnet run --project src/Api/Api.WebApi           # puerto 5002
dotnet run --project src/BFF/BFF.Api              # puerto 5001
dotnet run --project src/Gateway/Gateway.Api      # puerto 5000
dotnet run --project src/Worker/Worker.Service    # worker (sin puerto HTTP)
cd frontend && npm run dev                         # puerto 5173
```

### Docker Compose

```bash
cp .env.example .env          # configurar secretos antes del primer arranque

docker compose build                                  # construir imágenes
docker compose up -d                                  # app + RabbitMQ
docker compose --profile observability up -d          # app + observabilidad completa
docker compose -f docker-compose.infra.yml up -d      # solo infra (dev local)
docker compose logs -f api                            # logs de un servicio
```

### Azure Functions (local)

```bash
cd src/Functions/Functions.App
func start                    # requiere Azure Functions Core Tools v4
```

## Tests

### .NET — unit e integración

```bash
dotnet test                                                     # todos
dotnet test src/Api/Api.UnitTests
dotnet test src/Api/Api.IntegrationTests
dotnet test src/BFF/BFF.Tests
dotnet test src/Api/Api.UnitTests --filter "FullyQualifiedName~NombreDelTest"
```

Frameworks: **xUnit + Moq + FluentAssertions**. Los de integración usan `WebApplicationFactory`.

### E2E — Playwright

```bash
cd frontend
npm run test:e2e              # arranca los 3 servicios automáticamente si no están corriendo
npm run test:e2e:report       # informe HTML del último run
```

Credenciales de prueba: `admin@demo.com / Admin123!` y `user@demo.com / User123!`.

## Kubernetes

```bash
# Local (minikube/kind) — imágenes cargadas localmente
minikube image load demo/api:latest && minikube image load demo/bff:latest  # etc.
kubectl apply -k k8s/overlays/local

# Azure AKS — deploy completo (Bicep + build + push + kubectl)
./infra/deploy.sh dev

# Ver estado
kubectl get all -n demo
kubectl get ingress -n demo
```

La estructura K8s usa **Kustomize**: `k8s/base/` con overlays en `k8s/overlays/local/` y `k8s/overlays/azure/`.

## Estructura del backend (.NET)

```
src/
  Api/
    Api.Domain/          # Entidades, Value Objects, interfaces de repositorio
    Api.Application/     # Comandos/queries MediatR, validadores FluentValidation
    Api.Infrastructure/  # EF Core InMemory, JWT, BCrypt, seeding
    Api.WebApi/          # Controladores, Program.cs, Swagger (puerto 5002)
    Api.UnitTests/
    Api.IntegrationTests/
  BFF/
    BFF.Domain/
    BFF.Application/     # Handlers MediatR
    BFF.Infrastructure/  # HttpClient hacia el Backend API, gestión de tokens
    BFF.Api/             # Controladores, Program.cs, Swagger (puerto 5001)
    BFF.Tests/
  Gateway/
    Gateway.Api/         # YARP reverse proxy (puerto 5000) — punto de entrada Docker/K8s
  Worker/
    Worker.Service/      # MassTransit: RabbitMQ (local) / Azure Service Bus (Azure)
                         # Consumers: UserCreated, UserDeleted, UserRoleChanged
  Functions/
    Functions.App/       # Azure Functions v4 isolated worker
                         # HTTP triggers, Timer (heartbeat), ServiceBus triggers
```

## Estructura del frontend (React)

```
frontend/src/
  contexts/AuthContext.tsx   # estado JWT + sessionStorage
  services/
    api.ts                   # instancia Axios + interceptor 401
    authService.ts
    userService.ts
  pages/                     # LoginPage, UsersPage, UnauthorizedPage
  components/                # componentes Radix UI + Tailwind v4
  types/                     # tipos TypeScript compartidos
  lib/                       # clsx, tailwind-merge, cva
```

Path alias `@` → `frontend/src/`. Tailwind v4 via plugin Vite (`@tailwindcss/vite`), sin `tailwind.config.*`.

## Infraestructura Azure (Bicep)

```
infra/
  bicep/
    main.bicep               # orquesta todos los módulos
    modules/
      aks.bicep              # AKS cluster + AcrPull role assignment
      acr.bicep              # Azure Container Registry (Basic SKU)
      servicebus.bicep       # Service Bus Standard + topic user-events + subscriptions
      keyvault.bicep         # Key Vault Standard + RBAC
  deploy.sh                  # script completo: Bicep + build imágenes + push ACR + kubectl apply
```

## Skill de infraestructura

Usa `/infra` para obtener ayuda contextual con Docker, Kubernetes y Azure para este proyecto.

## Mensajes de commit

Sigue las reglas definidas en
[.github/commit-message-instructions.md](.github/commit-message-instructions.md):
**Conventional Commits en español, imperativo, asunto ≤ 72 caracteres,
cuerpo que explique el por qué.**

Esa misma fuente la consume GitHub Copilot Chat
(vía `.vscode/settings.json`), por lo que cualquier ajuste debe hacerse
en ese único archivo para mantener coherencia entre ambas herramientas.
