# react-netcore-web-api — Demo Cloud-Native

Repositorio de demostración que muestra cómo construir y operar una aplicación distribuida con **React**, **.NET 9**, **Docker**, **Kubernetes** y **Azure**. Pensado tanto para aprender como para servir de referencia arquitectónica.

> **¿Por qué existe este repo?**  
> Demuestra capacidades de desarrollo full-stack, diseño de microservicios, observabilidad, infraestructura como código y uso de IA (GitHub Copilot + Claude Code) en todo el ciclo de vida del software.

---

## Índice

1. [Arquitectura](#1-arquitectura)
2. [Stack tecnológico](#2-stack-tecnológico)
3. [Inicio rápido — elige tu camino](#3-inicio-rápido--elige-tu-camino)
4. [Modo A · Sin Docker (local)](#4-modo-a--sin-docker-local)
5. [Modo B · Docker Compose](#5-modo-b--docker-compose)
6. [Modo C · GitHub Codespaces](#6-modo-c--github-codespaces)
7. [Servicios nuevos — Gateway, Worker, Functions](#7-servicios-nuevos--gateway-worker-functions)
8. [Observabilidad — Grafana Stack](#8-observabilidad--grafana-stack)
9. [Kubernetes — local y Azure AKS](#9-kubernetes--local-y-azure-aks)
10. [Despliegue en Azure](#10-despliegue-en-azure)
11. [Tests](#11-tests)
12. [Estructura del proyecto](#12-estructura-del-proyecto)
13. [Referencia de configuración](#13-referencia-de-configuración)
14. [Credenciales de demo](#14-credenciales-de-demo)
15. [Decisiones de diseño](#15-decisiones-de-diseño)
16. [Convenciones del repositorio](#16-convenciones-del-repositorio)

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
│          YARP API Gateway  ·  .NET 9                                │
│          localhost:5000  (Docker/K8s: :8080)                        │
│          Routing · Rate limiting · Health check activo de upstream  │
└──────────────────┬─────────────────────────���┬───────────────────────┘
                   │ /bff/*                   │ /api/*
                   ▼                          ▼
┌──────────────────────────┐  ┌────────────────────────────���─────────┐
│  BFF  ·  .NET 9          │  │  Backend API  ·  .NET 9              │
│  localhost:5001          │  │  localhost:5002                       │
│  Valida JWT              │  │  DDD + MediatR + CQRS                │
│  Proxea al API           │  │  FluentValidation                    │
└──────────────────────────┘  │  EF Core InMemory                    │
                              │  Emite JWT · Swagger                 │
                              └──────────────┬───────────────────────┘
                                             │ publica eventos
                                             ▼
                              ┌──────────────────────────────────────┐
                              │  RabbitMQ  (local)                   │
                              │  Azure Service Bus  (Azure)          │
                              └──────────────┬───────────────────────┘
                                             │ consume
                              ┌──────────────▼───────────────────────┐
                              │  Worker Service  ·  .NET 9           │
                              │  MassTransit  ·  BackgroundService   │
                              │  UserCreated / UserDeleted consumers │
                              └──────────────────────────────────────┘

     Azure Functions  (standalone, event-driven)
     ├── HTTP trigger   — GET /functions/users
     ├── Timer trigger  — heartbeat cada 5 min
     └── ServiceBus     — procesa user-events
```

### Flujo de autenticación

```
Browser ──POST /bff/auth/login──▶ BFF ──▶ API (emite JWT)
Browser ◀──────── JWT ─────────── BFF ◀─────────────────
Browser ──GET /bff/users (Bearer)─▶ BFF valida JWT ──▶ API ──▶ DB
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
| Backend runtime | .NET / ASP.NET Core | 9.0 |
| Patrón | DDD + MediatR + CQRS | - |
| Validación | FluentValidation | 12 |
| ORM | EF Core InMemory | 9.0 |
| Autenticación | JWT Bearer | - |
| API Gateway | YARP ReverseProxy | 2.2 |
| Message bus | MassTransit + RabbitMQ / Azure Service Bus | 8.3 |
| Functions | Azure Functions v4 (isolated) | .NET 9 |
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

---

## 3. Inicio rápido — elige tu camino

```
¿Tienes Docker?
├── SÍ ──▶ Modo B (Docker Compose) — sección 5
│          Un solo comando levanta todo, incluyendo RabbitMQ y observabilidad.
│
└── NO ──▶ ¿Usas GitHub Codespaces?
           ├── SÍ ──▶ Modo C (Codespaces) — sección 6
           │          Funciona en el navegador, sin instalar nada.
           │
           └── NO ──▶ Modo A (local sin Docker) — sección 4
                      Necesitas .NET 9 SDK y Node 20.
```

---

## 4. Modo A · Sin Docker (local)

### Requisitos

| Herramienta | Versión mínima | Verificar |
|------------|---------------|-----------|
| .NET SDK | 9.x | `dotnet --version` |
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
cd frontend
npm install
npx playwright install --with-deps chromium   # solo si vas a correr tests E2E
cd ..
```

### Arrancar los servicios principales (API + BFF + React)

```bash
./start.sh
```

Este script arranca los tres servicios en paralelo, espera a que cada puerto esté listo y te muestra las URLs. Pulsa `Ctrl+C` para pararlos todos a la vez.

| Servicio | URL | Descripción |
|----------|-----|-------------|
| Frontend React | http://localhost:5173 | App principal |
| Backend API Swagger | http://localhost:5002/swagger | Explora los endpoints del API |
| BFF Swagger | http://localhost:5001/swagger | Explora los endpoints del BFF |
| API health | http://localhost:5002/api/health | Liveness check |
| BFF health | http://localhost:5001/bff/health | Liveness check |

> **¿Los logs?** Se escriben en `/tmp/api.log`, `/tmp/bff.log` y `/tmp/frontend.log`.  
> Ver en tiempo real: `tail -f /tmp/api.log`

### Parar los servicios

```bash
# Si arrancaste con ./start.sh y cerraste la terminal (procesos huérfanos):
./stop.sh

# O manualmente por puerto:
lsof -ti:5002 | xargs kill -9
```

### Arrancar servicios individualmente (en terminales separadas)

Para trabajar en un servicio concreto sin levantar los demás:

```bash
# Terminal 1 — Backend API
dotnet run --project src/Api/Api.WebApi

# Terminal 2 — BFF
dotnet run --project src/BFF/BFF.Api

# Terminal 3 — Frontend
cd frontend && npm run dev

# Terminal 4 (opcional) — YARP Gateway
dotnet run --project src/Gateway/Gateway.Api

# Terminal 5 (opcional) — Worker Service
dotnet run --project src/Worker/Worker.Service
```

> **Nota para juniors:** el Gateway y el Worker **no son obligatorios** para que la app funcione en modo local sin Docker. El Gateway es el punto de entrada en Docker/Kubernetes; el Worker necesita RabbitMQ corriendo.

---

## 5. Modo B · Docker Compose

### Requisitos

- Docker Desktop (o Docker Engine + Compose plugin)
- Verificar: `docker --version` y `docker compose version`

### Primer uso

```bash
# 1. Clonar
git clone https://github.com/<tu-usuario>/react-netcore-web-api.git
cd react-netcore-web-api

# 2. Crear el archivo de variables de entorno
cp .env.example .env
# Opcional: editar .env y cambiar los valores por defecto
```

> **¿Qué hay en `.env`?**  
> ```
> JWT_SECRET=CHANGE-THIS-SECRET-IN-PRODUCTION-MIN32CHARS!!
> RABBITMQ_USER=admin
> RABBITMQ_PASS=admin123
> GRAFANA_ADMIN_PASS=admin
> ```

### Opción 1 — Solo la aplicación (recomendado para empezar)

Levanta: Frontend + Gateway + BFF + API + RabbitMQ.

```bash
docker compose up -d
```

| Servicio | URL |
|----------|-----|
| Frontend | http://localhost:5173 |
| YARP Gateway | http://localhost:5000 |
| BFF | http://localhost:5001 |
| Backend API | http://localhost:5002/swagger |
| RabbitMQ UI | http://localhost:15672 (admin / admin123) |

### Opción 2 — Aplicación + Observabilidad completa

Añade: OTel Collector + Prometheus + Loki + Tempo + Grafana.

```bash
docker compose --profile observability up -d
```

| Herramienta | URL | Descripción |
|-------------|-----|-------------|
| Grafana | http://localhost:3001 | Dashboard principal (admin / admin) |
| Prometheus | http://localhost:9090 | Métricas raw |
| OTel Collector | localhost:4317 (gRPC) | Receptor OTLP |

### Opción 3 — Solo infraestructura (para desarrollar servicios localmente)

Levanta solo RabbitMQ y el stack de observabilidad, sin los servicios de la app. Ideal cuando quieres desarrollar con `dotnet run` pero tener la infraestructura disponible.

```bash
docker compose -f docker-compose.infra.yml up -d
```

Luego en tu máquina: `./start.sh` o los servicios individualmente.

### Comandos útiles de Docker Compose

```bash
# Ver logs de un servicio en tiempo real
docker compose logs -f api

# Reconstruir una imagen después de cambios en el código
docker compose up -d --build api

# Parar todo
docker compose down

# Parar y borrar volúmenes (base de datos, colas, métricas)
docker compose down -v

# Ver estado de los contenedores
docker compose ps
```

### Cómo funcionan los Dockerfiles

Cada servicio tiene un **multi-stage Dockerfile** en `docker/<servicio>/Dockerfile`:

1. **Stage builder** — usa `mcr.microsoft.com/dotnet/sdk:9.0`, copia los `.csproj` primero (caching de capas), restaura paquetes, luego compila y publica.
2. **Stage final** — usa `mcr.microsoft.com/dotnet/aspnet:9.0` (imagen mínima sin SDK), copia solo los binarios. El usuario es `app` (no root).

El frontend usa `node:20-alpine` para compilar el SPA y `nginx:alpine` para servir los archivos estáticos. El nginx incluye un proxy hacia el BFF para `/bff/*`.

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

## 7. Servicios nuevos — Gateway, Worker, Functions

### YARP API Gateway (`src/Gateway/Gateway.Api`)

**¿Qué es?** Un proxy inverso construido con [YARP (Yet Another Reverse Proxy)](https://microsoft.github.io/reverse-proxy/) que actúa como punto de entrada único en Docker y Kubernetes.

**¿Para qué sirve?**
- En producción/Docker, el cliente solo habla con el Gateway (puerto 5000).
- El Gateway hace health checks activos a los servicios upstream (BFF y API).
- Permite añadir rate limiting, transformaciones de headers y circuit breaker sin tocar los servicios.

**Arrancar:**
```bash
dotnet run --project src/Gateway/Gateway.Api
# Disponible en http://localhost:5000
# Health: http://localhost:5000/health
```

**Configuración de rutas** (`appsettings.json`):
```json
"ReverseProxy": {
  "Routes": {
    "bff-route": { "Match": { "Path": "/bff/{**catch-all}" } },
    "api-route": { "Match": { "Path": "/api/{**catch-all}" } }
  }
}
```

En Docker/K8s el destino de los clusters se sobreescribe con variables de entorno.

---

### Worker Service con MassTransit (`src/Worker/Worker.Service`)

**¿Qué es?** Un servicio de background que consume mensajes de una cola. Usa [MassTransit](https://masstransit.io/) como abstracción sobre el bus de mensajes.

**¿Para qué sirve?**
- Procesa eventos de dominio de forma asíncrona (ej. `UserCreated`, `UserDeleted`).
- **Mismo código** funciona con RabbitMQ en local y con Azure Service Bus en Azure, controlado por la variable `MessageBus:Transport`.

**Arrancar (requiere RabbitMQ):**
```bash
# Primero levanta RabbitMQ
docker compose -f docker-compose.infra.yml up rabbitmq -d

# Luego el Worker
dotnet run --project src/Worker/Worker.Service
```

**Cambiar el transporte** en `appsettings.json`:
```json
"MessageBus": {
  "Transport": "RabbitMQ",   // o "AzureServiceBus"
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "admin",
    "Password": "admin123"
  }
}
```

**Consumers implementados:**
| Consumer | Mensaje | Acción |
|----------|---------|--------|
| `UserCreatedConsumer` | `UserCreated` | Log + procesamiento |
| `UserDeletedConsumer` | `UserDeleted` | Log + limpieza |
| `UserRoleChangedConsumer` | `UserRoleChanged` | Log + auditoría |

---

### Azure Functions (`src/Functions/Functions.App`)

**¿Qué es?** Una Function App con el modelo **Isolated Worker** (.NET 9), la forma moderna de Azure Functions que no depende del proceso del host.

**Triggers implementados:**

| Función | Trigger | Ruta / Schedule | Descripción |
|---------|---------|-----------------|-------------|
| `GetUsers` | HTTP GET | `/api/functions/users` | Devuelve usuarios (demo) |
| `GetUserById` | HTTP GET | `/api/functions/users/{id}` | Devuelve usuario por ID |
| `Heartbeat` | Timer | `0 */5 * * * *` | Cada 5 min, registra latido |
| `ProcessUserCreated` | Service Bus | topic `user-events` | Procesa evento UserCreated |
| `ProcessUserDeleted` | Service Bus | topic `user-events` | Procesa evento UserDeleted |

**Ejecutar localmente** (requiere [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)):
```bash
npm install -g azure-functions-core-tools@4

cd src/Functions/Functions.App
func start
```

**¿Por qué Functions + Worker si hacen algo similar?**

| | Worker Service | Azure Functions |
|--|----------------|-----------------|
| Despliegue | Kubernetes pod | Function App (serverless) |
| Escalado | Manual (réplicas) | Automático según carga |
| Coste | 24/7 | Solo cuando ejecuta |
| Ideal para | Procesamiento continuo | Picos, tareas ocasionales |

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
│   │   ├── services/               # api.ts (Axios), authService, userService
│   │   ├── pages/                  # LoginPage, UsersPage, UnauthorizedPage
│   │   ├── components/             # UI components (Radix + Tailwind)
│   │   └── types/                  # Tipos TypeScript
│   ├── e2e/
│   │   ├── pages/                  # Page Object Models
│   │   └── *.spec.ts               # Tests Playwright
│   └── vite.config.ts              # proxy /bff → :5001, plugin Tailwind
│
├── .devcontainer/
│   └── devcontainer.json           # Codespaces: .NET 9 + Node 20 + Playwright
├── .github/
│   └── commit-message-instructions.md  # Conventional Commits en español
├── docker-compose.yml              # Stack completo (app + infra + observabilidad)
├── docker-compose.infra.yml        # Solo infraestructura (RabbitMQ + observabilidad)
├── .env.example                    # Plantilla de variables de entorno
├── start.sh                        # Arranca API + BFF + React en paralelo
├── stop.sh                         # Para procesos en puertos 5002/5001/5173
├── CLAUDE.md                       # Instrucciones para Claude Code
└── react-netcore-web-api.slnx      # Solución .NET (todos los proyectos)
```

---

## 13. Referencia de configuración

### Variables de entorno (.env)

| Variable | Valor por defecto | Usado por |
|----------|------------------|-----------|
| `JWT_SECRET` | `CHANGE-THIS-...` | API + BFF |
| `RABBITMQ_USER` | `admin` | RabbitMQ + Worker |
| `RABBITMQ_PASS` | `admin123` | RabbitMQ + Worker |
| `GRAFANA_ADMIN_PASS` | `admin` | Grafana |

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
    "ExpiresInMinutes": "60"
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
| API | GET | `/api/users` | JWT |
| API | GET | `/api/health` | No |
| BFF | POST | `/bff/auth/login` | No |
| BFF | GET | `/bff/users` | JWT |
| BFF | GET | `/bff/health` | No |
| Gateway | GET | `/health` | No |
| Functions | GET | `/api/functions/users` | No (demo) |

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
| **EF Core InMemory** | Cero dependencias externas — funciona en Codespaces, CI y local sin instalar nada |
| **DDD + MediatR + CQRS** | Separa la lógica de negocio de la infraestructura. Los controllers son delgados; los handlers son testeables |
| **YARP Gateway en Docker/K8s** | Punto de entrada único. Permite añadir rate limiting, circuit breaker y auth centralizada sin tocar los servicios |
| **MassTransit como abstracción** | El mismo código de consumers funciona con RabbitMQ local o Azure Service Bus en Azure, cambiando solo la configuración |
| **Vite proxy** | El frontend usa `/bff/...` relativo. Vite proxea en dev, nginx en Docker. Nunca hay URLs hardcodeadas |
| **Kustomize (no Helm)** | Para un proyecto demo, Kustomize es más legible y directo. Helm tiene más sentido cuando el chart se reutiliza en múltiples deployments |
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

### Asistentes de IA integrados

| Herramienta | Cómo está configurada |
|-------------|----------------------|
| **Claude Code** | [`CLAUDE.md`](CLAUDE.md) — arquitectura, comandos y skill `/infra` para ayuda con Docker/K8s/Azure |
| **GitHub Copilot** | [`.vscode/settings.json`](.vscode/settings.json) — apunta al estándar de commits |

> El flujo de trabajo AI-assisted es parte del showcase del proyecto: desde scaffolding inicial hasta generación de manifiestos K8s y templates Bicep, con revisión y corrección humana en cada paso.
