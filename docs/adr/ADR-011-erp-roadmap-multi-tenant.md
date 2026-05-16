# ADR-011: Roadmap funcional — ERP multi-tenant multi-país

**Estado:** Aceptado
**Fecha:** 2026-05-16

---

## Contexto

El proyecto ha consolidado la plataforma técnica: BFF, gateway YARP, DDD/CQRS,
MassTransit con outbox, SignalR, observabilidad OTel, Worker, Azure Functions,
agente IA con Semantic Kernel y Kubernetes con Bicep. Esa base es vitrina técnica
sólida pero el dominio funcional sigue siendo mínimo (sólo usuarios y sesiones).

El siguiente capítulo del proyecto enriquece el dominio para mostrar **calidad
de modelado a la escala de un ERP real estilo QuickBooks/SAP**: clientes,
proveedores, personal, catálogos, ventas, compras, inventario, banca,
conciliaciones, contabilidad, impuestos y reportes — operados por varias
empresas en distintos países desde una sola plataforma.

El reto no es la lista de módulos, sino las **decisiones estructurales** que
los gobiernan: cómo se aíslan los datos entre empresas, cómo se modela la
contabilidad, cómo se soportan jurisdicciones distintas sin contaminar el
dominio con `if (country == "MX")`, y en qué orden se construye sin perder
demostrabilidad continua.

---

## Decisiones

### 1. Multi-tenant físico: una base de datos por filial

Cada empresa o filial vive en su propia base de datos PostgreSQL. Un **control
plane** (BD maestra separada) mantiene el catálogo de tenants, sus connection
strings, su país, su moneda funcional y su plan.

**Por qué:** legislación contable y fiscal varía por país, los datos de una
empresa no deben mezclarse en backups, restauraciones, exports o logs con los
de otra, y el aislamiento físico es la única forma de cumplirlo sin
acrobacias. Una empresa que opere en USA, México y Costa Rica se modela como
tres tenants distintos bajo un mismo grupo (igual que SAP y QuickBooks).

**Alternativa descartada:** multi-tenant lógico (TenantId compartido en una
misma BD). Es más simple, pero ofrece menor compliance, hace los reportes
fiscales más frágiles y limita la escalabilidad por tenant (un tenant ruidoso
afecta a todos). El proyecto necesita demostrar la solución correcta, no la
fácil.

### 2. Jurisdicciones soportadas: USA, México, Costa Rica — extensibles

Se modelan estos tres países como caso inicial, pero el diseño es **abierto a
nuevas jurisdicciones** sin tocar el dominio:

- `ITaxEngine` con implementaciones `TaxEngine.USA`, `TaxEngine.MX`,
  `TaxEngine.CR`, resuelto desde un registry por código de país del tenant.
- `IInvoiceFormatter` análogo, para formatos de factura (CFDI MX, MH CR,
  invoice estándar USA).
- `IChartOfAccountsTemplate` con plantillas de plan de cuentas por país,
  sembradas en provisioning del tenant.
- `INumberingPolicy` para series fiscales por país y sede.

**Por qué:** añadir España, Colombia o Chile en el futuro debe ser registrar
una nueva implementación, no abrir cirugía en agregados. La regla es
**policy-as-data + strategy-as-code**.

### 3. Contabilidad de doble partida real con event sourcing

El módulo de Accounting implementa partida doble real: plan de cuentas, asientos
con débitos y créditos balanceados, libro mayor, balance general y estado de
resultados. El agregado `JournalEntry` usa **event sourcing**: cada asiento es
una secuencia inmutable de eventos, las projections derivan el libro mayor.

**Por qué:** es exactamente lo que hace QuickBooks por debajo. Event sourcing
encaja porque (a) contabilidad nunca borra, sólo reversa — coincide con la
inmutabilidad de eventos; (b) los reportes son proyecciones sobre el mismo
log; (c) la auditoría es gratuita; (d) muestra dominio del patrón en su
escenario más natural.

**Alternativa descartada:** tabla `transactions` con CRUD tradicional.
Funcional pero pierde el escenario donde event sourcing brilla, y no permite
proyecciones múltiples (libro mayor, P&L, balance) sin recomputar.

### 4. Diseño tenant-aware desde el día 1, control plane en E3.5

Todos los agregados llevan `TenantId` y `CountryCode` no-nulos desde el primer
módulo. El `DbContext` aplica un **global query filter** por tenant resuelto
desde un `ITenantContext` scoped. El JWT incluye claims `tenant_id` y
`country_code` desde ya.

Mientras se construyen los primeros módulos (E0 a E3), `ITenantContext`
devuelve un tenant "default" hard-coded; el control plane no se activa hasta
**E3.5**, momento en el que se introduce la BD maestra, el provisioning
workflow y el cambio de `IDbContextFactory` a per-tenant.

**Por qué:** construir control plane completo antes del dominio retrasa 4-6
semanas la primera demo funcional. Construir dominio sin pensar en tenancy
implica refactor doloroso en cada agregado después. Diseñar tenant-aware
desde el día 1 cuesta lo mismo que no hacerlo y deja el switch a multi-tenant
físico como un cambio quirúrgico de dos componentes (`ITenantContext` y
`IDbContextFactory`). Es el patrón que aplicó Shopify.

**Tradeoff aceptado:** durante E0-E3 hay un tenant único; alguien que mire el
código sin contexto puede pensar que la separación es "decorativa". El ADR
y el commit que active multi-tenancy en E3.5 dejan trazabilidad del diseño.

### 5. Tenant Configuration como capability cross-cutting

Cada tenant tiene una tabla `tenant_settings` con configuraciones tipadas por
categoría: `Approvals.Enabled`, `Approvals.ThresholdUSD`,
`Invoicing.SeriesByLocation`, `Inventory.CostingMethod`, `Accounting.FiscalYearStart`,
etc. Un servicio `ITenantSettings` con cache las expone tipadas, y un evento
de cambio invalida la cache.

**Por qué:** los ERPs reales no hard-codean reglas como "facturas sobre $10.000
necesitan aprobación" — las hacen configurables por empresa, e incluso por
sede. Modelarlo así desde el inicio convierte features futuros como Approvals
en consumidores triviales del setting, y exhibe **policy-as-data** como
patrón sistémico en vez de feature aislado.

### 6. Etiquetado: "Etapas funcionales" (E0–E9) para evitar colisión

El roadmap funcional se etiqueta **E0–E9** ("Etapas funcionales"), distinto
del concepto de "Fase 1–9" usado en [docs/ai-workflow.md](../ai-workflow.md)
para referirse a la historia evolutiva de la plataforma técnica. Las Fases
del workflow son pasado; las Etapas del roadmap son futuro.

---

## Bounded contexts

| Contexto | Responsabilidad | Patrón técnico que demuestra |
|---|---|---|
| **Identity** | Usuarios, roles, permisos, auditoría | JWT + RBAC (ya implementado) |
| **Tenancy** | Grupos, empresas/filiales, provisioning, control plane | DbContext factory, migraciones programáticas, workflow de provisioning |
| **Parties** | Clientes, proveedores, empleados (modelo unificado de "party") | Agregados DDD, herencia/composición de roles |
| **Catalog** | Productos físicos y servicios, listas de precios vigentes, multi-currency | Versionado, value objects (`Money`), specifications |
| **Sales** | Cotización → Orden → Factura → Recibo | Saga MassTransit, state machine, CQRS read models, PDF (QuestPDF) |
| **Purchasing** | Orden de compra → Recepción → Factura proveedor → Pago | Espeja Sales, prueba reutilización del patrón |
| **Inventory** | Stock, movimientos, ubicaciones, costeo (FIFO/promedio) | Event sourcing, projection de stock actual |
| **Accounting** | Plan de cuentas, asientos, libro mayor, balance, P&L | Event sourcing puro, invariante de doble partida, transactional outbox |
| **Banking** | Cuentas bancarias, movimientos, conciliación | Algoritmo de matching fuzzy, import OFX/CSV, integración externa con Polly |
| **Tax** | Impuestos, retenciones, descuentos, formatos electrónicos | Strategy registry por país, factory, circuit breaker para PACs externos |
| **Reporting** | Estados financieros, BI, consolidación cross-tenant | Read models dedicados, FX para multi-currency, materialized views |

---

## Roadmap por etapas

| Etapa | Contenido | Salida demostrable |
|---|---|---|
| **E0** (intercalada) | Diseño tenant-aware: claims, global query filter, strategy registries placeholder, `ITenantSettings` | Tests que validan el filter; setting cambiable desde endpoint admin |
| **E1** | Parties + Catalog (multi-currency listo) | CRUD completo de clientes, proveedores, empleados, productos y servicios con listas de precios vigentes |
| **E2** | Sales pipeline + numeración por sede + tax engine **stub** | Cotización PDF → orden → factura PDF → recibo, en USD con tax simple |
| **E3** | Inventory con event sourcing + costeo | Stock auditable, integración con Sales (descarga al facturar) |
| **E3.5** | **Switch a control plane real + BD-por-tenant** | Crear "Filial CR" desde admin: BD nueva, plan de cuentas tico, login con claim de tenant |
| **E4** | Purchasing | PO → recepción → factura proveedor, integra Inventory (carga stock) |
| **E5** | Accounting con event sourcing | Asientos auto-generados desde eventos de Sales/Purchasing, balance general y P&L |
| **E6** | Banking + Reconciliations | Import OFX/CSV, matching fuzzy contra movimientos contables |
| **E7** | Tax engine completo (USA + MX + CR) + Approvals configurable por tenant | Factura CFDI mock, sales tax USA real, MH mock CR, workflow de aprobación opt-in por tenant |
| **E8** | Reporting consolidado multi-tenant | Dashboard ejecutivo cross-empresa con conversión FX |
| **E9** | Personal (HR limitado) | Directorio de empleados + contratos + ausencias (sin payroll) |

Cada etapa entrega algo demostrable end-to-end (video, captura, demo pública).
No se avanza a la siguiente hasta cerrar la actual con tests, observabilidad y
ADR si la decisión lo amerita.

---

## Hilos cross-cutting

- **i18n / l10n**: formatos de fecha, número y moneda por filial. Frontend con
  `react-intl` o equivalente, backend con `CultureInfo` por tenant.
- **Audit trail**: cada cambio sensible queda con quién/cuándo/qué, vía event
  sourcing en Accounting y vía outbox + tabla de auditoría en el resto.
- **Soft delete + period locking**: en contabilidad no se borra, se reversa.
  Los períodos cerrados no se tocan. Disciplina de dominio aplicada con
  invariantes de agregado, no con validación de aplicación.
- **Multi-currency consolidation**: cada filial tiene su moneda funcional;
  los reportes cross-tenant requieren tipos de cambio históricos. Se modela
  desde E1 (en value object `Money`) aunque sólo se explote en E8.
- **Observabilidad tenant-aware**: cada span y log lleva `tenant.id` como
  tag/baggage. Esto se añade en E0 al middleware OTel.

---

## Consecuencias

### Positivas

- Vitrina técnica de calidad enterprise: multi-tenancy físico, event sourcing
  real en contabilidad, strategy registries por país y policy-as-data son
  patrones que pocos proyectos públicos exhiben implementados juntos.
- Demos vendibles por etapa: cada cierre (E1, E2, ...) es un video/post de
  LinkedIn con valor independiente.
- Reusabilidad de plataforma existente: outbox, SignalR, Worker, Functions y
  observabilidad ya están listos y reciben uso real bajo carga de dominio.
- Compliance-friendly por construcción: el aislamiento físico permite reglas
  fiscales y de privacidad por jurisdicción sin gimnasia.

### Negativas / costes asumidos

- El control plane y el provisioning de tenants es complejidad genuina que
  hay que mantener (migraciones N veces, monitoreo por tenant, backups).
- Multi-currency real obliga a usar `Money` value object desde E1 incluso
  cuando todo es USD — disciplina inicial sin retorno inmediato.
- Event sourcing en Accounting es exigente: requiere outbox, projections,
  manejo de versionado de eventos. No se justifica si Accounting fuera CRUD;
  se justifica porque el dominio lo pide.
- El roadmap es ambicioso (~9-12 meses de trabajo serio a tiempo parcial).
  El riesgo de no terminar todas las etapas existe; se mitiga con la regla
  de "cada etapa entrega valor por sí sola".

### Decisiones que quedan abiertas

Las siguientes se documentarán en ADRs futuros cuando llegue su momento:

- Mecanismo concreto de `ITenantContext` (subdominio vs. header vs. claim) — E3.5.
- Estrategia de migraciones N-tenant (sequential vs. paralelo, política de
  rollback) — E3.5.
- Workflow engine para Approvals (in-house simple vs. Elsa / Workflow Core) — E7.
- Generador de PDFs (QuestPDF vs. alternativa) — E2.
- Persistencia de event store para Accounting (tabla EF Core vs. Marten) — E5.
