# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Arquitectura general

Sistema de tres capas: **React (puerto 5173) → BFF (puerto 5001) → Backend API (puerto 5002)**.

- El frontend nunca llama directamente al Backend API; todas las peticiones pasan por el BFF.
- El Vite dev server proxea `/bff/*` a `localhost:5001`, evitando CORS y URLs hardcodeadas.
- El Backend API emite JWTs; el BFF los valida con el mismo secreto compartido (`appsettings.json`).
- El Backend API usa **DDD** (Domain → Application → Infrastructure → WebApi) con **MediatR + CQRS** y EF Core InMemory.
- El BFF tiene su propia estructura en capas (Domain / Application / Infrastructure / Api).

## Comandos de desarrollo

### Arrancar todos los servicios

```bash
./start.sh          # lanza los 3 servicios en paralelo y espera a que estén listos
./stop.sh           # mata los procesos en los puertos 5173, 5001 y 5002
```

### Backend API (`src/Api/Api.WebApi`, puerto 5002)

```bash
dotnet run --project src/Api/Api.WebApi
```

### BFF (`src/BFF/BFF.Api`, puerto 5001)

```bash
dotnet run --project src/BFF/BFF.Api
```

### Frontend (puerto 5173)

```bash
cd frontend
npm run dev          # servidor de desarrollo
npm run build        # tsc -b && vite build
npm run lint         # eslint .
npm run preview      # preview del build
```

## Tests

### .NET — unit e integración

```bash
# todos los tests
dotnet test

# un proyecto concreto
dotnet test src/Api/Api.UnitTests
dotnet test src/Api/Api.IntegrationTests
dotnet test src/BFF/BFF.Tests

# un test concreto
dotnet test src/Api/Api.UnitTests --filter "FullyQualifiedName~NombreDelTest"
```

Frameworks: **xUnit + Moq + FluentAssertions**. Los de integración usan `WebApplicationFactory`.

### E2E — Playwright

```bash
cd frontend
npm run test:e2e              # ejecuta todos los tests E2E
npm run test:e2e:report       # abre el informe HTML del último run
```

`playwright.config.ts` arranca automáticamente los tres servicios si no están ya corriendo (`reuseExistingServer: true`). Los Page Objects viven en `frontend/e2e/pages/`.

Credenciales de prueba: `admin@demo.com / Admin123!` y `user@demo.com / User123!`.

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
```

## Estructura del frontend (React)

```
frontend/src/
  contexts/AuthContext.tsx   # estado de autenticación JWT + sessionStorage
  services/
    api.ts                   # instancia Axios + interceptor 401
    authService.ts
    userService.ts
  pages/                     # LoginPage, UsersPage, UnauthorizedPage
  components/                # componentes reutilizables (Radix UI + Tailwind v4)
  types/                     # tipos TypeScript compartidos
  lib/                       # utilidades (clsx, tailwind-merge, cva)
```

Path alias `@` apunta a `frontend/src/`. Tailwind v4 se integra como plugin de Vite (`@tailwindcss/vite`), sin `tailwind.config.*`.

## Mensajes de commit

Sigue las reglas definidas en
[.github/commit-message-instructions.md](.github/commit-message-instructions.md):
**Conventional Commits en español, imperativo, asunto ≤ 72 caracteres,
cuerpo que explique el por qué.**

Esa misma fuente la consume GitHub Copilot Chat
(vía `.vscode/settings.json`), por lo que cualquier ajuste debe hacerse
en ese único archivo para mantener coherencia entre ambas herramientas.
