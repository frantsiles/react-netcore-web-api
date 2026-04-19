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

- **React** — Vite + TypeScript, React Router v7, React Query v5, Axios
- **BFF** — .NET 10, validates JWT tokens, proxies API calls, exposes Swagger at `/swagger`
- **Backend API** — .NET 10, DDD (Domain / Application / Infrastructure / WebApi), MediatR CQRS, FluentValidation, EF Core InMemory, issues JWT tokens, Swagger at `/swagger`

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | 20.x | `node --version` |
| npm | 10.x | bundled with Node 20 |

> **GitHub Codespaces** — no local installation needed. The devcontainer installs everything automatically.

---

## Quick Start

### Option A — GitHub Codespaces (recommended)

1. Click **Code → Codespaces → Create codespace on main**.
2. Wait for the container to build (~2 min). Dependencies are installed automatically.
3. Open **three terminals** and run one service per terminal (see [Running the services](#running-the-services) below).
4. The browser tab for React (port 5173) opens automatically once Vite is ready.

### Option B — Local development

```bash
# 1. Restore .NET dependencies
dotnet restore

# 2. Install frontend dependencies + Playwright browsers
cd frontend
npm install
npx playwright install --with-deps chromium
cd ..
```

Then start each service in a separate terminal (see below).

---

## Running the Services

Open **three terminals**, one per service. Start them in this order:

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

The database is seeded automatically on startup:

| Email | Password | Role |
|-------|----------|------|
| `admin@demo.com` | `Admin123!` | Admin (all permissions) |
| `user@demo.com` | `User123!` | Viewer (read-only) |

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
│   └── devcontainer.json          # Codespaces config (Node 20 + Playwright install)
├── frontend/                      # React application
│   ├── e2e/
│   │   ├── pages/                 # Page Object Models (LoginPage, UsersPage)
│   │   ├── auth.spec.ts           # Auth flow E2E tests
│   │   └── users.spec.ts          # Users page E2E tests
│   ├── src/
│   │   ├── components/            # ProtectedRoute
│   │   ├── contexts/              # AuthContext (JWT + sessionStorage)
│   │   ├── pages/                 # LoginPage, UsersPage, UnauthorizedPage
│   │   ├── services/              # api.ts (Axios), authService, userService
│   │   └── types/                 # TypeScript interfaces
│   └── playwright.config.ts
├── src/
│   ├── Api/                       # Backend API (DDD)
│   │   ├── Api.Domain/            # Entities, Value Objects, Repository interfaces
│   │   ├── Api.Application/       # CQRS commands/queries (MediatR), validators
│   │   ├── Api.Infrastructure/    # EF Core InMemory, JWT, BCrypt, seeding
│   │   ├── Api.WebApi/            # Controllers, Program.cs, Swagger
│   │   ├── Api.UnitTests/         # xUnit unit tests
│   │   └── Api.IntegrationTests/  # xUnit integration tests (WebApplicationFactory)
│   └── BFF/                       # Backend For Frontend
│       ├── BFF.Domain/
│       ├── BFF.Application/
│       ├── BFF.Infrastructure/    # HttpClient wrapper (ApiClient)
│       ├── BFF.Api/               # Controllers, Program.cs, Swagger
│       └── BFF.Tests/
└── react-netcore-web-api.slnx     # .NET solution file
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

### React BFF URL

`frontend/.env.development`:

```
VITE_BFF_URL=http://localhost:5001
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
| **Playwright webServer** | E2E tests are self-contained — one command starts all services, runs tests, tears down |
