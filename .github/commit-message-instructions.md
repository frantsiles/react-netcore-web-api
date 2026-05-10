# Estándar para mensajes de commit

Sigue **Conventional Commits** en **español**, en modo imperativo y describiendo el *por qué* del cambio cuando no sea evidente.

## Formato

```
<tipo>(<ámbito opcional>): <resumen en imperativo, ≤ 72 caracteres>

<cuerpo opcional, párrafos envueltos a 72 columnas, explica el porqué>

<pie opcional: BREAKING CHANGE / refs a issues>
```

## Tipos permitidos

| Tipo       | Cuándo usarlo |
|------------|---------------|
| `feat`     | Nueva funcionalidad para el usuario final |
| `fix`      | Corrección de un bug |
| `docs`     | Solo documentación (README, comentarios, ADRs) |
| `style`    | Formato, espacios, comas — sin cambios de lógica |
| `refactor` | Reestructura sin cambiar comportamiento ni añadir features |
| `perf`     | Mejora de rendimiento |
| `test`     | Añade o corrige tests |
| `build`    | Cambios en sistema de build, dependencias (`npm`, `csproj`, `Dockerfile`) |
| `ci`       | Cambios en pipelines (GitHub Actions, scripts CI) |
| `chore`    | Tareas de mantenimiento que no entran arriba |
| `revert`   | Reversión de un commit anterior |

## Ámbitos sugeridos para este repo

`api`, `bff`, `frontend`, `auth`, `domain`, `infra`, `tests`, `e2e`, `devcontainer`, `scripts`.

Omite el ámbito si el cambio es transversal.

## Reglas del resumen (primera línea)

- Imperativo en español: *"añade"*, *"corrige"*, *"elimina"*, *"renombra"* — no *"añadido"*, *"se corrigió"*.
- Sin punto final.
- Sin mayúscula inicial después de los dos puntos.
- Máximo 72 caracteres.

## Reglas del cuerpo

- Separado del resumen por una línea en blanco.
- Explica **por qué** se hizo el cambio y, si aplica, las consecuencias o alternativas descartadas.
- No describas *qué* hace el código (eso ya lo dice el diff).
- Líneas envueltas a ~72 columnas.

## Cambios incompatibles

Cuando rompas la API pública, contratos JSON o configuración:

```
feat(api)!: cambia formato de respuesta de /users

BREAKING CHANGE: el endpoint ahora devuelve `items` en lugar de `data`.
Clientes deben actualizar su deserializador.
```

El `!` antes de los dos puntos y/o el footer `BREAKING CHANGE:` son obligatorios.

## Buenos ejemplos

```
feat(frontend): añade toggle de tema oscuro en LoginPage

El equipo de UX pidió coherencia con el resto de la suite, que ya usa
preferencia del sistema. Persistimos la elección en sessionStorage para
no contaminar el almacenamiento entre usuarios.
```

```
fix(bff): valida expiración del JWT antes de proxear al backend

Antes el BFF reenviaba tokens caducados y el backend devolvía 500 en
lugar de 401, ocultando la causa real al frontend.
```

```
refactor(api): extrae UserSeeder de Program.cs

Program.cs estaba acumulando lógica de inicialización; aislarlo
facilita los tests de integración y permite reutilizarlo desde la
WebApplicationFactory.
```

```
build(frontend)!: sube React Router a v7

BREAKING CHANGE: las rutas anidadas requieren `Outlet` explícito.
Migradas todas las páginas existentes.
```

```
chore(devcontainer): fija Node a 20 LTS
```

## Antipatrones a evitar

- ❌ `update files` / `cambios varios` / `wip`
- ❌ `fix: arreglo bug` (sin contexto)
- ❌ `Se añadió un nuevo endpoint para...` (no imperativo, no formato)
- ❌ Mezclar varios cambios sin relación en un solo commit — divide el trabajo.
- ❌ Mensajes que describen el diff en lugar del motivo.

## Cuando el cambio es trivial

Una sola línea basta — sin cuerpo:

```
docs(readme): corrige enlace al swagger del BFF
```
