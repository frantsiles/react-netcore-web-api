# react-netcore-web-api — Demo Cloud-Native

Repositorio de demostración que muestra cómo construir y operar una aplicación distribuida con **React**, **.NET 10**, **Docker**, **Kubernetes** y **Azure**. Pensado tanto para aprender como para servir de referencia arquitectónica.

> **¿Por qué existe este repo?**  
> Demuestra capacidades de desarrollo full-stack, diseño de microservicios, observabilidad, infraestructura como código y uso de IA (GitHub Copilot + Claude Code) en todo el ciclo de vida del software.

---

## Capacidades demostradas

| Área | Qué hay en este repo |
|------|---------------------|
| **Arquitectura distribuida** | Gateway → BFF → API → Worker. Cada servicio con responsabilidad única y contratos explícitos. |
| **DDD + CQRS + MediatR** | Backend API con cuatro layers, handlers testeables, validación con FluentValidation. |
| **Autenticación JWT + Sessions** | Access tokens (15 min) + refresh tokens stateful con rotación. Revocación por sesión en tiempo real. |
| **Persistencia containerizada** | PostgreSQL 17 en contenedor, EF Core con migraciones y data seeding. Fallback a InMemory si no hay connection string. |
| **Mensajería asíncrona** | MassTransit sobre RabbitMQ (local) o Azure Service Bus (Azure) con el mismo código. |
| **Outbox Pattern + Idempotency** | Eventos publicados transaccionalmente vía `MassTransit.EntityFrameworkCore` Outbox sobre Postgres; deduplicación de reintentos con header `X-Idempotency-Key`. |
| **Serverless** | Azure Functions v4 isolated con HTTP triggers, timer y Service Bus triggers. |
| **Observabilidad** | OTel SDK + Collector → Prometheus + Loki + Tempo → Grafana. Correlación log-traza automática. |
| **Contenedores** | Multi-stage Dockerfiles, Docker Compose con perfiles (app / observabilidad / infra). |
| **Kubernetes** | Kustomize con base + overlays local/azure. Liveness/readiness probes, resource limits. |
| **IaC Azure** | Bicep: AKS + ACR + Service Bus + Key Vault. Script de deploy end-to-end. |
| **Testing** | Unit (xUnit + Moq + FluentAssertions), integración (WebApplicationFactory), E2E (Playwright). |
| **SignalR en tiempo real** | SessionHub con grupos por usuario y admin. Compatible con Azure SignalR Service (drop-in). |
| **AI Agent + SSE** | Agente conversacional con Semantic Kernel 1.76.0, plugins sobre ISender y streaming via Server-Sent Events. Soporta Ollama, OpenAI y Azure OpenAI. |
| **Ingeniería AI-augmented** | Claude Code + GitHub Copilot integrados en todo el ciclo. [Ver cómo →](docs/ai-workflow.md) |
| **CI/CD** | GitHub Actions: tests .NET, build frontend, Docker builds en paralelo, validación de manifiestos K8s. |

---

## Documentación adicional

| Documento | Descripción |
|-----------|-------------|
| [docs/ai-workflow.md](docs/ai-workflow.md) | Cómo se usó la IA en este proyecto: qué funcionó, qué no, y dónde el juicio humano fue irreemplazable |
| [docs/adr/ADR-001](docs/adr/ADR-001-bff-pattern.md) | Patrón BFF — por qué el frontend nunca habla directamente con el API |
| [docs/adr/ADR-002](docs/adr/ADR-002-ddd-cqrs-mediatr.md) | DDD + CQRS + MediatR en el Backend API |
| [docs/adr/ADR-003](docs/adr/ADR-003-masstransit-transport-abstraction.md) | MassTransit como abstracción del message bus |
| [docs/adr/ADR-004](docs/adr/ADR-004-ef-core-inmemory.md) | EF Core InMemory — cero dependencias para una demo |
| [docs/adr/ADR-005](docs/adr/ADR-005-yarp-gateway.md) | YARP como API Gateway .NET-nativo |
| [docs/adr/ADR-006](docs/adr/ADR-006-opentelemetry-observability.md) | OpenTelemetry como estándar de observabilidad |
| [docs/adr/ADR-007](docs/adr/ADR-007-kustomize-over-helm.md) | Kustomize en lugar de Helm |
| [docs/adr/ADR-008](docs/adr/ADR-008-stateful-refresh-tokens-signalr.md) | Refresh tokens stateful + revocación en tiempo real con SignalR |
| [docs/adr/ADR-010](docs/adr/ADR-010-semantic-kernel-agent-sse.md) | Agente IA con Semantic Kernel + SSE: plugins sobre ISender, IKernelFactory multi-provider, BFF pass-through |
| [docs/adr/ADR-009](docs/adr/ADR-009-outbox-pattern-idempotency.md) | Outbox Pattern de MassTransit + Idempotency Key como middleware |

---

## Índice

1. [Arquitectura](#1-arquitectura)
2. [Stack tecnológico](#2-stack-tecnológico)
3. [Inicio rápido — elige tu camino](#3-inicio-rápido--elige-tu-camino)
4. [Modo A · Sin Docker (local)](#4-modo-a--sin-docker-local)
5. [Modo B · Docker Compose](#5-modo-b--docker-compose)
6. [Modo C · GitHub Codespaces](#6-modo-c--github-codespaces)
7. [Componentes — qué resuelve cada capa y cuándo usarla](#7-componentes--qué-resuelve-cada-capa-y-cuándo-usarla)
8. [Observabilidad — Grafana Stack](#8-observabilidad--grafana-stack)
9. [Kubernetes — local y Azure AKS](#9-kubernetes--local-y-azure-aks)
10. [Despliegue en Azure](#10-despliegue-en-azure)
11. [Tests](#11-tests)
12. [Estructura del proyecto](#12-estructura-del-proyecto)
13. [Referencia de configuración](#13-referencia-de-configuración)
14. [Credenciales de demo](#14-credenciales-de-demo)
15. [Decisiones de diseño](#15-decisiones-de-diseño)
16. [Convenciones del repositorio](#16-convenciones-del-repositorio)

> Documentación extendida en [`docs/`](docs/) — ADRs y workflow AI-augmented.

---

## 1. Arquitectura

### Vista general

```
┌─────────────────────────────────────────────────────────────────────┐
│                          CLIENTE (browser)                          │
└─────────────────────────────┬───────────────────────────────────────┘
                              │ HTTP (dev: Vite proxy | prod: nginx)
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│              React SPA  ·  Vite + TypeScript + Tailwind v4          │
│              localhost:5173  (Docker/K8s: nginx :80)                │
└─────────────────────────────┬───────────────────────────────────────┘
                              │ /bff/*
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│          YARP API Gateway  ·  .NET 10                               │
│          localhost:5000  (Docker/K8s: :8080)                        │
│          Routing · Rate limiting · Health check activo de upstream  │
└──────────────────┬────────────────────────────┬───────────────────────┘
                   │ /bff/*                   │ /api/*
                   ▼                          ▼
┌──────────────────────────┐  ┌────────────────────────────────────────┐
│  BFF  ·  .NET 10         │  │  Backend API  ·  .NET 10             │
│  localhost:5001          │  │  localhost:5002                       │
│  Valida JWT              │  │  DDD + MediatR + CQRS                │
│  Proxea al API           │  │  FluentValidation                    │
└──────────────────────────┘  │  EF Core + Npgsql                    │
                              │  Emite JWT · Swagger                 │
                              └──────┬────────────────┬──────────────┘
                                     │                │ publica
                                     ▼                ▼
                         ┌──────────────────┐  ┌────────────────────┐
                         │  PostgreSQL 17   │  │  RabbitMQ (local)  │
                         │  :5432           │  │  Service Bus(Azure)│
                         │  Migraciones EF  │  └──────────┬─────────┘
                         │  Seed automático │             │ consume
                         └──────────────────┘  ┌──────────▼─────────┐
                                               │ Worker Service     │
                                               │ MassTransit        │
                                               │ Background consumer│
                                               └────────────────────┘

     Azure Functions  (standalone, event-driven)
     ├── HTTP trigger   — GET /functions/users
     ├── Timer trigger  — heartbeat cada 5 min
     └── ServiceBus     — procesa user-events
```

### Flujo de autenticación

```
Browser ──POST /bff/auth/login──▶ BFF ──▶ API (emite AccessToken + RefreshToken)
Browser ◀── AccessToken (15min) + RefreshToken (30d) ── BFF ◀────────────────────
Browser ──GET /bff/users (Bearer)─▶ BFF valida JWT ──▶ API ──▶ DB
                                                   │
        AccessToken expira                         ▼
Browser ──POST /bff/auth/refresh (RefreshToken)──▶ BFF ──▶ API (Token Rotation)
Browser ◀───────── nuevo AccessToken + RefreshToken ──────────────────────────────

Browser ──WS ws://api:5002/hubs/sessions──▶ SessionHub (directo al API, no vía BFF)
         notificación en tiempo real cuando se revoca una sesión
```

### Flujo de observabilidad

```
.NET services
(Serilog JSON + OTel SDK)
        │ OTLP gRPC :4317
        ▼
OTel Collector
        ├── métricas ──▶ Prometheus :9090
        ├── logs ───────▶ Loki :3100
        └── trazas ─────▶ Tempo :3200
                                │
                         Grafana :3001  ◀── dashboard unificado
```

---

## 2. Stack tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| Frontend | React + Vite + TypeScript | 19 / 8 / 6 |
| Estilos | Tailwind CSS (via Vite plugin) | 4 |
| UI primitivos | Radix UI + lucide-react | - |
| Estado servidor | TanStack Query | 5 |
| HTTP client | Axios | 1.15 |
| WebSockets | @microsoft/signalr | 10 |
| Backend runtime | .NET / ASP.NET Core | 10.0 |
| Patrón | DDD + MediatR + CQRS | - |
| Validación | FluentValidation | 12 |
| Base de datos | PostgreSQL (contenedor) | 17-alpine |
| ORM | EF Core + Npgsql provider | 10.0 / 10.0 |
| Autenticación | JWT Bearer | - |
| API Gateway | YARP ReverseProxy | 2.2 |
| Message bus | MassTransit + RabbitMQ / Azure Service Bus | 8.3 |
| Functions | Azure Functions v4 (isolated) | .NET 8 |
| Trazas / Métricas | OpenTelemetry SDK | 1.9 |
| Logs estructurados | Serilog (JSON + OTLP sink) | 8 |
| Colector OTel | OpenTelemetry Collector Contrib | 0.115 |
| Métricas | Prometheus | 2.55 |
| Logs | Grafana Loki | 3.2 |
| Trazas | Grafana Tempo | 2.6 |
| Dashboard | Grafana | 11.3 |
| Contenedores | Docker + Docker Compose | - |
| Orquestación | Kubernetes + Kustomize | - |
| IaC Azure | Bicep | - |
| Tests .NET | xUnit + Moq + FluentAssertions | - |
| Tests E2E | Playwright | 1.59 |
| AI Agent | Semantic Kernel | 1.76.0 |
| LLM local | Ollama (default, sin coste) | - |

---

## 3. Inicio rápido — elige tu camino

```
¿Tienes Docker?
├── SÍ ──▶ Modo B (Docker Compose) — sección 5      ← recomendado
│          ./start.sh levanta todo: 5 servicios + Postgres + RabbitMQ.
│
└── NO ──▶ ¿Usas GitHub Codespaces?
           ├── SÍ ──▶ Modo C (Codespaces) — sección 6
           │          Funciona en el navegador, sin instalar nada.
           │
           └── NO ──▶ Modo A (local sin Docker) — sección 4
                      `dotnet run` por servicio. Requiere .NET 10 SDK + Node 20.
```

**Camino corto si ya tienes Docker:**

```bash
git clone https://github.com/<tu-usuario>/react-netcore-web-api.git
cd react-netcore-web-api
./start.sh                # build (primera vez) + docker compose up -d --wait
# → http://localhost:5173
./stop.sh                 # baja todo
```

---

## 4. Modo A · Sin Docker (local)

Útil para iterar rápido en un único servicio con hot-reload y atacar el código con el debugger del IDE. **No recomendado como flujo principal**: el stack containerizado es más representativo de cómo corre la app en producción.

### Requisitos

| Herramienta | Versión mínima | Verificar |
|------------|---------------|-----------|
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | 20.x | `node --version` |
| npm | 10.x | `npm --version` |

### Primer uso

```bash
# 1. Clonar
git clone https://github.com/<tu-usuario>/react-netcore-web-api.git
cd react-netcore-web-api

# 2. Restaurar dependencias .NET
dotnet restore

# 3. Instalar dependencias de frontend
cd frontend && npm install && cd ..
npx playwright install --with-deps chromium   # solo si vas a correr tests E2E
```

### Levantar la infraestructura (Postgres + RabbitMQ + observabilidad)

Para `dotnet run` necesitas Postgres en `localhost:5433` (lo que asume `appsettings.Development.json`):

```bash
docker compose -f docker-compose.infra.yml up -d
```

Levanta solo la infraestructura sin las imágenes de la app — los servicios .NET los corres con `dotnet run` y se conectan a Postgres y RabbitMQ del compose de infra.

### Arrancar servicios individualmente

```bash
# Terminal 1 — Backend API (puerto 5002)
dotnet run --project src/Api/Api.WebApi

# Terminal 2 — BFF (puerto 5001)
dotnet run --project src/BFF/BFF.Api

# Terminal 3 — Frontend (puerto 5173)
cd frontend && npm run dev

# Terminal 4 (opcional) — YARP Gateway (puerto 5000)
dotnet run --project src/Gateway/Gateway.Api

# Terminal 5 (opcional) — Worker Service
dotnet run --project src/Worker/Worker.Service
```

| Servicio | URL | Descripción |
|----------|-----|-------------|
| Frontend React | http://localhost:5173 | App principal |
| Backend API Swagger | http://localhost:5002/swagger | Explora los endpoints del API |
| BFF Swagger | http://localhost:5001/swagger | Explora los endpoints del BFF |
| API health | http://localhost:5002/api/health | Liveness check |
| BFF health | http://localhost:5001/bff/health | Liveness check |

> **Nota:** el Gateway y el Worker **no son obligatorios** para iterar en local. El Gateway es el punto de entrada en Docker/K8s; el Worker necesita RabbitMQ corriendo.

---

## 5. Modo B · Docker Compose

Flujo principal recomendado. Toda la arquitectura corre en contenedores, igual que en Kubernetes — solo cambia la orquestación.

### Requisitos

- Docker Desktop (o Docker Engine + Compose plugin)
- Verificar: `docker --version` y `docker compose version`

### Arranque rápido — `start.sh`

```bash
./start.sh              # app + Postgres + RabbitMQ
./start.sh --obs        # + stack de observabilidad (Grafana/Loki/Tempo/Prometheus)
./start.sh --build      # fuerza rebuild de imágenes antes de levantar
./stop.sh               # docker compose down (preserva volúmenes)
./stop.sh --clean       # docker compose down -v (borra datos de Postgres/RabbitMQ)
```

`start.sh` se encarga de:

1. Crear `.env` a partir de `.env.example` si no existe.
2. Construir las imágenes la primera vez (o con `--build`).
3. Lanzar `docker compose up -d --wait` para que regrese sólo cuando todos los healthchecks pasen.
4. Imprimir las URLs disponibles.

### Servicios que levanta por defecto

| Servicio | URL / Puerto | Notas |
|----------|--------------|-------|
| Frontend (nginx) | http://localhost:5173 | SPA compilado, proxy a BFF |
| YARP Gateway | http://localhost:5000 | Punto de entrada único |
| BFF | http://localhost:5001 | Backend For Frontend |
| Backend API | http://localhost:5002/swagger | Emite JWT, expone Swagger |
| **PostgreSQL 17** | localhost:5432 | `demo` / `demo123`, BD `demodb` |
| RabbitMQ | localhost:5672 / UI:15672 | `admin` / `admin123` |
| Worker | (sin puerto) | Consume cola `user-events` |

### Con `--obs` se añaden

| Herramienta | URL | Descripción |
|-------------|-----|-------------|
| Grafana | http://localhost:3001 | Dashboard unificado (admin / admin) |
| Prometheus | http://localhost:9090 | Métricas raw |
| Loki | http://localhost:3100 | Logs agregados |
| Tempo | http://localhost:3200 | Trazas distribuidas |
| OTel Collector | localhost:4317 (gRPC) | Receptor OTLP |

### Variables de entorno (`.env`)

```env
JWT_SECRET=CHANGE-THIS-SECRET-IN-PRODUCTION-MIN32CHARS!!
POSTGRES_DB=demodb
POSTGRES_USER=demo
POSTGRES_PASSWORD=demo123
RABBITMQ_USER=admin
RABBITMQ_PASS=admin123
GRAFANA_ADMIN_PASS=admin
```

### Solo infraestructura — para iterar con `dotnet run`

Levanta Postgres + RabbitMQ + observabilidad sin las imágenes de la app:

```bash
docker compose -f docker-compose.infra.yml up -d
```

Postgres queda expuesto en **5433** (no 5432) para no chocar con otra instancia en el host, lo que coincide con la connection string de `appsettings.Development.json`.

### Comandos útiles de Docker Compose

```bash
docker compose logs -f api              # logs en tiempo real de un servicio
docker compose up -d --build api        # rebuild + restart de un servicio
docker compose ps                       # estado de los contenedores
docker compose down                     # parar todo (preserva volúmenes)
docker compose down -v                  # parar y borrar datos
```

### Cómo funcionan los Dockerfiles

Cada servicio .NET tiene un **multi-stage Dockerfile** en `docker/<servicio>/Dockerfile`:

1. **Stage builder** — `mcr.microsoft.com/dotnet/sdk:10.0`, copia los `.csproj` primero (caching de capas) y publica el binario.
2. **Stage final** — `mcr.microsoft.com/dotnet/aspnet:10.0` (sin SDK), instala `curl` para los healthchecks y arranca el binario como `ENTRYPOINT`.

El frontend usa `node:20-alpine` para compilar el SPA y `nginx:alpine` para servirlo. El nginx incluye un proxy `/bff/*` → BFF para que el SPA no haga CORS.

---

## 6. Modo C · GitHub Codespaces

No necesitas instalar nada en tu máquina.

1. Abre el repositorio en GitHub.
2. Haz clic en **Code → Codespaces → Create codespace on main**.
3. Espera ~2 minutos mientras el contenedor construye e instala las dependencias automáticamente.
4. En la terminal integrada, ejecuta:
   ```bash
   ./start.sh
   ```
5. El navegador abrirá automáticamente el puerto 5173 (o puedes ir a la pestaña **Ports** en VS Code).

> **¿Por qué funciona sin cambiar URLs?**  
> El frontend usa rutas relativas (`/bff/...`). En local, Vite las proxea a `localhost:5001`. En Codespaces, el mismo proxy redirige a la URL interna del Codespace. Nunca hay URLs hardcodeadas.

---

## 7. Componentes — qué resuelve cada capa y cuándo usarla

Esta sección explica el **por qué** detrás de cada elemento de la arquitectura: qué problema concreto resuelve, en qué escenarios es valioso, y cuándo es excesivo. La idea es que puedas decidir, en tu próximo proyecto, cuáles incorporar y cuáles omitir.

---

### Frontend SPA (`frontend/`)

**Qué es.** Aplicación React 19 + Vite + TypeScript que en desarrollo corre con el dev server de Vite (HMR) y en Docker/K8s se sirve compilada con `nginx:alpine`.

**Qué resuelve.** Separa el ciclo de vida del cliente del de la API: el frontend puede desplegarse independientemente, escalar horizontalmente como contenido estático (nginx) y cachearse en CDN.

**Cuándo lo quieres.** Cualquier app web con cierta interactividad, dashboards con estado client-side, o cuando vas a tener apps móviles consumiendo el mismo backend (el contrato HTTP ya está).

**Cuándo es excesivo.** Para sitios mayoritariamente de lectura o con interacción mínima, MVC server-rendered es más simple, indexable en SEO de forma trivial y no requiere mantener una pipeline de bundling.

---

### YARP API Gateway (`src/Gateway/Gateway.Api`)

**Qué es.** Reverse proxy en .NET 10 con [YARP](https://microsoft.github.io/reverse-proxy/). En Docker/K8s es el único servicio expuesto al exterior.

**Qué resuelve.**
- **Punto de entrada único.** El cliente solo conoce una URL aunque por detrás haya N microservicios.
- **Concerns transversales.** Rate limiting, autenticación de borde, transformación de headers, circuit breaker — todo aplicado una vez, no por servicio.
- **Health checks activos.** Detecta upstreams caídos antes de mandarles tráfico.
- **Routing por path.** `/bff/*` → BFF, `/api/*` → API directamente para casos donde no se necesita el BFF (Swagger admin, healthchecks).

**Cuándo lo quieres.**
- Cuando tienes ≥2 servicios públicos y quieres una capa para políticas comunes.
- Cuando despliegas en Kubernetes y quieres un Ingress *del lado de la app* (no del Ingress Controller) para lógica que el Ingress no expresa bien (auth con JWT, header rewriting complejo).
- Cuando vas a partir un monolito y necesitas ir migrando paths a nuevos servicios sin que el cliente se entere.

**Cuándo es excesivo.**
- Un único servicio HTTP: nginx o el propio Kestrel detrás del Ingress es suficiente.
- API consumida solo por scripts internos: añade un hop sin valor.
- Alternativas: si ya pagas por un API Management gestionado (Azure APIM, AWS API Gateway, Kong), suelen cubrir el caso con menos código.

---

### BFF — Backend For Frontend (`src/BFF/`)

**Qué es.** Servicio .NET intermedio entre el SPA y la API. Tiene la misma estructura en capas que el API (Domain/Application/Infrastructure/Api) pero su `Infrastructure` no habla con BD: habla con la API por HTTP.

**Qué resuelve.**
- **Agregación de respuestas.** Si una pantalla necesita combinar 3 endpoints, el BFF los une y manda un solo payload al cliente — menos round-trips, menos código en el cliente.
- **Adaptación de contrato.** El API expone un modelo limpio del dominio; el BFF lo adapta a lo que el SPA necesita pintar.
- **Aislamiento del API.** El equipo de frontend puede cambiar el BFF sin tocar el API. El API puede evolucionar sin romper el frontend.
- **Auth segura.** El BFF valida el JWT y puede mantener sesiones server-side (cookies HttpOnly) si quieres evitar tokens en el navegador.

**Cuándo lo quieres.**
- Tienes **varios clientes** (web, móvil, ext. de Chrome) con necesidades distintas — un BFF por cliente.
- El API es de uso público o pertenece a otro equipo, y necesitas una capa que evolucione al ritmo del front.
- Quieres mover lógica de presentación fuera del cliente (cálculos, formateo, joins).

**Cuándo es excesivo.**
- Un solo cliente con necesidades que coinciden 1:1 con el API: el BFF es un proxy tonto que añade latencia y un servicio más que mantener.
- Pequeñas apps internas: usa el API directamente.

---

### Backend API (`src/Api/`)

**Qué es.** Servicio .NET con **DDD + CQRS + MediatR** que es la fuente de verdad del dominio. Emite JWTs, aplica migraciones a Postgres, valida con FluentValidation, expone Swagger.

**Qué resuelve.**
- **Encapsular las reglas de negocio.** Si una regla cambia, cambia en un solo lugar.
- **Testabilidad.** Los handlers de MediatR son testeables en unit tests sin levantar el host.
- **Reuso.** Un mismo dominio puede ser consumido por BFF web, BFF móvil, integraciones B2B, etc.

**Cuándo es DDD/CQRS valioso.** Dominios con reglas no triviales (cálculo de tarifas, autorización por contexto, máquinas de estado). Cuando varias personas escriben en el mismo módulo y necesitan navegar con seguridad.

**Cuándo es excesivo.** CRUDs simples sobre 2-3 tablas. Una EFCore + Minimal APIs es más corto y se lee igual de bien.

---

### PostgreSQL 17 (`postgres` en `docker-compose.yml`)

**Qué es.** Base de datos relacional ACID en contenedor. El API se conecta vía Npgsql (provider EF Core para Postgres) y aplica migraciones al arrancar (`DatabaseInitializer.InitializeAsync`).

**Qué resuelve.**
- **Persistencia real.** Los datos sobreviven al reinicio del proceso de la app.
- **Transacciones.** Garantías ACID que un message bus o un caché distribuido no te dan.
- **Consultas potentes.** SQL con joins, agregaciones, índices, full-text, JSON nativo.
- **Demo de migración EF → Postgres.** Misma capa de datos que en producción, pero corriendo en tu máquina con un único `docker compose up`.

**Por qué Postgres y no SQL Server / MySQL.** Para el demo: licencia permisiva, imagen Alpine pequeña (~80MB), soporte de tipos JSON/array sin extensiones, ampliamente usado en cloud-native.

**Detalle interesante de este repo.** El API tiene **fallback automático a EF Core InMemory** si `ConnectionStrings:DefaultConnection` está vacío (ver [DependencyInjection.cs:23](src/Api/Api.Infrastructure/DependencyInjection.cs#L23)). Esto permite que los tests de integración corran sin Postgres y que la app arranque "a secas" para una demo rápida — pero al levantar con `./start.sh` la connection string se inyecta y se usa Postgres real.

**Cuándo lo quieres.** Casi siempre. La excepción son cargas write-heavy a gran escala donde un store especializado (Cassandra, DynamoDB) saca ventaja, o dominios fundamentalmente document-oriented donde MongoDB simplifica el modelado.

**Cuándo es excesivo.** Para un cache, una cola de tareas o blobs binarios — Redis, RabbitMQ o un object storage son herramientas mejores. No conviertas Postgres en un cajón de sastre.

---

### RabbitMQ — message bus (`rabbitmq` en `docker-compose.yml`)

**Qué es.** Broker AMQP 0-9-1 con UI de gestión en `:15672`. En este proyecto es el transporte que usa MassTransit para mover eventos entre API y Worker.

**Qué resuelve.**
- **Desacoplamiento temporal.** El API publica `UserCreated` y sigue. El Worker lo procesa cuando puede. Si el Worker está caído, los mensajes esperan en la cola.
- **Desacoplamiento de despliegue.** API y Worker pueden desplegarse y escalarse de forma independiente.
- **Retry y dead-lettering.** Si un consumer falla, RabbitMQ puede reintentar o mover el mensaje a una DLQ.
- **Fan-out.** Un mismo evento puede tener varios consumers (Worker para enviar email, otro para auditoría, otro para indexación).

**Cuándo lo quieres.**
- Trabajo de fondo que no debe bloquear la respuesta HTTP (envío de email, generación de PDF, cálculo costoso).
- Comunicación entre servicios donde la **eventual consistency** es aceptable.
- Workflows largos con pasos que pueden fallar y reintentar de forma independiente.

**Cuándo es excesivo.**
- Comunicación punto a punto que necesita respuesta inmediata: una llamada HTTP es más simple.
- Procesos cortos y locales: usar `Channel<T>` o `BackgroundService` con cola en memoria evita la dependencia.

**¿RabbitMQ o Azure Service Bus?** En este repo cambias entre ambos con `MessageBus:Transport` en `appsettings.json` — el código de los consumers no cambia gracias a la abstracción de MassTransit. RabbitMQ es ideal local/on-prem; Service Bus es la opción gestionada en Azure (sin op cost).

---

### Worker Service (`src/Worker/Worker.Service`)

**Qué es.** Proceso `BackgroundService` de .NET 10 que consume mensajes con [MassTransit](https://masstransit.io/). No tiene puerto HTTP — vive de la cola.

**Qué resuelve.** Es el extremo "consumidor" del patrón pub/sub. Procesa eventos publicados por el API sin que el flujo HTTP sea bloqueante. Permite **escalar el consumo** (más réplicas = más throughput) independiente del API.

**Consumers implementados:**
| Consumer | Mensaje | Qué hace |
|----------|---------|----------|
| `UserCreatedConsumer` | `UserCreated` | Procesa alta de usuario (en demo: log) |
| `UserDeletedConsumer` | `UserDeleted` | Limpieza post-baja |
| `UserRoleChangedConsumer` | `UserRoleChanged` | Auditoría de cambio de roles |

**Cuándo lo quieres.**
- Cuando tienes trabajo asíncrono que justifica un proceso dedicado (no un `Task.Run` perdido en el API).
- Cuando quieres escalar el procesamiento de eventos sin escalar la API.
- Cuando el trabajo es lo suficientemente largo o intensivo que metido en el request HTTP daría timeouts.

**Cuándo es excesivo.** Para tareas que tardan milisegundos: hazlas inline o con `IHostedService` dentro del propio API.

---

### Azure Functions (`src/Functions/Functions.App`)

**Qué es.** Function App .NET 8 **isolated worker** con HTTP, Timer y Service Bus triggers. Pensada para ejecutarse en el plan de consumo de Azure (paga por ejecución).

**Triggers implementados:**

| Función | Trigger | Ruta / Schedule | Descripción |
|---------|---------|-----------------|-------------|
| `GetUsers` | HTTP GET | `/api/functions/users` | Devuelve usuarios (demo) |
| `GetUserById` | HTTP GET | `/api/functions/users/{id}` | Devuelve usuario por ID |
| `Heartbeat` | Timer | `0 */5 * * * *` | Cada 5 min, registra latido |
| `ProcessUserCreated` | Service Bus | topic `user-events` | Procesa evento UserCreated |
| `ProcessUserDeleted` | Service Bus | topic `user-events` | Procesa evento UserDeleted |

**Worker vs Functions — ¿cuál uso?**

| | Worker Service | Azure Functions |
|--|----------------|-----------------|
| Despliegue | Pod en Kubernetes, contenedor 24/7 | Function App serverless |
| Escalado | Manual (réplicas) o KEDA | Automático según cola/HTTP |
| Coste | Capacidad reservada | Solo cuando se ejecuta |
| Estado en memoria | Posible (instancia long-running) | Stateless, frío al iniciar |
| Ideal para | Procesamiento continuo, alto throughput | Picos, tareas raras, integraciones |

**Cuándo lo quieres.** Tareas bursty (un import semanal), integraciones reactivas (procesar un evento de Service Bus que ocurre 100 veces al día), o webhooks que reciben tráfico irregular.

**Cuándo es excesivo.** Trabajo continuo: el cold start y el coste por ejecución penalizan. Para 1000 mensajes/segundo constantes el Worker en K8s sale más barato y predecible.

**Ejecutar local:**
```bash
npm install -g azure-functions-core-tools@4
cd src/Functions/Functions.App && func start
```

---

### Red Docker — `demo-network` (bridge user-defined)

**Qué es.** En `docker-compose.yml` se declara una red bridge llamada `demo-network` y **todos los servicios la comparten**. Docker crea un DNS interno donde el nombre del servicio resuelve a la IP del contenedor.

**Por qué importa.**
- **Service discovery por nombre.** El BFF llama al API como `http://api:8080/`, no como `http://172.x.y.z`. Si el contenedor se recrea con otra IP, el nombre sigue resolviéndolo.
- **Aislamiento.** Lo que está en `demo-network` no es alcanzable desde otras redes Docker sin attach explícito.
- **Solo lo expuesto sale al host.** Postgres y RabbitMQ están en `demo-network` y además hacen `ports: 5432:5432` y `5672:5672` — pero podrías quitar ese mapeo en producción para que solo otros contenedores los vean.
- **Mapeo equivalente en Kubernetes.** En K8s cada servicio tiene un `Service` con DNS interno (`api.demo.svc.cluster.local`); la migración del modelo mental Compose → K8s es 1:1.

**Hostnames internos en este repo:**
| Origen → destino | URL usada |
|------------------|-----------|
| BFF → API | `http://api:8080/` |
| Gateway → BFF | `http://bff:8080/` |
| Gateway → API | `http://api:8080/` |
| API → Postgres | `Host=postgres;Port=5432` |
| API → RabbitMQ | `rabbitmq:5672` |
| Worker → RabbitMQ | `rabbitmq:5672` |
| Frontend (nginx) → BFF | `http://bff:8080/bff/` |
| Servicios .NET → OTel Collector | `http://otel-collector:4317` |

**Cuándo lo quieres.** Siempre que tengas más de un contenedor que se hablen entre sí. La red bridge user-defined es el default sano — la red `bridge` por defecto (sin nombre) **no** tiene DNS automático entre servicios.

**Cuándo limitar la red.** En producción, si un servicio no necesita hablar con otro, sepáralo en su propia red. En este demo todo está en una única red por simplicidad.

---

## 8. Observabilidad — Grafana Stack

La observabilidad está integrada en todos los servicios .NET mediante **OpenTelemetry SDK** y **Serilog**.

### Cómo arranca

```bash
docker compose --profile observability up -d
```

### Acceder a Grafana

1. Abre http://localhost:3001
2. Usuario: `admin` / Contraseña: `admin` (configurable en `.env`)
3. En el menú lateral: **Explore** para buscar logs, métricas o trazas.

### Los tres pilares

#### Métricas (Prometheus)

Las métricas de cada servicio .NET se envían al OTel Collector, que las expone para Prometheus.

En Grafana → Explore → selecciona **Prometheus**:
```promql
# Peticiones HTTP por servicio
http_server_request_duration_seconds_count{job="demo-api"}

# Uso de memoria
process_runtime_dotnet_gc_heap_size_bytes

# Errores
http_server_request_duration_seconds_count{http_response_status_code="500"}
```

#### Logs (Loki)

Los logs estructurados en JSON van del OTel Sink de Serilog → OTel Collector → Loki.

En Grafana → Explore → selecciona **Loki**:
```logql
# Todos los logs del API
{job="demo-api"}

# Solo errores
{job="demo-api"} | json | level = "Error"

# Logs con TraceID para correlación
{job="demo-api"} | json | traceId = "<id>"
```

#### Trazas (Tempo)

Las trazas distribuidas muestran el recorrido completo de una petición: Gateway → BFF → API.

En Grafana → Explore → selecciona **Tempo** → busca por TraceID o usa el Service Graph.

### Correlación log ↔ traza

Grafana está configurado para que al ver un log puedas saltar directamente a la traza correspondiente y viceversa. Esto funciona gracias al campo `traceId` que Serilog incluye automáticamente en cada log gracias al enricher de OpenTelemetry.

### Cómo está integrado en el código

En cada `Program.cs` de los servicios .NET:

```csharp
// Serilog — JSON en consola + envío a OTel Collector
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.OpenTelemetry(opt => {
        opt.Endpoint = "http://localhost:4317";
        opt.Protocol = OtlpProtocol.Grpc;
    }));

// OpenTelemetry — trazas HTTP + métricas de runtime
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddAspNetCoreInstrumentation().AddOtlpExporter())
    .WithMetrics(b => b.AddRuntimeInstrumentation().AddOtlpExporter());
```

En **local sin Docker**, el OTel Collector no está corriendo, así que los logs irán solo a consola (sin errores — Serilog falla silenciosamente si el endpoint no está disponible).

---

## 9. Kubernetes — local y Azure AKS

Los manifiestos están organizados con [Kustomize](https://kustomize.io/): una base común y overlays por entorno.

```
k8s/
├── base/              ← manifiestos base (Deployments, Services, Secrets, Ingress)
└── overlays/
    ├── local/         ← minikube/kind: imagePullPolicy Never
    └── azure/         ← AKS: imágenes de Azure Container Registry
```

### Local — minikube

**Requisitos:** `minikube` y `kubectl`.

```bash
# 1. Arrancar minikube
minikube start

# 2. Construir las imágenes localmente
docker build -t demo/api:latest -f docker/api/Dockerfile .
docker build -t demo/bff:latest -f docker/bff/Dockerfile .
docker build -t demo/gateway:latest -f docker/gateway/Dockerfile .
docker build -t demo/worker:latest -f docker/worker/Dockerfile .
docker build -t demo/frontend:latest -f docker/frontend/Dockerfile .

# 3. Cargar imágenes en minikube (no las descarga de internet)
minikube image load demo/api:latest
minikube image load demo/bff:latest
minikube image load demo/gateway:latest
minikube image load demo/worker:latest
minikube image load demo/frontend:latest

# 4. Aplicar manifiestos
kubectl apply -k k8s/overlays/local

# 5. Ver que todo está corriendo
kubectl get all -n demo

# 6. Acceder a la app (en otra terminal)
minikube tunnel   # expone el LoadBalancer
# o port-forward:
kubectl port-forward -n demo svc/frontend 8080:80
```

### Local — kind (Kubernetes in Docker)

```bash
kind create cluster --name demo

# Cargar imágenes
kind load docker-image demo/api:latest --name demo
kind load docker-image demo/bff:latest --name demo
# ... igual para los demás

# Aplicar manifiestos
kubectl apply -k k8s/overlays/local
```

### Comandos Kubernetes útiles

```bash
# Ver pods y su estado
kubectl get pods -n demo

# Ver logs de un pod
kubectl logs -n demo deployment/api -f

# Entrar a un contenedor (debug)
kubectl exec -it -n demo deployment/api -- /bin/sh

# Ver eventos recientes (útil cuando un pod no arranca)
kubectl describe pod -n demo <nombre-del-pod>

# Eliminar todo
kubectl delete -k k8s/overlays/local
```

### ¿Qué hay en los manifiestos base?

| Recurso | Para qué sirve |
|---------|---------------|
| `namespace.yaml` | Namespaces `demo` y `demo-monitoring` |
| `api/deployment.yaml` | 2 réplicas del API, con liveness/readiness probes y resource limits |
| `api/secret.yaml` | Secreto con el JWT secret (**cambiar antes de producción**) |
| `rabbitmq/statefulset.yaml` | RabbitMQ con PersistentVolumeClaim (los mensajes sobreviven reinicios) |
| `ingress.yaml` | Nginx Ingress que enruta `/bff` y `/api` al servicio correcto |

---

## 10. Despliegue en Azure

La infraestructura en Azure se define con [Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview) (IaC declarativo, alternativa moderna a ARM templates).

### Recursos que crea

| Recurso | Módulo | Para qué |
|---------|--------|---------|
| AKS Cluster | `aks.bicep` | Kubernetes gestionado (1-5 nodos, autoscaling) |
| Azure Container Registry | `acr.bicep` | Registro privado de imágenes Docker |
| Service Bus Standard | `servicebus.bicep` | Cola de mensajes para el Worker y Functions |
| Key Vault | `keyvault.bicep` | Secretos (JWT secret, connection strings) |

### Deploy completo con un script

```bash
# Necesitas tener instalado: az CLI, kubectl, docker, jq
./infra/deploy.sh dev
```

Este script hace todo en orden:
1. Login en Azure
2. Crea el resource group
3. Despliega la infraestructura con Bicep
4. Obtiene credenciales de AKS
5. Construye y sube las imágenes Docker a ACR
6. Aplica los manifiestos Kubernetes

> **Entornos:** `./infra/deploy.sh dev` · `./infra/deploy.sh staging` · `./infra/deploy.sh prod`

### Deploy manual paso a paso

```bash
# 1. Login
az login

# 2. Crear recursos de Azure
az group create --name rg-demok8s-dev --location eastus
az deployment group create \
  --resource-group rg-demok8s-dev \
  --template-file infra/bicep/main.bicep \
  --parameters environment=dev

# 3. Obtener credenciales de AKS
az aks get-credentials --resource-group rg-demok8s-dev --name demok8s-dev-aks

# 4. Actualizar el overlay con el nombre de tu ACR
# Edita k8s/overlays/azure/kustomization.yaml y reemplaza <ACR_NAME>

# 5. Aplicar manifiestos
kubectl apply -k k8s/overlays/azure
```

---

## 11. Tests

### Tests unitarios

Prueban la lógica de negocio de forma aislada, sin I/O ni base de datos. Usan Moq para los mocks y FluentAssertions para las verificaciones.

```bash
dotnet test src/Api/Api.UnitTests
dotnet test src/BFF/BFF.Tests

# Ejecutar un test concreto
dotnet test src/Api/Api.UnitTests --filter "FullyQualifiedName~GetUsersQueryHandlerTests"
```

### Tests de integración

Levanta el API completo en memoria (`WebApplicationFactory`) con una base de datos InMemory aislada. No necesita servicios externos corriendo.

```bash
dotnet test src/Api/Api.IntegrationTests
```

### Tests E2E (Playwright)

Prueba la aplicación completa desde el navegador. Playwright arranca automáticamente los tres servicios si no están corriendo.

```bash
cd frontend
npm run test:e2e

# Ver el informe HTML con capturas de pantalla
npm run test:e2e:report
```

> **Tip para juniors:** si ya tienes los servicios corriendo con `./start.sh`, Playwright los reutiliza y los tests van más rápido.

### Ejecutar todos los tests

```bash
dotnet test       # todos los proyectos .NET
```

---

## 12. Estructura del proyecto

```
react-netcore-web-api/
│
├── docker/                         # Configuración de contenedores
│   ├── api/Dockerfile              # Multi-stage build del Backend API
│   ├── bff/Dockerfile              # Multi-stage build del BFF
│   ├── gateway/Dockerfile          # Multi-stage build del Gateway
│   ├── worker/Dockerfile           # Build del Worker Service
│   ├── frontend/
│   │   ├── Dockerfile              # Build SPA + nginx
│   │   └── nginx.conf              # Nginx con proxy /bff/* → BFF
│   ├── otel-collector/
│   │   └── otelcol-config.yaml     # Pipelines OTLP → Prometheus / Loki / Tempo
│   ├── prometheus/
│   │   └── prometheus.yml          # Targets de scraping
│   ├── loki/
│   │   └── loki-config.yaml        # Almacenamiento de logs
│   ├── tempo/
│   │   └── tempo-config.yaml       # Almacenamiento de trazas
│   └── grafana/
│       ├── datasources/            # Provisioning automático de datasources
│       └── dashboards/             # Provisioning de dashboards
│
├── k8s/                            # Kubernetes (Kustomize)
│   ├── base/                       # Manifiestos base para todos los entornos
│   │   ├── api/                    # Deployment + Service + Secret del API
│   │   ├── bff/                    # Deployment + Service del BFF
│   │   ├── gateway/                # Deployment + Service del Gateway
│   │   ├── frontend/               # Deployment + Service del Frontend
│   │   ├── worker/                 # Deployment del Worker
│   │   ├── rabbitmq/               # StatefulSet + Service + Secret
│   │   ├── ingress.yaml            # Nginx Ingress
│   │   └── kustomization.yaml      # Lista de recursos del base
│   └── overlays/
│       ├── local/                  # minikube/kind: imagePullPolicy Never
│       └── azure/                  # AKS: imágenes de ACR
│
├── infra/                          # Infraestructura como código
│   ├── bicep/
│   │   ├── main.bicep              # Punto de entrada: orquesta todos los módulos
│   │   └── modules/
│   │       ├── aks.bicep           # AKS Cluster
│   │       ├── acr.bicep           # Azure Container Registry
│   │       ├── servicebus.bicep    # Service Bus + topics + subscriptions
│   │       └── keyvault.bicep      # Key Vault
│   └── deploy.sh                   # Script de deploy end-to-end
│
├── src/
│   ├── Api/                        # Backend API (DDD)
│   │   ├── Api.Domain/             # Entidades, Value Objects, interfaces
│   │   ├── Api.Application/        # Handlers MediatR, validadores
│   │   ├── Api.Infrastructure/     # EF Core, JWT, BCrypt, seeding
│   │   ├── Api.WebApi/             # Controllers, Program.cs, Swagger
│   │   ├── Api.UnitTests/          # xUnit + Moq + FluentAssertions
│   │   └── Api.IntegrationTests/   # WebApplicationFactory
│   ├── BFF/                        # Backend For Frontend (misma estructura en capas)
│   │   ├── BFF.Domain/
│   │   ├── BFF.Application/
│   │   ├── BFF.Infrastructure/     # ApiClient (HttpClient tipado hacia el API)
│   │   ├── BFF.Api/
│   │   └── BFF.Tests/
│   ├── Gateway/
│   │   └── Gateway.Api/            # YARP: routes + clusters en appsettings.json
│   ├── Worker/
│   │   └── Worker.Service/
│   │       ├── Consumers/          # UserCreated, UserDeleted, UserRoleChanged
│   │       ├── Messages/           # Contratos de mensajes (records)
│   │       └── Workers/            # HeartbeatWorker (BackgroundService)
│   └── Functions/
│       └── Functions.App/
│           └── Functions/          # UserHttpFunction, HeartbeatTimer, UserEventFunction
│
├── frontend/                       # React SPA
│   ├── src/
│   │   ├── contexts/               # AuthContext (JWT + sessionStorage)
│   │   ├── services/               # api.ts (Axios + refresh interceptor), authService, userService, sessionService, signalRService, assistantService
│   │   ├── pages/                  # LoginPage, UsersPage, SessionsPage, AdminSessionsPage, AssistantPage, UnauthorizedPage
│   │   ├── components/             # UI components (Radix + Tailwind)
│   │   └── types/                  # Tipos TypeScript
│   ├── e2e/
│   │   ├── pages/                  # Page Object Models
│   │   └── *.spec.ts               # Tests Playwright
│   └── vite.config.ts              # proxy /bff → :5001, plugin Tailwind
│
├── .devcontainer/
│   └── devcontainer.json           # Codespaces: .NET 10 + Node 20 + Playwright
├── .github/
│   └── commit-message-instructions.md  # Conventional Commits en español
├── docker-compose.yml              # Stack completo (app + Postgres + RabbitMQ + observabilidad)
├── docker-compose.infra.yml        # Solo infraestructura (Postgres :5433 + RabbitMQ + observabilidad)
├── .env.example                    # Plantilla de variables de entorno
├── start.sh                        # docker compose up -d --wait (con --obs y --build)
├── stop.sh                         # docker compose down (con --clean para borrar volúmenes)
├── scripts/
│   └── db-reset.sh                 # Reinicia datos de Postgres (DB_RESET=true)
├── CLAUDE.md                       # Instrucciones para Claude Code
└── react-netcore-web-api.slnx      # Solución .NET (todos los proyectos)
```

---

## 13. Referencia de configuración

### Variables de entorno (.env)

| Variable | Valor por defecto | Usado por |
|----------|------------------|-----------|
| `JWT_SECRET` | `CHANGE-THIS-...` | API + BFF |
| `POSTGRES_DB` | `demodb` | Postgres + API (connection string) |
| `POSTGRES_USER` | `demo` | Postgres + API (connection string) |
| `POSTGRES_PASSWORD` | `demo123` | Postgres + API (connection string) |
| `RABBITMQ_USER` | `admin` | RabbitMQ + Worker |
| `RABBITMQ_PASS` | `admin123` | RabbitMQ + Worker |
| `GRAFANA_ADMIN_PASS` | `admin` | Grafana |

La connection string del API se compone automáticamente desde las variables anteriores en [docker-compose.yml](docker-compose.yml):

```
ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
```

Si está vacía (corriendo en local sin Postgres) el API cae a EF Core InMemory automáticamente.

### appsettings.json — API y BFF

```json
{
  "Otel": {
    "Endpoint": "http://localhost:4317"   // OTel Collector (Docker: http://otel-collector:4317)
  },
  "Jwt": {
    "Secret": "...",          // mínimo 32 caracteres, igual en API y BFF
    "Issuer": "demo-api",
    "Audience": "demo-bff",
    "ExpiresInMinutes": "15"  // access token de corta duración; refresh token dura 30 días
  },
  "AzureSignalR": {
    "ConnectionString": ""    // vacío = SignalR local; rellenar para Azure SignalR Service
  }
}
```

### appsettings.json — BFF adicional

```json
{
  "BackendApi": {
    "BaseUrl": "http://localhost:5002/"   // Docker: http://api:8080/
  }
}
```

### appsettings.json — Worker Service

```json
{
  "MessageBus": {
    "Transport": "RabbitMQ",             // o "AzureServiceBus"
    "RabbitMQ": {
      "Host": "localhost",               // Docker: rabbitmq
      "VirtualHost": "/",
      "Username": "admin",
      "Password": "admin123"
    },
    "AzureServiceBus": {
      "ConnectionString": ""             // pegar connection string de Azure
    }
  }
}
```

### Endpoints de la API

| Servicio | Método | Ruta | Auth requerida |
|----------|--------|------|---------------|
| API | POST | `/api/auth/login` | No |
| API | POST | `/api/auth/refresh` | No |
| API | POST | `/api/auth/logout` | JWT |
| API | GET | `/api/users` | JWT |
| API | POST | `/api/users` | JWT · Admin |
| API | DELETE | `/api/users/{id}` | JWT · Admin |
| API | PATCH | `/api/users/{id}/role` | JWT · Admin |
| API | GET | `/api/sessions/my` | JWT |
| API | GET | `/api/sessions` | JWT · Admin |
| API | PATCH | `/api/sessions/{id}/revoke` | JWT |
| API | DELETE | `/api/sessions/my` | JWT |
| API | DELETE | `/api/admin/users/{userId}/sessions` | JWT · Admin |
| API | WS | `/hubs/sessions` | JWT (SignalR) |
| API | GET | `/api/health` | No |
| BFF | POST | `/bff/auth/login` | No |
| BFF | POST | `/bff/auth/refresh` | No |
| BFF | POST | `/bff/auth/logout` | JWT |
| BFF | GET | `/bff/users` | JWT |
| BFF | POST | `/bff/users` | JWT · Admin |
| BFF | DELETE | `/bff/users/{id}` | JWT · Admin |
| BFF | PATCH | `/bff/users/{id}/role` | JWT · Admin |
| BFF | GET | `/bff/sessions/my` | JWT |
| BFF | GET | `/bff/sessions` | JWT · Admin |
| BFF | PATCH | `/bff/sessions/{id}/revoke` | JWT |
| BFF | DELETE | `/bff/sessions/my` | JWT |
| BFF | DELETE | `/bff/admin/users/{userId}/sessions` | JWT · Admin |
| BFF | GET | `/bff/health` | No |
| Gateway | GET | `/health` | No |
| Functions | GET | `/api/functions/users` | No (demo) |
| API | POST | `/api/assistant` | JWT · SSE |
| BFF | POST | `/bff/assistant` | JWT · SSE pass-through |

---

## 14. Credenciales de demo

La base de datos se rellena automáticamente al arrancar:

| Email | Contraseña | Rol | Permisos |
|-------|-----------|-----|---------|
| `admin@demo.com` | `Admin123!` | Admin | read, write, delete, roles:manage |
| `user@demo.com` | `User123!` | Viewer | read |

Las contraseñas se guardan hasheadas con BCrypt.

---

## 15. Decisiones de diseño

| Decisión | Por qué |
|----------|---------|
| **Patrón BFF** | React nunca habla directamente con el API. El BFF es el contrato cliente-servidor; el API puede evolucionar sin romper el frontend |
| **JWT emitido por API, validado por BFF** | El API es la única fuente de verdad de auth. El BFF solo necesita el secreto compartido, no el almacén de usuarios |
| **Postgres en contenedor con fallback InMemory** | El stack containerizado demuestra una BD real (migraciones EF, seed, ACID). Si la connection string está vacía (e.g. tests de integración o demo sin Docker), EF cae a InMemory automáticamente. Lo mejor de ambos mundos: fidelidad de producción + cero fricción para arrancar |
| **DDD + MediatR + CQRS** | Separa la lógica de negocio de la infraestructura. Los controllers son delgados; los handlers son testeables |
| **Red Docker `demo-network` única** | Todos los servicios comparten una red bridge user-defined → service discovery por nombre (`http://api:8080`, `Host=postgres`). Simplifica el modelo mental y migra 1:1 a Kubernetes Services. En producción de verdad conviene separar redes por dominio de confianza |
| **YARP Gateway en Docker/K8s** | Punto de entrada único. Permite añadir rate limiting, circuit breaker y auth centralizada sin tocar los servicios |
| **MassTransit como abstracción** | El mismo código de consumers funciona con RabbitMQ local o Azure Service Bus en Azure, cambiando solo la configuración |
| **Vite proxy** | El frontend usa `/bff/...` relativo. Vite proxea en dev, nginx en Docker. Nunca hay URLs hardcodeadas |
| **Kustomize (no Helm)** | Para un proyecto demo, Kustomize es más legible y directo. Helm tiene más sentido cuando el chart se reutiliza en múltiples deployments |
| **Refresh tokens stateful** | Permiten revocar sesiones individuales y mantener audit trail. El access token dura 15 min; el refresh token 30 días con rotación en cada uso. [ADR-008](docs/adr/ADR-008-stateful-refresh-tokens-signalr.md) |
| **Outbox Pattern + Idempotency** | Los comandos mutantes publican eventos vía `MassTransit.EntityFrameworkCore` Outbox en la misma transacción que el agregado — cero pérdida si el bus cae. El header `X-Idempotency-Key` se gestiona en un middleware único que cachea la respuesta original 24h para deduplicar reintentos sin tocar los handlers. [ADR-009](docs/adr/ADR-009-outbox-pattern-idempotency.md) |
| **SignalR directo al API (no vía BFF)** | El BFF es un proxy REST. Una conexión WebSocket persistente no encaja en ese patrón. El access token JWT es suficiente para autenticar el hub. |
| **SK + SSE + IKernelFactory** | Semantic Kernel orquesta los tool calls; SSE (Server-Sent Events) envía tokens al cliente en streaming sin WebSocket. `IKernelFactory` desacopla el provider LLM (Ollama/OpenAI/Azure) de la lógica del agente. El BFF hace pass-through del stream en lugar de deserializar, preservando la naturaleza de tiempo real del protocolo. [ADR-010](docs/adr/ADR-010-semantic-kernel-agent-sse.md) |
| **OpenTelemetry SDK nativo** | OTel es el estándar de la industria. Exportar a OTLP permite cambiar el backend (Jaeger, Zipkin, DataDog...) sin tocar el código |
| **Serilog con sink OTel** | Serilog es más ergonómico que `ILogger` para logging estructurado y es compatible con OTel para la correlación de trazas |

---

## 16. Convenciones del repositorio

### Mensajes de commit

Este repositorio usa **Conventional Commits en español**. El estándar completo está en [`.github/commit-message-instructions.md`](.github/commit-message-instructions.md) y lo consumen tanto **GitHub Copilot Chat** como **Claude Code** (vía [`CLAUDE.md`](CLAUDE.md)).

```
feat(api): agrega endpoint de búsqueda de usuarios por email

El cliente necesitaba filtrar usuarios sin cargar toda la lista.
Se añade un query parameter opcional ?email= al endpoint GET /api/users.
```

**Tipos permitidos:** `feat` · `fix` · `docs` · `style` · `refactor` · `perf` · `test` · `build` · `ci` · `chore` · `revert`

**Ámbitos sugeridos:** `api` · `bff` · `frontend` · `gateway` · `worker` · `functions` · `auth` · `domain` · `infra` · `tests` · `e2e`

### Ingeniería AI-augmented

El uso de IA no es decorativo — es parte central del showcase. El documento [`docs/ai-workflow.md`](docs/ai-workflow.md) describe en detalle:

- Qué herramientas se usaron (Claude Code CLI + GitHub Copilot IDE) y cómo.
- El workflow real fase a fase: arquitectura → scaffolding → frontend → infra → tests.
- Dónde el AI acertó en el primer intento y dónde necesitó corrección.
- Las reglas de cuándo confiar, cuándo revisar y cuándo descartar el output.

| Herramienta | Configuración |
|-------------|--------------|
| **Claude Code** | [`CLAUDE.md`](CLAUDE.md) — arquitectura del sistema, comandos, skill `/infra` |
| **GitHub Copilot** | [`.vscode/settings.json`](.vscode/settings.json) — apunta al estándar de commits |
