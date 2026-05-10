# react-netcore-web-api

A demo repository showing how to build a **React** frontend connected to a **.NET BFF (Backend For Frontend)** that proxies requests to a **.NET backend API**, featuring JWT authentication, DDD architecture, and a full test suite that works locally and in **GitHub Codespaces**.

---

## Architecture

```
┌─────────────────────┐       ┌─────────────────────┐       ┌─────────────────────┐
│   React (Vite)      │──────▶│   BFF API (.NET)    │──────▶│   Backend API (.NET)│
│   localhost:5173    │  JWT  │   localhost:5001    │  JWT  │   localhost:5002    │
│                     │◀──────│                     │◀──────│   EF Core InMemory  │
└─────────────────────┘       └─────────────────────┘       └─────────────────────┘
```

- **React 19** — Vite 8 + TypeScript, React Router v7, TanStack Query v5, Axios, **Tailwind CSS v4** (vía `@tailwindcss/vite`), Radix UI primitives + `lucide-react`. Las llamadas a `/bff/...` van al mismo origen; Vite las proxea al BFF (sin URLs hardcodeadas, funciona en Codespaces).
- **BFF** — .NET 9, valida tokens JWT, proxea llamadas al API, Swagger en `/swagger`
- **Backend API** — .NET 9, DDD (Domain / Application / Infrastructure / WebApi), MediatR CQRS, FluentValidation, EF Core InMemory, emite tokens JWT, Swagger en `/swagger`

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 9.x | `dotnet --version` |
| Node.js | 20.x | `node --version` |
| npm | 10.x | bundled with Node 20 |

> **GitHub Codespaces** — no local installation needed. The devcontainer installs everything automatically.

---

## Quick Start

### Option A — One-command startup (recommended)

A single script starts the Backend API, BFF and React frontend in parallel, waits until each port is ready, then prints the URLs:

```bash
./start.sh        # arranca los 3 servicios
./stop.sh         # mata cualquier proceso en 5002, 5001 y 5173
```

Press `Ctrl+C` en la terminal de `start.sh` para parar los tres a la vez. Si arrancaste en background o quedaron procesos huérfanos, usa `./stop.sh` (busca por puerto con `lsof`, intenta SIGTERM y luego SIGKILL).

Logs are written to:

| Service | Log file |
|---------|----------|
| Backend API | `/tmp/api.log` |
| BFF | `/tmp/bff.log` |
| Frontend | `/tmp/frontend.log` |

### Option B — GitHub Codespaces (manual)

1. Click **Code → Codespaces → Create codespace on main**.
2. Wait for the container to build (~2 min). Dependencies are installed automatically.
3. Run `./start.sh` or open three terminals and start each service individually (see below).
4. The browser tab for React (port 5173) opens automatically once Vite is ready.

### Option C — Local development (manual)

```bash
# 1. Restore .NET dependencies
dotnet restore

# 2. Install frontend dependencies + Playwright browsers
cd frontend
npm install
npx playwright install --with-deps chromium
cd ..
```

Then run `./start.sh` or start each service in a separate terminal (see below).

---

## Running the Services Individually

If you prefer to start each service in its own terminal:

### 1 · Backend API (port 5002)

```bash
dotnet run --project src/Api/Api.WebApi
```

Health check: <http://localhost:5002/api/health>  
Swagger UI:   <http://localhost:5002/swagger>

### 2 · BFF API (port 5001)

```bash
dotnet run --project src/BFF/BFF.Api
```

Health check: <http://localhost:5001/bff/health>  
Swagger UI:   <http://localhost:5001/swagger>

### 3 · React frontend (port 5173)

```bash
cd frontend
npm run dev
```

App URL: <http://localhost:5173>

---

## Demo Credentials

La base de datos se seedea automáticamente al arrancar (`Api.Infrastructure/Persistence/Seed/DataSeeder.cs`):

| Email | Password | Rol | Permisos |
|-------|----------|-----|----------|
| `admin@demo.com` | `Admin123!` | Admin | `users:read`, `users:write`, `users:delete`, `roles:manage` |
| `user@demo.com` | `User123!` | Viewer | `users:read` |

Las contraseñas se almacenan hasheadas con BCrypt.

---

## Running Tests

### Unit tests

```bash
dotnet test src/Api/Api.UnitTests
dotnet test src/BFF/BFF.Tests
```

### Integration tests

Requires no running services — the test factory spins up an in-process server with an isolated InMemory database.

```bash
dotnet test src/Api/Api.IntegrationTests
```

### E2E tests (Playwright)

Playwright starts all three services automatically, runs the tests headlessly, then stops them.

```bash
cd frontend
npm run test:e2e
```

Open the HTML report after a run:

```bash
npm run test:e2e:report
```

> **Tip:** If you already have the services running, Playwright reuses them (`reuseExistingServer: true`) and skips the startup wait.

---

## Project Structure

```
react-netcore-web-api/
├── .devcontainer/
│   └── devcontainer.json          # Codespaces (Node 20 + Playwright install)
├── .github/
│   └── commit-message-instructions.md   # Estándar Conventional Commits (consumido por Copilot)
├── .vscode/
│   └── settings.json              # Apunta Copilot al estándar de commits
├── .claude/
│   └── settings.local.json        # Preferencias locales de Claude Code
├── CLAUDE.md                      # Reglas para Claude Code (delega al estándar de commits)
├── frontend/                      # Aplicación React
│   ├── e2e/
│   │   ├── pages/                 # Page Object Models (LoginPage, UsersPage)
│   │   ├── auth.spec.ts           # E2E del flujo de autenticación
│   │   └── users.spec.ts          # E2E de la página de usuarios
│   ├── src/
│   │   ├── components/            # ProtectedRoute + componentes UI (shadcn-style)
│   │   ├── contexts/              # AuthContext (JWT + sessionStorage)
│   │   ├── pages/                 # LoginPage, UsersPage, UnauthorizedPage
│   │   ├── services/              # api.ts (Axios + interceptor 401), authService, userService
│   │   └── types/                 # interfaces TypeScript
│   ├── vite.config.ts             # proxy /bff → :5001, plugin @tailwindcss/vite
│   └── playwright.config.ts
├── src/
│   ├── Api/                       # Backend API (DDD)
│   │   ├── Api.Domain/            # Entities, Value Objects, Repository interfaces
│   │   ├── Api.Application/       # CQRS commands/queries (MediatR), validators
│   │   ├── Api.Infrastructure/    # EF Core InMemory, JWT, BCrypt, seeding
│   │   ├── Api.WebApi/            # Controllers (Auth, Users, Health), Program.cs, Swagger
│   │   ├── Api.UnitTests/         # tests unitarios (xUnit + Moq + FluentAssertions)
│   │   └── Api.IntegrationTests/  # tests de integración (WebApplicationFactory)
│   └── BFF/                       # Backend For Frontend
│       ├── BFF.Domain/
│       ├── BFF.Application/
│       ├── BFF.Infrastructure/    # ApiClient (HttpClient tipado)
│       ├── BFF.Api/               # Controllers (Auth, Users, Health), Program.cs, Swagger
│       └── BFF.Tests/
├── start.sh                       # arranca los 3 servicios en paralelo
├── stop.sh                        # mata procesos en 5002 / 5001 / 5173
└── react-netcore-web-api.slnx     # solución .NET
```

---

## Configuration

### JWT (shared between API and BFF)

Both services in `appsettings.json` use the same secret so the BFF can validate tokens issued by the API:

```json
"Jwt": {
  "Secret": "CHANGE-THIS-SECRET-IN-PRODUCTION-MIN32CHARS!!",
  "Issuer": "demo-api",
  "Audience": "demo-bff",
  "ExpiresInMinutes": "60"
}
```

### BFF → Backend API URL

`src/BFF/BFF.Api/appsettings.json`:

```json
"BackendApi": {
  "BaseUrl": "http://localhost:5002/"
}
```

### React → BFF (Vite proxy)

The frontend never hardcodes the BFF URL. All `/bff/...` requests are intercepted by the Vite dev server and proxied to `http://localhost:5001`. This makes the app work identically in local dev and GitHub Codespaces without any environment-specific configuration.

`frontend/vite.config.ts`:

```ts
proxy: {
  '/bff': {
    target: 'http://localhost:5001',
    changeOrigin: true,
  },
},
```

---

## Key Design Decisions

| Decision | Why |
|----------|-----|
| **BFF pattern** | React never talks to the Backend API directly; the BFF owns the API-to-client contract |
| **JWT issued by API, validated by BFF** | Single source of truth for auth; BFF only holds the shared secret, never the user store |
| **EF Core InMemory** | Zero external dependencies — works in Codespaces and CI out of the box |
| **DDD layers** | Domain ← Application ← Infrastructure ← WebApi enforces dependency direction; domain logic stays pure |
| **MediatR + CQRS** | Commands and queries are decoupled from controllers; `ValidationBehavior` pipeline centralises FluentValidation |
| **Vite proxy for BFF** | Frontend calls `/bff/...` on its own origin; Vite proxies server-side — no CORS issues, no URL changes between local and Codespaces |
| **Playwright webServer** | E2E tests are self-contained — one command starts all services, runs tests, tears down |
| **Tailwind v4 vía plugin Vite** | Sin `tailwind.config.ts` — `@tailwindcss/vite` autogenera la configuración por convención; menos archivos que mantener |

---

## Endpoints

| Servicio | Método | Ruta | Auth |
|----------|--------|------|------|
| API | POST | `/api/auth/login` | público |
| API | GET  | `/api/users` | JWT |
| API | GET  | `/api/health` | público |
| BFF | POST | `/bff/auth/login` | público (proxea al API) |
| BFF | GET  | `/bff/users` | JWT |
| BFF | GET  | `/bff/health` | público |

---

## Convenciones del repositorio

- **Mensajes de commit:** Conventional Commits en español. Estándar completo en [`.github/commit-message-instructions.md`](.github/commit-message-instructions.md). GitHub Copilot Chat y Claude Code (vía [`CLAUDE.md`](CLAUDE.md)) consumen ese mismo archivo, por lo que cualquier ajuste se hace en un único sitio.
- **Runtime:** los `.csproj` apuntan a `net9.0`. Si solo tienes el SDK de .NET 10 instalado, exporta `DOTNET_ROLL_FORWARD=Major` antes de `./start.sh`.
