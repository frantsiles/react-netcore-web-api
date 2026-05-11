# Frontend — React SPA

Aplicación de página única (SPA) construida con **React 19 + Vite + TypeScript**. Parte del stack de demostración cloud-native `react-netcore-web-api`.

## Stack

| Tecnología | Rol |
|------------|-----|
| React 19 + Vite | Framework + bundler con HMR |
| TypeScript 6 | Tipado estático end-to-end |
| Tailwind CSS v4 | Estilos utility-first (via Vite plugin, sin `tailwind.config.*`) |
| Radix UI | Componentes accesibles (dialogs, dropdowns, tooltips) |
| TanStack Query v5 | Estado de servidor, caché y sincronización |
| Axios | HTTP client con interceptor 401 automático |
| Playwright | Tests E2E con Page Object Models |

## Estructura

```
frontend/src/
├── contexts/
│   └── AuthContext.tsx       # Estado JWT + sessionStorage, hook useAuth()
├── services/
│   ├── api.ts                # Instancia Axios + interceptor 401 → redirect login
│   ├── authService.ts        # login(), logout()
│   └── userService.ts        # getUsers(), createUser(), deleteUser()
├── pages/
│   ├── LoginPage.tsx         # Formulario de login con validación
│   ├── UsersPage.tsx         # CRUD de usuarios (solo Admin)
│   └── UnauthorizedPage.tsx  # Vista 403
├── components/               # Componentes UI reutilizables
│   ├── UserTable.tsx         # Tabla de usuarios con acciones
│   ├── CreateUserDialog.tsx  # Dialog Radix para crear usuario
│   └── ...
├── types/
│   └── index.ts              # Tipos TypeScript compartidos (User, AuthResponse…)
└── lib/
    └── utils.ts              # clsx + tailwind-merge (cn helper)
```

## Cómo arrancar

```bash
npm install
npm run dev      # http://localhost:5173
```

El Vite dev server proxea `/bff/*` a `http://localhost:5001`. El BFF debe estar corriendo (ver `start.sh` en la raíz del repo).

## Scripts disponibles

```bash
npm run dev          # Dev server con HMR
npm run build        # Build de producción en dist/
npm run preview      # Preview del build de producción
npx tsc --noEmit     # Solo type-check, sin compilar
npm run test:e2e     # Tests E2E con Playwright
npm run test:e2e:report  # Abre el informe HTML del último run
```

## Decisiones de diseño

**Proxy relativo en lugar de URLs absolutas**

El frontend nunca tiene la URL del BFF hardcodeada. En `vite.config.ts`:

```ts
server: {
  proxy: {
    '/bff': 'http://localhost:5001'
  }
}
```

En Docker, el nginx hace el mismo proxy. En Codespaces, el mismo mecanismo funciona sin cambios. Ver [ADR-001](../docs/adr/ADR-001-bff-pattern.md).

**AuthContext con sessionStorage**

El JWT se guarda en `sessionStorage` (no `localStorage`). Se pierde al cerrar la pestaña — comportamiento intencionado para una demo de autenticación. El `api.ts` incluye un interceptor que redirige al login si cualquier respuesta devuelve 401.

**TanStack Query para estado de servidor**

Las llamadas a `/bff/users` no están en `useEffect` — están en queries de TanStack Query con `staleTime` y `gcTime` configurados. Esto da invalidación automática tras mutaciones (crear/borrar usuario) sin refetch manual.

**Tailwind v4 via Vite plugin**

No hay `tailwind.config.ts`. La configuración del tema es CSS-first en el archivo principal de estilos. Esto es la forma moderna de usar Tailwind v4 — sin archivo de configuración separado.

## Tests E2E

Los tests están en `e2e/` y usan Page Object Models:

```
e2e/
├── pages/
│   ├── LoginPage.ts     # Locators y acciones de la página de login
│   └── UsersPage.ts     # Locators y acciones de la página de usuarios
├── auth.spec.ts         # Flujo de autenticación
└── users.spec.ts        # CRUD de usuarios como admin
```

Credenciales de demo: `admin@demo.com / Admin123!` y `user@demo.com / User123!`.

Playwright arranca los servicios automáticamente si no están corriendo (configurado en `playwright.config.ts` con `webServer`).
