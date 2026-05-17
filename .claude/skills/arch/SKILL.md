---
name: arch
description: "Guía de arquitectura completa para este proyecto ERP: cómo están organizadas las capas, los patrones canónicos por capa, y el checklist paso a paso para agregar un módulo nuevo de inicio a fin (backend DDD → BFF → YARP → frontend React). DISPARADORES: 'cómo agrego un módulo', 'nuevo módulo ERP', 'scaffold', 'cómo funciona la arquitectura', 'dónde va X', 'cómo se conecta el BFF', 'patrón DDD', 'cómo registro el DbContext', 'cómo agrego una ruta YARP', 'cómo agrego un nav item', '/arch', 'arch guide', 'architecture guide', 'add new module', 'wire up feature'."
---

# Guía de arquitectura — ERP React + .NET Core

Este skill codifica el conocimiento arquitectónico del proyecto para que Claude no tenga que re-derivarlo de cero cada sesión. Úsalo antes de implementar cualquier módulo nuevo, hacer cambios cross-layer, o cuando necesites entender dónde vive algo.

---

## Mapa de la arquitectura

```
React (5173)
   └─ Vite proxy: /bff/* → localhost:5001
         │
         ▼
   BFF.Api (5001)
   ├─ BFF.Application  ← MediatR handlers que llaman al API via HTTP
   ├─ BFF.Infrastructure ← IApiClient (HttpClient)
   └─ BFF.Domain        ← IBffMessages (records de request/response)
         │
         ▼ HTTP (api/...)
   Api.WebApi (5002)
   ├─ Controllers        ← thin: solo ISender.Send(cmd)
   ├─ Api.Application    ← MediatR: Commands, Queries, Validators
   ├─ Api.Infrastructure ← DbContexts, Repositories, Services
   └─ [Module].Domain    ← Aggregates, Entities, VOs, Events, IRepository
         │
         ▼
   Postgres (5432)
   (una schema por módulo: parties, sales, purchasing, inventory, ...)
```

**Regla crítica:** El frontend NUNCA llama a `api/...` directamente. Siempre pasa por `/bff/...`. El BFF valida el JWT, extrae el token y lo reenvía al API.

---

## Estructura de proyectos clave

```
src/
  Api/
    Api.Domain/           # SOLO: Users, Roles, Permissions, Sessions
    Api.Application/      # SOLO: Auth, Users, Sessions, common behaviors
    Api.Infrastructure/   # SOLO: AppDbContext (users/roles), ClaimsTenantContext, Seeder
    Api.WebApi/           # Program.cs, Controllers/, ModuleMigrator.cs, DemoDataSeeder.cs

  # ERP modules — cada uno es su propio "bounded context":
  # Purchasing.Domain, Purchasing.Application, Purchasing.Infrastructure
  # Sales.Domain, Sales.Application, Sales.Infrastructure ... etc.
  # (los módulos ERP NO están en Api.Domain — tienen sus propios namespaces)

  BFF/
    BFF.Domain/           # Interfaces: IApiClient
    BFF.Application/      # Handlers MediatR por módulo (Purchasing/, Sales/, ...)
    BFF.Infrastructure/   # ApiClient: implementación HTTP
    BFF.Api/              # Controllers BFF

  Gateway/
    Gateway.Api/          # YARP reverse proxy (puerto 5000) — punto de entrada Docker/K8s
                          # Rutas en appsettings.json → ReverseProxy.Routes
```

---

## Multi-tenant: cómo funciona

Cada request HTTP al API lleva un JWT. La clase `ClaimsTenantContext` (`Api.Infrastructure/Tenant/ClaimsTenantContext.cs`) extrae dos claims del JWT:
- `tenant_id` → `ITenantContext.TenantId` (Guid)
- `country_code` → `ITenantContext.CountryCode` (string, "MX", "CR", "US", etc.)

Todos los aggregates ERP implementan `ITenantEntity` y tienen `TenantId`. **Toda query debe filtrar por `TenantId`** para que los datos de un tenant no sean visibles a otro.

El `TenantEnrichmentMiddleware` agrega `TenantId` y `CountryCode` a cada span OTel y contexto Serilog automáticamente.

---

## Demo data

`DemoDataSeeder.cs` (`Api.WebApi/Infrastructure/`) siembra **5 empresas** independientes con sus propios usuarios admin:

| Tenant | País | Moneda | Plan | Email admin |
|--------|------|--------|------|-------------|
| TechSol Distribuciones S.A. | ES | EUR | Enterprise | admin@techsol.es / TechSol123! |
| Nexo Consulting Group | MX | MXN | Standard | admin@nexo.mx / Nexo123! |
| Bella Moda Retail S.A. | AR | ARS | Standard | admin@bellamoda.ar / BellaModa123! |
| La Mesa Gourmet S.R.L. | ES | EUR | Free | admin@mesagourmet.es / MesaGourmet123! |
| MercaMás S.A. | CR | CRC | Enterprise | admin@mercamas.cr / MercaMas123! |

Cada empresa tiene su propio método `SeedXxxAsync` que puede extenderse sin resetear la base de datos. El seeder es idempotente: si el tenant ya existe, lo omite.

---

## Checklist completo: agregar un módulo ERP nuevo

Sigue los pasos en orden. Cada paso tiene el patrón exacto.

---

### PASO 1 — Domain (el corazón del módulo)

**Ubicación:** crear `src/[Module]/[Module].Domain/`

**Qué va aquí:**
- El Aggregate Root (hereda de `AggregateRoot<Guid>`, implementa `ITenantEntity`)
- Entidades hijas
- Value Objects
- Domain Events (`IRequest<Unit>` o simplemente `record`)
- Interface del repositorio `I[Module]Repository`
- Enums de estado

**Patrón del Aggregate:**
```csharp
// [Module].Domain/[Entity]/[Entity].cs
public sealed class Widget : AggregateRoot<Guid>, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public WidgetStatus Status { get; private set; }

    private Widget() { }  // EF Core

    public static Widget Create(string name, Guid tenantId)
    {
        var w = new Widget { Id = Guid.NewGuid(), Name = name, TenantId = tenantId, Status = WidgetStatus.Active };
        w.AddDomainEvent(new WidgetCreatedEvent(w.Id, tenantId));
        return w;
    }

    public void Deactivate()
    {
        if (Status != WidgetStatus.Active) throw new DomainException("Widget no está activo");
        Status = WidgetStatus.Inactive;
    }
}
```

**Patrón del repositorio:**
```csharp
// [Module].Domain/[Entity]/Repositories/I[Entity]Repository.cs
public interface IWidgetRepository
{
    Task<Widget?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Widget>> SearchAsync(Guid tenantId, string? name, CancellationToken ct = default);
    void Add(Widget widget);
}
```

---

### PASO 2 — Application (comandos y queries MediatR)

**Ubicación:** `src/[Module]/[Module].Application/[Entity]/Commands/` y `Queries/`

**Patrón de un comando:**
```csharp
// Commands/CreateWidget/CreateWidgetCommand.cs
public record CreateWidgetCommand(string Name) : IRequest<WidgetDto>;

// Commands/CreateWidget/CreateWidgetCommandHandler.cs
public class CreateWidgetCommandHandler(
    IWidgetRepository repo,
    IUnitOfWork uow,
    ITenantContext tenant) : IRequestHandler<CreateWidgetCommand, WidgetDto>
{
    public async Task<WidgetDto> Handle(CreateWidgetCommand request, CancellationToken ct)
    {
        var widget = Widget.Create(request.Name, tenant.TenantId);
        repo.Add(widget);
        await uow.SaveChangesAsync(ct);
        return widget.ToDto();
    }
}

// Commands/CreateWidget/CreateWidgetCommandValidator.cs
public class CreateWidgetCommandValidator : AbstractValidator<CreateWidgetCommand>
{
    public CreateWidgetCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

**Patrón de una query:**
```csharp
// Queries/SearchWidgets/SearchWidgetsQuery.cs
public record SearchWidgetsQuery(string? Name, int Skip, int Take)
    : IRequest<IReadOnlyList<WidgetDto>>;

public class SearchWidgetsQueryHandler(IWidgetRepository repo, ITenantContext tenant)
    : IRequestHandler<SearchWidgetsQuery, IReadOnlyList<WidgetDto>>
{
    public async Task<IReadOnlyList<WidgetDto>> Handle(SearchWidgetsQuery request, CancellationToken ct)
        => (await repo.SearchAsync(tenant.TenantId, request.Name, ct))
           .Select(w => w.ToDto()).ToList();
}
```

---

### PASO 3 — Infrastructure (DbContext + Repository + Migration)

**3a. DbContext**
```csharp
// [Module].Infrastructure/Persistence/WidgetsDbContext.cs
public class WidgetsDbContext(DbContextOptions<WidgetsDbContext> opts, ITenantContext tenant)
    : DbContext(opts)
{
    public DbSet<Widget> Widgets => Set<Widget>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("widgets");
        mb.Entity<Widget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.TenantId);
        });
    }
}
```

**3b. DesignTime factory** (necesaria para `dotnet ef migrations add`):
```csharp
// [Module].Infrastructure/Persistence/WidgetsDbContextFactory.cs
public class WidgetsDbContextFactory : IDesignTimeDbContextFactory<WidgetsDbContext>
{
    public WidgetsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<WidgetsDbContext>()
            .UseNpgsql("Host=localhost;Database=demo;Username=postgres;Password=postgres")
            .Options;
        return new WidgetsDbContext(opts, new DesignTimeTenantContext());
    }
}
```

**3c. Repository:**
```csharp
// [Module].Infrastructure/Persistence/Repositories/WidgetRepository.cs
public class WidgetRepository(WidgetsDbContext db) : IWidgetRepository
{
    public async Task<Widget?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
        => await db.Widgets.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Widget>> SearchAsync(Guid tenantId, string? name, CancellationToken ct)
        => await db.Widgets
            .Where(w => w.TenantId == tenantId)
            .Where(w => name == null || w.Name.Contains(name))
            .ToListAsync(ct);

    public void Add(Widget widget) => db.Widgets.Add(widget);
}
```

**3d. Generar la migration:**
```bash
cd src/[Module]/[Module].Infrastructure
dotnet ef migrations add InitialCreate \
  --context WidgetsDbContext \
  --output-dir Persistence/Migrations
```

---

### PASO 4 — Registrar DI en Program.cs

Abrir `src/Api/Api.WebApi/Program.cs`. Encontrar la sección donde se registran los otros módulos y agregar:

```csharp
// Widgets module
builder.Services.AddDbContext<WidgetsDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IWidgetRepository, WidgetRepository>();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateWidgetCommand).Assembly));
```

---

### PASO 5 — Registrar en ModuleMigrator

Abrir `src/Api/Api.WebApi/Infrastructure/ModuleMigrator.cs` y agregar:

```csharp
await MigrateAsync<WidgetsDbContext>(sp, logger);
```

---

### PASO 6 — Controlador en Api.WebApi

```csharp
// src/Api/Api.WebApi/Controllers/WidgetsController.cs
[ApiController]
[Route("api/widgets")]
[Authorize]
public class WidgetsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWidgetCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetWidgetByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? name, [FromQuery] int skip = 0, [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await sender.Send(new SearchWidgetsQuery(name, skip, take), ct));
}
```

**Nota:** El controlador no extrae `TenantId` manualmente. `ClaimsTenantContext` lo inyecta automáticamente via DI. Solo agrega `[Authorize]`.

---

### PASO 7 — BFF Messages

```csharp
// src/BFF/BFF.Application/Widgets/WidgetsBffMessages.cs
namespace BFF.Application.Widgets;

public record WidgetBffDto(Guid Id, string Name, string Status, DateTime CreatedAt);

public record SearchWidgetsBffQuery(string Token, string? Name, int Skip, int Take)
    : IRequest<IReadOnlyList<WidgetBffDto>>;

public record CreateWidgetBffCommand(string Token, string Name)
    : IRequest<WidgetBffDto>;
```

---

### PASO 8 — BFF Handlers

```csharp
// src/BFF/BFF.Application/Widgets/WidgetsBffHandlers.cs
using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Widgets;

public class SearchWidgetsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchWidgetsBffQuery, IReadOnlyList<WidgetBffDto>>
{
    public async Task<IReadOnlyList<WidgetBffDto>> Handle(
        SearchWidgetsBffQuery req, CancellationToken ct)
    {
        var qs = req.Name != null ? $"?name={Uri.EscapeDataString(req.Name)}" : "";
        var result = await apiClient.GetAsync<List<WidgetBffDto>>(
            $"api/widgets{qs}", req.Token, ct);
        return result ?? [];
    }
}

public class CreateWidgetBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateWidgetBffCommand, WidgetBffDto>
{
    public async Task<WidgetBffDto> Handle(CreateWidgetBffCommand req, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, WidgetBffDto>(
            "api/widgets", new { req.Name }, req.Token, ct);
        return result!;
    }
}
```

---

### PASO 9 — BFF Controller

```csharp
// src/BFF/BFF.Api/Controllers/WidgetsController.cs
[ApiController]
[Route("bff/widgets")]
[Authorize]
public class WidgetsController(ISender sender, IHttpContextAccessor ctx) : ControllerBase
{
    private string Token => ctx.HttpContext!.Request.Headers.Authorization
        .ToString().Replace("Bearer ", "");

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? name, [FromQuery] int skip = 0, [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await sender.Send(new SearchWidgetsBffQuery(Token, name, skip, take), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWidgetBffCommand cmd, CancellationToken ct)
        => Ok(await sender.Send(cmd with { Token = Token }, ct));
}
```

---

### PASO 10 — Ruta YARP en Gateway

Abrir `src/Gateway/Gateway.Api/appsettings.json`. En la sección `ReverseProxy.Routes`, agregar:

```json
"bff-widgets-route": {
  "ClusterId": "bff-cluster",
  "Match": { "Path": "/bff/widgets/{**catch-all}" }
},
```

El cluster `bff-cluster` ya existe y apunta al BFF en `:5001`. Solo se necesita la ruta.

---

### PASO 11 — Frontend: tipos TypeScript

```typescript
// frontend/src/types/erp/widgets.ts
export type WidgetStatus = 'Active' | 'Inactive'

export interface WidgetDto {
  id: string
  name: string
  status: WidgetStatus
  createdAt: string
}

export interface CreateWidgetBody {
  name: string
}
```

---

### PASO 12 — Frontend: service

```typescript
// frontend/src/features/widgets/widgetsService.ts
import api from '@/services/api'
import type { WidgetDto, CreateWidgetBody } from '@/types/erp/widgets'

export const widgetsService = {
  search: (params?: { name?: string }) =>
    api.get<WidgetDto[]>('/bff/widgets', { params }).then(r => r.data),

  create: (body: CreateWidgetBody) =>
    api.post<WidgetDto>('/bff/widgets', body).then(r => r.data),
}
```

---

### PASO 13 — Frontend: página (patrón mínimo)

```typescript
// frontend/src/features/widgets/WidgetsPage.tsx
import { useQuery } from '@tanstack/react-query'
import { widgetsService } from './widgetsService'

export function WidgetsPage() {
  const { data: widgets, isLoading } = useQuery({
    queryKey: ['widgets'],
    queryFn: () => widgetsService.search(),
  })
  // ... render table con TanStack Table (ver SalesPage.tsx o PurchasingPage.tsx como referencia)
}
```

Ver `frontend/src/features/purchasing/PurchasingPage.tsx` como referencia completa de la estructura de página (tabla + dialogs + mutations + formularios con zod+react-hook-form).

---

### PASO 14 — Frontend: nav item

```typescript
// frontend/src/components/layout/nav-items.ts
// Agregar en el grupo apropiado:
{ title: 'Widgets', href: '/widgets', icon: SquareStack }
```

```typescript
// frontend/src/App.tsx
// Agregar la ruta:
<Route path="/widgets" element={<WidgetsPage />} />
```

---

### PASO 15 — Demo data (opcional pero recomendado)

En `src/Api/Api.WebApi/Infrastructure/DemoDataSeeder.cs`, en el método `SeedXxxAsync` del tenant más apropiado, agregar los registros demo del nuevo módulo. El seeder es idempotente por diseño.

---

## Resumen del checklist (para verificar)

```
[ ] 1.  Domain: Aggregate + IRepository + Events
[ ] 2.  Application: Commands + Queries + Validators + DTOs
[ ] 3a. Infrastructure: DbContext con HasDefaultSchema("module")
[ ] 3b. Infrastructure: IDesignTimeDbContextFactory
[ ] 3c. Infrastructure: Repository impl
[ ] 3d. dotnet ef migrations add InitialCreate --context XxxDbContext
[ ] 4.  Program.cs: AddDbContext + AddScoped<IRepo> + AddMediatR
[ ] 5.  ModuleMigrator.cs: MigrateAsync<XxxDbContext>
[ ] 6.  Api.WebApi/Controllers/XxxController.cs [Authorize]
[ ] 7.  BFF.Application/Xxx/XxxBffMessages.cs (records)
[ ] 8.  BFF.Application/Xxx/XxxBffHandlers.cs (IApiClient calls)
[ ] 9.  BFF.Api/Controllers/XxxController.cs [Authorize]
[ ] 10. Gateway appsettings.json: ruta bff-xxx-route
[ ] 11. frontend/src/types/erp/xxx.ts
[ ] 12. frontend/src/features/xxx/xxxService.ts
[ ] 13. frontend/src/features/xxx/XxxPage.tsx
[ ] 14. nav-items.ts + App.tsx
[ ] 15. DemoDataSeeder (seed de datos demo)
```

---

## Pitfalls conocidos (cosas que se olvidan)

1. **TenantId en queries**: Toda query al repositorio DEBE filtrar por `TenantId`. Sin esto, un tenant puede ver datos de otro.

2. **DI del DbContext**: Si no se registra en `Program.cs`, el servidor arranca pero `ModuleMigrator` falla en runtime con "service not found".

3. **ModuleMigrator**: Si no se agrega `MigrateAsync<XxxDbContext>`, las tablas no se crean al arrancar. Nada falla silenciosamente — hay un error EF al primer query.

4. **YARP route**: Sin la ruta en `appsettings.json` del Gateway, el módulo funciona en desarrollo local (donde el BFF es directo en `:5001`) pero falla en Docker/K8s donde todo pasa por el Gateway en `:5000`.

5. **BFF Token extraction**: El BFF extrae el token del header `Authorization: Bearer XXX` y lo reenvía al API como Bearer. Si el comando/query BFF no lleva `Token`, `IApiClient` hace la llamada sin auth y el API devuelve 401.

6. **IDesignTimeDbContextFactory**: Sin este archivo, `dotnet ef migrations add` falla porque no puede instanciar el DbContext (que necesita DI para `ITenantContext`). La factory usa `DesignTimeTenantContext` que es un stub hardcodeado.

7. **Nav item route vs page route**: El `href` en nav-items y la ruta en `App.tsx` deben coincidir exactamente.

8. **`strictFunctionTypes: false`** en `tsconfig.app.json`: Este flag está desactivado para compatibilidad con TanStack Table. No modificarlo.

---

## Módulos ERP existentes (para referencia de patrones)

| Módulo | Schema DB | BFF Application | Frontend |
|--------|-----------|----------------|----------|
| Parties | `parties` | `BFF.Application/Parties/` | `features/parties/` |
| Catalog | `catalog` | `BFF.Application/Catalog/` | `features/catalog/` |
| Sales | `sales` | `BFF.Application/Sales/` | `features/sales/` |
| Purchasing | `purchasing` | `BFF.Application/Purchasing/` | `features/purchasing/` |
| Inventory | `inventory` | `BFF.Application/Inventory/` | `features/inventory/` |
| Accounting | `accounting` | `BFF.Application/Accounting/` | `features/accounting/` |
| Banking | `banking` | `BFF.Application/Banking/` | `features/banking/` |
| Tax | `tax` | `BFF.Application/Tax/` | `features/tax/` |
| Approvals | `approvals` | `BFF.Application/Approvals/` | `features/approvals/` |
| HR | `hr` | `BFF.Application/HR/` | `features/hr/` |
| Invoicing | `invoicing` | `BFF.Application/Invoicing/` | `features/invoicing/` |
| Control Plane (tenants) | `controlplane` | `BFF.Application/Tenants/` | `features/admin/` |

**Módulo de referencia recomendado para copiar patrones:** `Purchasing` — es completo, tiene state machine, líneas de documento, y recepción parcial.
