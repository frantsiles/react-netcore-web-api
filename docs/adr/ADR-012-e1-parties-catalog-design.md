# ADR-012: E1 — Diseño de Parties y Catalog

| Campo | Valor |
|---|---|
| **Estado** | Aceptado |
| **Fecha** | 2026-05-16 |
| **Ámbito** | Bounded contexts Parties y Catalog (Etapa E1 del roadmap ADR-011) |
| **Depende de** | ADR-011 (roadmap ERP), ADR-002 (DDD/CQRS), ADR-004 (EF Core) |

---

## Contexto

E1 es la primera etapa funcional del roadmap ERP. Introduce los dos bounded contexts
fundacionales sobre los que todo el dominio se apoya:

- **Parties**: actores del negocio — clientes, proveedores, empleados, contactos.
- **Catalog**: bienes y servicios que se compran, venden o consumen, con listas de precios vigentes.

Ambos contextos deben estar tenant-aware desde el día 1 (según ADR-011 §4),
soportar multi-currency desde el modelo (aunque solo se use USD inicialmente),
y ser lo suficientemente sólidos para que Sales (E2), Purchasing (E4) y Accounting (E5)
los referencien sin refactor.

El proyecto también cumple una función como **base comercial reutilizable**: las decisiones
de diseño deben ser justificables en producción real, no solo demostrativas.

---

## Decisiones de diseño

### 1. Modelo unificado de Party con roles

Una `Party` es la entidad raíz para cualquier actor: cliente, proveedor, empleado
o contacto. Los roles son relaciones comerciales que se activan o desactivan de
forma independiente. El mismo RFC/TIN, razón social y dirección no se duplican
en tablas separadas.

**Por qué:** un proveedor que también es cliente (relación común en B2B) no debería
tener dos registros con el mismo TaxId. Un empleado que es también contacto no
debería sincronizarse manualmente. El modelo unificado elimina esa fricción desde
el inicio.

**Alternativa descartada:** entidades separadas (`Customer`, `Supplier`, `Employee`).
Más simple al inicio, pero genera duplicación de datos master (nombre, dirección,
TaxId) y hace búsquedas cross-rol innecesariamente complejas.

**Decisión de tabla:** una sola tabla `parties` con campo `party_type`
(Individual | Organization). No herencia TPH/TPT — los campos condicionales por tipo
se permiten nulos. Más simple, sin joins adicionales.

### 2. Money como value object compartido desde E1

`Money` (Amount + CurrencyCode ISO 4217) vive en un proyecto `SharedKernel` y es
usado por ambos bounded contexts desde el inicio.

**Por qué:** introducirlo en E3 o E5 implicaría migrar columnas en Parties
(CreditLimit), Catalog (UnitPrice) y PriceList. El costo de hacerlo bien desde E1
es mínimo; el costo de migrarlo después es alto.

**Reglas del value object:**
- `Amount >= 0` siempre
- Operaciones aritméticas solo entre instancias de la misma `CurrencyCode`
- La igualdad incluye `CurrencyCode`

### 3. PriceList como agregado separado con vigencia y entradas escaladas

Las listas de precios son entidades de negocio independientes con ciclo de vida
propio (creación, publicación, expiración). Se modelan como agregado raíz separado
de `CatalogItem`.

Se incluye `MinQuantity` en `PriceListEntry` desde E1 para soportar precios por
volumen. El costo en el modelo es mínimo (un campo decimal más); omitirlo
significaría una migración dolorosa cuando Sales (E2) necesite descuentos por
cantidad.

### 4. TaxId como value object con validación por país

El TaxId no es un string libre: su formato depende del país del tenant.

| País | Persona física | Persona jurídica |
|---|---|---|
| US | SSN (XXX-XX-XXXX) | EIN (XX-XXXXXXX) |
| MX | RFC persona física (13 chars) | RFC persona moral (12 chars) |
| CR | Cédula física (9 dígitos) | NITE / Cédula jurídica |

La validación se realiza en el value object con regex por `CountryCode`. No se
valida contra padrón fiscal externo (eso es E7 — Tax engine completo).

### 5. SKU inmutable tras la creación

El `SKU` de un `CatalogItem` es inmutable una vez creado porque actúa como
referencia externa en Sales, Purchasing e Inventory. Cambiarlo rompe historial.

Si un SKU necesita "cambiar", el flujo correcto es desactivar el ítem actual y
crear uno nuevo.

### 6. Navegación frontend: ruta única /parties con filtro por rol

Una ruta `/parties` con filtro multi-select por rol (Customer / Supplier / Employee /
Contact) en lugar de rutas separadas `/customers`, `/suppliers`, etc.

**Por qué:** el modelo unificado lo pide — una Party con tres roles aparecería en
tres listas distintas y sería confuso. La búsqueda cross-rol (¿quién es a la vez
cliente y proveedor?) es trivial con filtro, imposible con rutas separadas.

---

## Bounded context: Parties

### Agregado: `Party`

```
Party
├── PartyId          : Guid          (aggregate root)
├── TenantId         : Guid          (global query filter)
├── CountryCode      : string        ("US" | "MX" | "CR")
├── PartyType        : enum          Individual | Organization
├── LegalName        : string        (requerido, no en blanco)
├── TradeName        : string?       (nombre comercial, opcional)
├── TaxId            : TaxId?        (value object, nullable si Individual + país lo permite)
├── Addresses        : Address[]     (1..N)
├── ContactPoints    : ContactPoint[] (0..N)
├── Roles            : PartyRole[]   (1..N, al menos uno activo siempre)
└── IsActive         : bool
```

**Invariantes:**
- `Roles` nunca puede quedar vacío
- No puede desactivarse el último rol activo — lanzar `DomainException`
- `LegalName` no puede estar en blanco ni ser solo espacios
- `TaxId` puede ser nulo solo si `PartyType == Individual` y el país no lo exige
- Un rol ya activo no puede activarse nuevamente (idempotente: sin efecto, sin error)

### Entidad: `PartyRole` (dentro del agregado Party)

```
PartyRole
├── RoleType           : enum    Customer | Supplier | Employee | Contact
├── Status             : enum    Active | Inactive | Suspended
├── ActivatedAt        : DateTimeOffset
├── DeactivatedAt      : DateTimeOffset?
├── CreditLimit        : Money?  (solo Customer)
├── PaymentTermsDays   : int?    (Customer y Supplier)
└── EmployeeNumber     : string? (solo Employee)
```

### Value objects: Parties

| Value Object | Campos | Notas |
|---|---|---|
| `TaxId` | `CountryCode` + `Value` | Validación por regex según país y PartyType |
| `Address` | `Line1`, `Line2?`, `City`, `StateOrRegion`, `PostalCode`, `CountryCode` | |
| `ContactPoint` | `Type` (Email\|Phone\|Web), `Value`, `IsPrimary` | Formato validado |
| `Money` | `Amount` (decimal), `CurrencyCode` (ISO 4217) | Definido en SharedKernel |

### Casos de uso: Parties

**Commands:**

| Command | Descripción |
|---|---|
| `RegisterPartyCommand` | Crea Party con al menos un rol. Falla si LegalName vacío o rol inválido. |
| `UpdatePartyProfileCommand` | Actualiza LegalName, TradeName, TaxId. |
| `AddAddressCommand` | Agrega dirección. Primera dirección se marca como principal. |
| `UpdateAddressCommand` | Actualiza campos de una dirección existente. |
| `RemoveAddressCommand` | Elimina dirección. Falla si es la única. |
| `AddContactPointCommand` | Agrega punto de contacto. |
| `RemoveContactPointCommand` | Elimina punto de contacto. |
| `ActivateRoleCommand` | Activa un rol. Idempotente si ya está activo. |
| `DeactivateRoleCommand` | Desactiva un rol. Falla si es el último activo. |
| `DeactivatePartyCommand` | Desactiva todos los roles (soft delete a nivel Party). |

**Queries:**

| Query | Descripción |
|---|---|
| `GetPartyByIdQuery` | Retorna Party completa con roles, direcciones y contactos. |
| `SearchPartiesQuery` | Paginada. Filtros: rol(es), estado, búsqueda libre (LegalName, TradeName, TaxId). |
| `ListPartiesByRoleQuery` | Lista compacta (id + nombre) para lookups en Sales/Purchasing. |

**Eventos de dominio:**

| Evento | Cuándo |
|---|---|
| `PartyRegisteredEvent` | Al completar `RegisterPartyCommand`. |
| `PartyRoleActivatedEvent` | Al activar un rol previamente inactivo. |
| `PartyRoleDeactivatedEvent` | Al desactivar un rol. |
| `PartyProfileUpdatedEvent` | Al modificar LegalName, TradeName o TaxId. |

---

## Bounded context: Catalog

### Agregado: `CatalogItem`

```
CatalogItem
├── CatalogItemId    : Guid     (aggregate root)
├── TenantId         : Guid     (global query filter)
├── CountryCode      : string
├── ItemType         : enum     Product | Service
├── SKU              : string   (único por tenant, inmutable tras crear)
├── Name             : string   (requerido)
├── Description      : string?
├── UnitOfMeasure    : UnitOfMeasure  (value object, código UN/CEFACT)
├── TaxCategory      : string   ("STANDARD" | "EXEMPT" | "REDUCED")
├── DefaultCurrency  : string   (ISO 4217)
├── IsActive         : bool
├── TrackInventory   : bool     (solo Product)
└── ReorderPoint     : decimal? (solo Product + TrackInventory = true)
```

**Invariantes:**
- `SKU` único por tenant — validado en Application antes de persistir
- `SKU` inmutable tras la creación
- `TrackInventory` y `ReorderPoint` solo aplican a `ItemType == Product`
- `Name` no puede estar en blanco

### Agregado: `PriceList`

```
PriceList
├── PriceListId   : Guid       (aggregate root)
├── TenantId      : Guid       (global query filter)
├── Name          : string
├── CurrencyCode  : string     (ISO 4217 — toda la lista en una moneda)
├── ValidFrom     : DateOnly
├── ValidTo       : DateOnly?  (null = sin vencimiento)
├── IsDefault     : bool       (exactamente una por tenant)
└── Entries       : PriceListEntry[]
```

**Entidad `PriceListEntry`** (dentro del agregado PriceList):

```
PriceListEntry
├── CatalogItemId : Guid     (referencia — no FK fuerte entre contextos)
├── UnitPrice     : Money    (misma moneda que la lista)
└── MinQuantity   : decimal  (= 1.0 si precio único; > 1.0 para precios por volumen)
```

**Invariantes de `PriceList`:**
- `ValidFrom < ValidTo` si ValidTo tiene valor
- No dos entries con el mismo `CatalogItemId` + `MinQuantity` en la misma lista
- Exactamente una `PriceList` con `IsDefault = true` por tenant (invariante de Application, no de agregado)
- Al desactivar un ítem con entradas vigentes: comportamiento configurable vía `ITenantSettings` — por defecto es advertencia, no bloqueo

### Value objects: Catalog

| Value Object | Campos | Notas |
|---|---|---|
| `UnitOfMeasure` | `Code` (string UN/CEFACT) | Ej: "EA" (unidad), "KGM" (kg), "HUR" (hora), "MON" (mes) |
| `Money` | `Amount`, `CurrencyCode` | Compartido con Parties, definido en SharedKernel |

### Casos de uso: Catalog

**Commands:**

| Command | Descripción |
|---|---|
| `CreateCatalogItemCommand` | Crea ítem con SKU único por tenant. |
| `UpdateCatalogItemCommand` | Actualiza Name, Description, UoM, TaxCategory. SKU no modificable. |
| `DeactivateCatalogItemCommand` | Soft delete. Evalúa entradas vigentes según `ITenantSettings`. |
| `CreatePriceListCommand` | Crea lista. Si `IsDefault = true`, desactiva el flag en la lista anterior. |
| `UpdatePriceListCommand` | Actualiza Name, ValidFrom, ValidTo. |
| `AddPriceEntryCommand` | Agrega entrada. Valida unicidad de CatalogItemId + MinQuantity. |
| `UpdatePriceEntryCommand` | Actualiza UnitPrice o MinQuantity de una entrada. |
| `RemovePriceEntryCommand` | Elimina entrada. |
| `SetDefaultPriceListCommand` | Cambia la lista default del tenant (transaccional). |

**Queries:**

| Query | Descripción |
|---|---|
| `GetCatalogItemByIdQuery` | Retorna ítem completo. |
| `GetCatalogItemBySKUQuery` | Lookup por SKU (usado desde Sales/Purchasing). |
| `SearchCatalogItemsQuery` | Paginada. Filtros: nombre, SKU, tipo, activo. |
| `GetItemPriceQuery` | Item + fecha + cantidad → precio efectivo de la lista relevante. |
| `ListPriceListsQuery` | Lista todas las PriceLists del tenant con estado de vigencia. |
| `GetActivePriceListsQuery` | Solo las vigentes en fecha dada. |

**Eventos de dominio:**

| Evento | Cuándo |
|---|---|
| `CatalogItemCreatedEvent` | Al crear un ítem nuevo. |
| `CatalogItemDeactivatedEvent` | Al desactivar un ítem. |
| `PriceListPublishedEvent` | Al crear/actualizar una lista con ValidFrom <= hoy. |

---

## Pantallas

### Parties

| Ruta | Descripción |
|---|---|
| `/parties` | Lista paginada. Filtros: rol (chips multi-select), estado, búsqueda libre. Columnas: Nombre, TaxId, Roles (chips), País, Estado. Acciones: Nueva, Ver/Editar. |
| `/parties/new` | Wizard 2 pasos: (1) datos básicos — PartyType, CountryCode, LegalName, TradeName, TaxId; (2) primer rol + CreditLimit/PaymentTermsDays si Customer. |
| `/parties/:id` | Detail con tabs: **Perfil** \| **Roles** \| **Direcciones** \| **Contactos** \| **Historial**. |

### Catalog

| Ruta | Descripción |
|---|---|
| `/catalog/items` | Lista paginada. Filtros: tipo (Product/Service), activo. Columnas: SKU, Nombre, Tipo, UoM, Moneda, Estado. |
| `/catalog/items/new` | Form con secciones: Identificación \| Clasificación fiscal \| Inventario (visible solo si Product). |
| `/catalog/items/:id` | Mismo form en modo edición. SKU read-only. |
| `/catalog/pricelists` | Lista de PriceLists con badge de vigencia (Vigente / Próxima / Expirada). Badge de Default. |
| `/catalog/pricelists/new` | Form: Nombre, Moneda, ValidFrom, ValidTo. |
| `/catalog/pricelists/:id` | Header con metadatos + tabla de Entries editable in-place (agregar/editar/eliminar fila sin modal). |

---

## Estructura de proyectos a crear

```
src/
  SharedKernel/
    SharedKernel/               ← nuevo proyecto — Money, TaxId, otros VOs compartidos

  Parties/
    Parties.Domain/             ← Party, PartyRole, value objects, eventos, IPartyRepository
    Parties.Application/        ← Commands, Queries, Handlers MediatR, validadores
    Parties.Infrastructure/     ← EF Core DbContext, repositorio, migraciones
    Parties.Api/                ← Controladores HTTP (o se expone desde Api.WebApi via módulo)

  Catalog/
    Catalog.Domain/             ← CatalogItem, PriceList, value objects, eventos
    Catalog.Application/        ← Commands, Queries, Handlers
    Catalog.Infrastructure/     ← EF Core DbContext, repositorio, migraciones
    Catalog.Api/                ← Controladores HTTP
```

**Decisión de hosting HTTP (cerrada):** los controladores de Parties y Catalog se
implementan como módulos dentro del `Api.WebApi` existente — mismo proceso, mismo
puerto. No se crean servicios independientes en E1.

**Por qué:** microservicios sin justificación operacional real añaden latencia de red,
deploys coordinados y distributed tracing obligatorio para llamadas que son locales.
Los límites de módulo ya están en el código (namespaces, DbContexts separados,
proyectos `.Domain` / `.Application` / `.Infrastructure` propios) — extraer un módulo
a servicio independiente en el futuro es cirugía limpia sobre seams ya definidos,
no refactor total. Esta es la estrategia que aplican Shopify y Stack Overflow a escala
considerable antes de cualquier extracción.

---

## Catálogo de componentes React (paralelo a E1)

Conforme se construyan las pantallas de E1, se extraen a `/frontend/src/components/ui/`
los controles reutilizables:

| Componente | Uso en E1 |
|---|---|
| `DataTable` | Listas de Parties y CatalogItems |
| `FilterBar` | Chips de rol, dropdowns de estado, búsqueda |
| `StatusBadge` | Active / Inactive / Suspended / Vigente / Expirada |
| `FormWizard` | Alta de Party (2 pasos) |
| `TabPanel` | Detail de Party |
| `InlineEditTable` | Entries de PriceList |
| `MoneyInput` | CreditLimit, UnitPrice |
| `CountrySelect` | CountryCode en forms |
| `RoleChips` | Visualización de roles en lista |

Estos componentes se documentan en una ruta `/dev/components` visible solo en desarrollo,
que actúa como catálogo vivo sin necesidad de Storybook.

---

## Consecuencias

### Positivas
- Parties unificado elimina duplicación de datos master para el 80% de los casos reales de negocio.
- `Money` desde E1 evita migraciones dolorosas en E2–E5.
- Precios escalados desde E1 evitan refactor cuando Sales necesite descuentos por volumen.
- SKU inmutable garantiza integridad referencial sin FK dura entre contextos.
- El catálogo de componentes crece orgánicamente — cada pantalla de E1 aporta controles reutilizables para E2 en adelante.

### Negativas / costes asumidos
- El modelo de Party es más complejo que una tabla `customers` simple. Quien vea el código por primera vez necesita leer el ADR para entender la intención.
- `Money` con validación de moneda igual obliga a ser explícito en conversiones cross-currency — en E1 no hay conversión, por lo que la restricción existe antes de necesitarse.
- El catálogo de componentes requiere disciplina para no construir pantallas ad-hoc que luego no sean reutilizables.

### Decisiones que quedan abiertas para ADRs futuros
- Cuándo y qué módulo extraer primero a servicio independiente — cuando el volumen
  o la necesidad de escala independiente lo justifique (no antes).
- Estrategia de validación de TaxId contra padrón externo (SAT MX, SUNAT, Hacienda CR) — E7.
- Generador de PDFs para documentos de Sales — E2 (mencionado en ADR-011).
