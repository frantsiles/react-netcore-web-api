# ERP — Estado de implementación

> Documento de continuidad. Actualizar al cerrar cada etapa.
> Roadmap completo: [ADR-011](adr/ADR-011-erp-roadmap-multi-tenant.md)

---

## Resumen de etapas

| Etapa | Contenido | Estado |
|---|---|---|
| **E0** | Tenant-aware design | ✅ Completa |
| **E1** | Parties + Catalog | 🟡 Parties completo — Catalog pendiente |
| **E2** | Sales pipeline | ⬜ Pendiente |
| **E3** | Inventory (event sourcing) | ⬜ Pendiente |
| **E3.5** | Switch a control plane real + BD-por-tenant | ⬜ Pendiente |
| **E4** | Purchasing | ⬜ Pendiente |
| **E5** | Accounting (event sourcing) | ⬜ Pendiente |
| **E6** | Banking + Reconciliations | ⬜ Pendiente |
| **E7** | Tax engine completo + Approvals | ⬜ Pendiente |
| **E8** | Reporting consolidado multi-tenant | ⬜ Pendiente |
| **E9** | Personal — HR limitado | ⬜ Pendiente |

---

## E0 — Tenant-aware design ✅

**Completada:** 2026-05-16  
**Tests:** 87/87 ✅

### Qué se construyó

**Dominio (`Api.Domain.Common`)**
- `Money` — value object con Amount + CurrencyCode ISO 4217. Operaciones Add/Subtract/Multiply con validación de misma moneda. Igualdad estructural.
- `AggregateRoot` — base class con domain events (`RaiseDomainEvent`, `ClearDomainEvents`). Todos los agregados de E1+ heredan de aquí.
- `IDomainEvent` — interfaz pura de dominio, sin dependencia de MediatR.
- `ITenantEntity` — interfaz que marca entidades con `TenantId`.

**Application layer (`Api.Application.Common.Interfaces`)**
- `ITenantContext` — expone `TenantId` (Guid) y `CountryCode` (string).
- `ITenantSettings` — configuración tipada por tenant: GetBool / GetInt / GetDecimal / GetString.
- `ITaxEngine` — **placeholder**. Strategy para calcular impuestos por país. Implementaciones reales en E7.
- `IInvoiceFormatter` — **placeholder**. Strategy para formatear facturas por país (PDF/CFDI/MH). Implementaciones reales en E7.
- `IChartOfAccountsTemplate` — **placeholder**. Entradas del plan de cuentas por país para seeding en E3.5.

**Infrastructure (`Api.Infrastructure`)**
- `ClaimsTenantContext` — lee claims `tenant_id` y `country_code` del JWT. Fallback a tenant default `00000000-0000-0000-0000-000000000001` / `"US"` durante E0–E3.
- `DefaultTenantSettings` — lee sección `TenantSettings` de appsettings. Se reemplaza en E3.5 por implementación DB-backed con cache.
- `TenantAwareDbContext` — base DbContext abstracta con global query filter por `TenantId` y auto-stamp en `SaveChanges`. Los DbContexts de Parties y Catalog heredarán de aquí. `AppDbContext` (Identity) **no** hereda — Identity no es tenant-scoped.

**WebApi**
- `TenantEnrichmentMiddleware` — añade `tenant.id` y `tenant.country` a cada OTel span (Activity) y Serilog LogContext. Se ejecuta después de Auth y antes de Idempotency.

**JWT actualizado**
- `JwtTokenGenerator` añade claims `tenant_id` y `country_code` a cada token emitido en login. En E0–E3 el valor es el tenant default hard-coded.

### Decisiones de diseño
- `AppDbContext` (Users, Sessions, Roles) queda separado de `TenantAwareDbContext` — Identity no es tenant-scoped.
- `ITenantContext` vive en Application (no Domain) porque el dominio no necesita saber de HTTP/JWT.
- El tenant default usa un GUID fijo (no random) para facilitar seeds y tests deterministas.

---

## E1 — Parties + Catalog 🟡

**Diseño completo:** [ADR-012](adr/ADR-012-e1-parties-catalog-design.md)  
**Parties completado:** 2026-05-16  
**Tests Parties:** 39/39 ✅

### Qué se construyó — Parties

**`src/Parties/` — módulo independiente (monolito modular)**

**Parties.Domain**
- `Party` — aggregate root. Implementa `ITenantEntity` (TenantId con global query filter). Gestiona roles, addresses, contact points. Invariantes: mínimo 1 role activo, mínimo 1 address.
- `PartyRole` — entity owned por Party. Ciclo de vida: Active / Inactive / Suspended. Soporta CreditLimit (Money), PaymentTermsDays, EmployeeNumber.
- Value objects: `TaxId` (validación por regex según país: US/MX/ES/GT + fallback permisivo), `Address` (AsPrimary/AsSecondary), `ContactPoint` (Email/Phone/Web con validación de formato).
- Domain events: `PartyRegisteredEvent`, `PartyProfileUpdatedEvent`, `PartyRoleActivatedEvent`, `PartyRoleDeactivatedEvent`.
- `IPartyRepository` — GetById, ExistsByTaxId, Search (paginado), Add, Update.

**Parties.Application**
- 9 Commands: RegisterParty, UpdatePartyProfile, AddAddress, RemoveAddress, AddContactPoint, RemoveContactPoint, ActivateRole, DeactivateRole, DeactivateParty.
- 2 Queries: GetPartyById, SearchParties (filtros: legalName, roleType, isActive; paginación skip/take).
- Todos con FluentValidation. Mapeo a `PartyDto` via extension method.
- `AddPartiesApplication()` registra MediatR + validators.

**Parties.Infrastructure**
- `PartiesDbContext` — hereda `TenantAwareDbContext`. Owned entities: Roles, Addresses, ContactPoints (incluyendo TaxId y Money como owned types). Global query filter por TenantId automático.
- `PartyRepository` — InMemory en desarrollo, Npgsql en producción (misma config que AppDbContext).
- `AddPartiesInfrastructure()` registra DbContext + repository.

**Api.WebApi**
- `PartiesController` — 11 endpoints REST: CRUD completo + gestión de roles/addresses/contactPoints.
- Integrado en `Program.cs` vía `AddPartiesApplication()` + `AddPartiesInfrastructure()`.

### Decisiones de diseño ya cerradas

- **Modelo unificado `Party`** con roles (Customer / Supplier / Employee / Contact). No hay entidades separadas `Customer`, `Supplier` ni `Employee` — son roles de la misma Party.
- **SKU inmutable** tras la creación en `CatalogItem` — es referencia externa en Sales, Purchasing e Inventory.
- **`PriceList` como agregado separado** con vigencia (ValidFrom / ValidTo) y entradas escaladas (`MinQuantity` desde E1).
- **Monolito modular**: Parties y Catalog viven como módulos dentro del `Api.WebApi` existente. No hay servicios independientes. Extracción futura si la escala lo justifica.
- **Navegación frontend**: `/parties` con filtro por rol — no rutas separadas por tipo.

### Próximos pasos — Catalog

**Backend — Catalog:**
1. Agregados `CatalogItem` + `PriceList` / `PriceListEntry`
2. Value object `UnitOfMeasure`
3. `ICatalogItemRepository` + `IPriceListRepository`
4. `CatalogDbContext` (puede compartir contexto con Parties o ser separado)
5. 10 Commands + 6 Queries con sus Handlers
6. `CatalogController`

**Frontend:**
- `/parties` — lista con filtros
- `/parties/new` — wizard 2 pasos
- `/parties/:id` — detail con tabs
- `/catalog/items` y `/catalog/items/:id`
- `/catalog/pricelists` y `/catalog/pricelists/:id`
- Inicio del catálogo de componentes UI en `/frontend/src/components/ui/` con ruta `/dev/components`

---

## Decisiones arquitectónicas transversales

| Decisión | Valor | Referencia |
|---|---|---|
| Hosting de módulos | Monolito modular (mismo proceso) | [ADR-012](adr/ADR-012-e1-parties-catalog-design.md), [[feedback-architecture-modular-monolith]] |
| Base de datos | PostgreSQL (+ Marten para event sourcing en E5) | [ADR-011](adr/ADR-011-erp-roadmap-multi-tenant.md) |
| Multi-tenancy | Físico (BD por filial); tenant default hasta E3.5 | [ADR-011](adr/ADR-011-erp-roadmap-multi-tenant.md) |
| Tenant ID default (E0–E3) | `00000000-0000-0000-0000-000000000001` / `"US"` | `ClaimsTenantContext.cs` |
| Strategy registries | Interfaces placeholder en E0; implementaciones reales en E7 | ADR-011 §2 |
