# Demo Guide — ERP Platform

This is a full-stack ERP demo built with **React + .NET 9 + DDD + CQRS**, designed to showcase enterprise-grade architecture patterns. You can explore four independent fictional companies, each with its own data, users, and ERP configuration.

---

## Quick Start

### Option A — Docker (recommended)

```bash
git clone <repo-url>
cd react-netcore-web-api
cp .env.example .env
./start.sh
```

Open [http://localhost:5173](http://localhost:5173) — the app is ready.

### Option B — Local dev servers

```bash
# Terminal 1 — Backend API
dotnet run --project src/Api/Api.WebApi           # port 5002

# Terminal 2 — BFF
dotnet run --project src/BFF/BFF.Api              # port 5001

# Terminal 3 — Frontend
cd frontend && npm install && npm run dev          # port 5173
```

> **First run**: demo data is seeded automatically when the app starts in Development mode. No manual steps needed.

---

## Demo Companies

Four independent fictional companies are pre-loaded. Each has its own customers, suppliers, products, employees, bank accounts, and tax configuration. Log in with any of the accounts below to explore that company's data.

---

### 1. TechSol Distribuciones S.A.
**B2B electronics distributor — Spain (EUR) — Enterprise plan**

> A mid-size IT hardware distributor serving corporate clients across Spain. Manages two warehouses (Madrid and Barcelona), negotiates with major tech brands, and operates a full sales cycle from quotation to invoice.

| | |
|---|---|
| **Country** | Spain 🇪🇸 |
| **Currency** | EUR |
| **Industry** | Electronics Distribution (B2B) |
| **Modules active** | Sales, Purchasing, Inventory (2 warehouses), Accounting, Banking, Tax, HR |

**Login:**

| Email | Password | Role |
|---|---|---|
| `admin@techsol.es` | `TechSol123!` | Admin |

**What to explore:**
- 6 corporate customers with credit limits (€20k–€80k) and payment terms
- 3 suppliers: Samsung, LG, HP
- 8 catalog items (laptops, monitors, peripherals + installation service)
- Stock in 2 warehouses — Madrid (50 units) + Barcelona (30 units)
- Tax rates: IVA 21%, IVA reducido 10%, Exento 0%
- 2 bank accounts: Santander (EUR) + BBVA (USD) with historical transactions
- 6 employees across Sales, Warehouse, IT and Management departments

---

### 2. Nexo Consulting Group
**Professional services firm — Mexico (MXN) — Standard plan**

> A management consulting firm working with financial institutions and large enterprises in Mexico. No physical inventory — revenue comes entirely from billable hours and monthly retainers.

| | |
|---|---|
| **Country** | Mexico 🇲🇽 |
| **Currency** | MXN |
| **Industry** | Management Consulting (B2B services) |
| **Modules active** | Sales, Invoicing, Accounting, Banking, Tax, Approvals, HR |

**Login:**

| Email | Password | Role |
|---|---|---|
| `admin@nexo.mx` | `Nexo123!` | Admin |

**What to explore:**
- 5 high-value clients: banks, insurers, industrial groups
- 5 service catalog items billed by hour or month (strategy, audit, dev, training, PMO)
- Tax: IVA 16% + ISR 10% retention (typical Mexican professional services)
- 1 bank account with payroll and client payment transactions
- 5 employees: CEO, 2 senior/junior consultants, admin, finance analyst

---

### 3. Bella Moda Retail S.A.
**Fashion retail — Argentina (ARS) — Standard plan**

> A Buenos Aires fashion brand selling clothing and accessories to boutiques and online retailers across Argentina. High inventory turnover, dual-currency exposure (ARS + USD), and complex local tax rules.

| | |
|---|---|
| **Country** | Argentina 🇦🇷 |
| **Currency** | ARS |
| **Industry** | Fashion Retail |
| **Modules active** | Sales, Purchasing, Inventory, Accounting, Banking, Tax, HR |

**Login:**

| Email | Password | Role |
|---|---|---|
| `admin@bellamoda.ar` | `BellaModa123!` | Admin |

**What to explore:**
- 4 retail customers + 3 textile/footwear suppliers
- 10 product lines: clothing, shoes, bags, accessories, perfume — all tracked in inventory
- 40 units initial stock per item in the main Buenos Aires warehouse
- Tax: IVA 21% + IIBB 3% (Ingresos Brutos)
- 2 bank accounts: Banco Nación (ARS) + Banco Galicia (USD) — showcases multi-currency
- 4 employees: GM, 2 sales reps, warehouse operator

---

### 4. La Mesa Gourmet S.R.L.
**Restaurant — Spain (EUR) — Free plan**

> A gourmet restaurant in Madrid offering tasting menus, à-la-carte service, and external catering. High-frequency cash operations, perishable inventory, and regular supplier payments make this an ideal showcase for Banking and Reconciliation features.

| | |
|---|---|
| **Country** | Spain 🇪🇸 |
| **Currency** | EUR |
| **Industry** | Food & Beverage / Restaurant |
| **Modules active** | Sales, Purchasing, Inventory, Banking, Tax, HR |

**Login:**

| Email | Password | Role |
|---|---|---|
| `admin@mesagourmet.es` | `MesaGourmet123!` | Admin |

**What to explore:**
- 3 catering clients (events, hotels, corporations)
- 5 suppliers: fresh produce market, dairy, winery, butcher, specialty coffee
- Mixed catalog: 5 menu/service items + 7 perishable products (wine, meat, fish, coffee, bread)
- Kitchen & cellar warehouse with daily stock movements
- Tax: IVA 10% reduced (food service) + IVA 21% (alcohol) — dual rate per item
- 1 CaixaBank account with weekly revenue sweeps and supplier payments
- 5 employees: manager, executive chef, sous chef, 2 waitstaff

---

## Default Admin Account

A global demo account is always available regardless of tenant:

| Email | Password | Tenant |
|---|---|---|
| `admin@demo.com` | `Admin123!` | Demo Company (default) |
| `user@demo.com` | `User123!` | Demo Company (read-only) |

---

## ERP Modules

Every company can use all of the following modules (availability depends on plan):

| Module | What it does |
|---|---|
| **Parties** | Customers, suppliers, contacts — with roles, credit limits, payment terms |
| **Catalog** | Products and services with SKU, unit of measure, tax category, inventory tracking |
| **Sales** | Quotes → Sales Orders → Invoices (full document flow) |
| **Purchasing** | Purchase Orders → Receive stock → update inventory |
| **Inventory** | Multi-warehouse stock tracking, stock movements, reorder alerts |
| **Invoicing** | Invoice lifecycle: Draft → Issued → Paid, with payment recording |
| **Accounting** | Double-entry journal entries, chart of accounts, trial balance, P&L |
| **Banking** | Bank accounts, transactions, reconciliation against journal entries |
| **Tax** | Configurable tax rates by applicability (Sales / Purchase / Both) |
| **Approvals** | Configurable approval workflows with Pending → Approved / Rejected states |
| **HR** | Departments, employees, contracts, salary, employment type |
| **Reports** | Trial Balance, P&L, AR/AP Aging, Inventory Position, KPI Dashboard |
| **Tenants Admin** | Multi-tenant control panel (plan, status, settings) — Admin only |

---

## Architecture Overview

```
React (5173) ──→ YARP Gateway (5000) ──→ BFF (5001) ──→ Backend API (5002)
                                                               │
                                                         RabbitMQ / Azure Service Bus
                                                               │
                                                         Worker Service
                                         Azure Functions (HTTP / Timer / ServiceBus)
```

- **Frontend**: React 19 + TypeScript + Tailwind v4 + Radix UI + TanStack Table
- **BFF**: ASP.NET Core 9 — Backend For Frontend, token validation, request aggregation
- **Backend API**: ASP.NET Core 9 — DDD + CQRS with MediatR, FluentValidation, EF Core
- **Database**: PostgreSQL in Docker / InMemory for local dev
- **Messaging**: MassTransit → RabbitMQ (local) / Azure Service Bus (cloud)
- **Observability**: OpenTelemetry → Grafana + Loki + Tempo + Prometheus
- **Infrastructure**: Docker Compose → Kubernetes (Kustomize) → Azure AKS (Bicep)

---

## Resetting Demo Data

To restore all companies to their initial state:

```bash
# Docker
DB_RESET=true ./start.sh

# Local dev
DB_RESET=true dotnet run --project src/Api/Api.WebApi
```

The seeder is idempotent — if data already exists, it skips. `DB_RESET=true` drops and recreates the database first.

---

## Running Tests

```bash
dotnet test                   # all 273+ unit and integration tests
cd frontend && npm run test:e2e  # Playwright E2E (starts servers automatically)
```

E2E credentials: `admin@demo.com / Admin123!`
