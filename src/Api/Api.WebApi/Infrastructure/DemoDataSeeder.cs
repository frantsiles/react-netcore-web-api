using Api.Domain.Common;
using Banking.Domain.BankAccounts;
using Banking.Infrastructure.Persistence;
using Catalog.Domain.Catalog;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using ControlPlane.Domain.Tenants;
using ControlPlane.Infrastructure.Persistence;
using HR.Domain.Contracts;
using HR.Domain.Departments;
using HR.Domain.Employees;
using HR.Infrastructure.Persistence;
using Inventory.Domain.Warehouses;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Parties.Domain.Parties;
using Parties.Domain.ValueObjects;
using Parties.Infrastructure.Persistence;
using Tax.Domain.TaxRates;
using Tax.Infrastructure.Persistence;

namespace Api.WebApi.Infrastructure;

public static class DemoDataSeeder
{
    private static readonly Guid TechSolId    = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid NexoId       = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid BellaModaId  = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid MesaGourmetId = Guid.Parse("00000000-0000-0000-0000-000000000005");

    public static async Task SeedAsync(IServiceProvider provider, ILogger logger)
    {
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var controlPlane = sp.GetRequiredService<ControlPlaneDbContext>();
        if (await controlPlane.Tenants.AnyAsync(t => t.Id == TechSolId)) return;

        logger.LogInformation("Seeding demo companies...");

        await SeedTenantsAsync(controlPlane);
        await SeedTechSolAsync(sp);
        await SeedNexoAsync(sp);
        await SeedBellaModaAsync(sp);
        await SeedMesaGourmetAsync(sp);

        logger.LogInformation("Demo seed completed: 4 companies, full ERP data loaded.");
    }

    // ── Tenants ───────────────────────────────────────────────────────────────

    private static async Task SeedTenantsAsync(ControlPlaneDbContext db)
    {
        var companies = new[]
        {
            Tenant.CreateWithId(TechSolId,    "TechSol Distribuciones S.A.", "techsol",    "ES", "EUR", TenantPlan.Enterprise),
            Tenant.CreateWithId(NexoId,       "Nexo Consulting Group",       "nexo",       "MX", "MXN", TenantPlan.Standard),
            Tenant.CreateWithId(BellaModaId,  "Bella Moda Retail S.A.",      "bellamoda",  "AR", "ARS", TenantPlan.Standard),
            Tenant.CreateWithId(MesaGourmetId,"La Mesa Gourmet S.R.L.",      "mesagourmet","ES", "EUR", TenantPlan.Free),
        };

        await db.Tenants.AddRangeAsync(companies);
        await db.SaveChangesAsync();
    }

    // ── TechSol Distribuciones S.A. ──────────────────────────────────────────

    private static async Task SeedTechSolAsync(IServiceProvider sp)
    {
        var tid = TechSolId;

        // Parties
        var parties = sp.GetRequiredService<PartiesDbContext>();
        var customers = new[]
        {
            MakeOrg("Tecno Hogar S.L.",            "ES", PartyRoleType.Customer, 30_000m, "EUR", 30, tid),
            MakeOrg("Sistemas Empresariales Corp",  "ES", PartyRoleType.Customer, 50_000m, "EUR", 45, tid),
            MakeOrg("Digital Office S.A.",          "ES", PartyRoleType.Customer, 25_000m, "EUR", 30, tid),
            MakeOrg("Corporación Ibérica Tech",     "ES", PartyRoleType.Customer, 80_000m, "EUR", 60, tid),
            MakeOrg("Retail Electronics S.A.",      "ES", PartyRoleType.Customer, 40_000m, "EUR", 30, tid),
            MakeOrg("InfoSystems Corp",             "ES", PartyRoleType.Customer, 20_000m, "EUR", 15, tid),
        };
        var suppliers = new[]
        {
            MakeOrg("Samsung Distribution Europe", "DE", PartyRoleType.Supplier, 0m, "EUR", 45, tid),
            MakeOrg("LG Electronics Spain",        "ES", PartyRoleType.Supplier, 0m, "EUR", 30, tid),
            MakeOrg("HP Inc. España",              "ES", PartyRoleType.Supplier, 0m, "EUR", 30, tid),
        };
        await parties.Parties.AddRangeAsync([..customers, ..suppliers]);
        await parties.SaveChangesAsync();

        // Catalog
        var catalog = sp.GetRequiredService<CatalogDbContext>();
        var items = new[]
        {
            MakeProduct("LAP-PRO-15",  "Laptop Pro 15\" i7",          "ES", "EUR", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 5m),
            MakeProduct("MON-27-4K",   "Monitor 27\" 4K",             "ES", "EUR", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 10m),
            MakeProduct("TEC-MEC-PRO", "Teclado Mecánico Pro",        "ES", "EUR", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 20m),
            MakeProduct("MOU-WIRE",    "Mouse Inalámbrico Ultra",     "ES", "EUR", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 20m),
            MakeProduct("CAB-HDMI-2",  "Cable HDMI 2.0 2m",          "ES", "EUR", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 50m),
            MakeProduct("RTR-WIFI6",   "Router WiFi 6 Pro",          "ES", "EUR", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 10m),
            MakeProduct("SWT-24P",     "Switch 24 Puertos",          "ES", "EUR", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 5m),
            MakeService("SVC-INST",    "Instalación y Configuración", "ES", "EUR", UnitOfMeasure.Hour,    tid),
        };
        await catalog.CatalogItems.AddRangeAsync(items);
        await catalog.SaveChangesAsync();

        // Warehouses + inventory
        var inventory = sp.GetRequiredService<InventoryDbContext>();
        var whMad = Warehouse.Create("WH-MAD", "Almacén Central Madrid", "Polígono Industrial Norte, Madrid");
        whMad.TenantId = tid;
        var whBcn = Warehouse.Create("WH-BCN", "Almacén Barcelona", "Zona Franca, Barcelona");
        whBcn.TenantId = tid;
        await inventory.Warehouses.AddRangeAsync(whMad, whBcn);
        await inventory.SaveChangesAsync();

        foreach (var item in items.Where(i => i.TrackInventory))
        {
            var invMad = MakeInventoryItem(item.Id, whMad.Id, item.SKU, tid);
            invMad.Receive(50, "SEED-INITIAL");
            var invBcn = MakeInventoryItem(item.Id, whBcn.Id, item.SKU, tid);
            invBcn.Receive(30, "SEED-INITIAL");
            await inventory.InventoryItems.AddRangeAsync(invMad, invBcn);
        }
        await inventory.SaveChangesAsync();

        // Tax
        var tax = sp.GetRequiredService<TaxDbContext>();
        await tax.TaxRates.AddRangeAsync(
            TaxRate.Create(tid, "IVA-21", "IVA General 21%",   21m, TaxApplicability.Both,     "Tipo general del IVA español"),
            TaxRate.Create(tid, "IVA-10", "IVA Reducido 10%",  10m, TaxApplicability.Both,     "Tipo reducido equipos eléctricos"),
            TaxRate.Create(tid, "IVA-0",  "Exento 0%",          0m, TaxApplicability.Both,     "Operaciones exentas"));
        await tax.SaveChangesAsync();

        // Banking
        var banking = sp.GetRequiredService<BankingDbContext>();
        var cta1 = BankAccount.Create(tid, "0049-2323-81-2310000000", "Banco Santander",  "EUR");
        cta1.AddTransaction(DateTime.UtcNow.AddDays(-30), "Saldo inicial",          150_000m, BankTransactionType.Credit,  "OPEN-001");
        cta1.AddTransaction(DateTime.UtcNow.AddDays(-15), "Cobro cliente TS-001",   18_500m,  BankTransactionType.Credit,  "COB-001");
        cta1.AddTransaction(DateTime.UtcNow.AddDays(-10), "Pago proveedor Samsung", 45_000m,  BankTransactionType.Debit,   "PAG-001");
        var cta2 = BankAccount.Create(tid, "0182-1234-90-0100000001", "BBVA",             "USD");
        cta2.AddTransaction(DateTime.UtcNow.AddDays(-20), "Transferencia USD inicial", 50_000m, BankTransactionType.Credit, "OPEN-002");
        await banking.BankAccounts.AddRangeAsync(cta1, cta2);
        await banking.SaveChangesAsync();

        // HR
        var hr = sp.GetRequiredService<HrDbContext>();
        var deptMgmt  = Department.Create(tid, "MGMT",  "Dirección General");
        var deptSales = Department.Create(tid, "SALES", "Ventas", costCenter: "CC-SALES");
        var deptWare  = Department.Create(tid, "WARE",  "Almacén y Logística", costCenter: "CC-WARE");
        var deptIT    = Department.Create(tid, "IT",    "Tecnología", costCenter: "CC-IT");
        await hr.Departments.AddRangeAsync(deptMgmt, deptSales, deptWare, deptIT);
        await hr.SaveChangesAsync();

        var hireDate = new DateOnly(2020, 1, 15);
        var director = Employee.Hire(tid, "TS-001", "Carlos",   "Martínez",  "cmartinez@techsol.es",   deptMgmt.Id,  "Director General",         EmploymentType.FullTime, hireDate);
        var sales1   = Employee.Hire(tid, "TS-002", "Laura",    "Sánchez",   "lsanchez@techsol.es",    deptSales.Id, "Account Manager",          EmploymentType.FullTime, new DateOnly(2021, 3, 1), managerEmployeeId: director.EmployeeNumber);
        var sales2   = Employee.Hire(tid, "TS-003", "Miguel",   "Torres",    "mtorres@techsol.es",     deptSales.Id, "Sales Representative",     EmploymentType.FullTime, new DateOnly(2022, 6, 1), managerEmployeeId: director.EmployeeNumber);
        var ware1    = Employee.Hire(tid, "TS-004", "Pedro",    "López",     "plopez@techsol.es",      deptWare.Id,  "Jefe de Almacén",          EmploymentType.FullTime, new DateOnly(2020, 6, 1));
        var ware2    = Employee.Hire(tid, "TS-005", "Ana",      "García",    "agarcia@techsol.es",     deptWare.Id,  "Operaria de Almacén",      EmploymentType.FullTime, new DateOnly(2023, 1, 9), managerEmployeeId: ware1.EmployeeNumber);
        var itEng    = Employee.Hire(tid, "TS-006", "Roberto",  "Fernández", "rfernandez@techsol.es",  deptIT.Id,    "Técnico de Sistemas",      EmploymentType.FullTime, new DateOnly(2021, 9, 1));
        await hr.Employees.AddRangeAsync(director, sales1, sales2, ware1, ware2, itEng);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, director.Id, "CT-TS-001", new DateOnly(2020, 1, 15), 6_500m, "EUR"),
            Contract.Create(tid, sales1.Id,   "CT-TS-002", new DateOnly(2021, 3, 1),  2_800m, "EUR"),
            Contract.Create(tid, sales2.Id,   "CT-TS-003", new DateOnly(2022, 6, 1),  2_400m, "EUR"),
            Contract.Create(tid, ware1.Id,    "CT-TS-004", new DateOnly(2020, 6, 1),  2_200m, "EUR"),
            Contract.Create(tid, ware2.Id,    "CT-TS-005", new DateOnly(2023, 1, 9),  1_800m, "EUR"),
            Contract.Create(tid, itEng.Id,    "CT-TS-006", new DateOnly(2021, 9, 1),  3_200m, "EUR"));
        await hr.SaveChangesAsync();
    }

    // ── Nexo Consulting Group ─────────────────────────────────────────────────

    private static async Task SeedNexoAsync(IServiceProvider sp)
    {
        var tid = NexoId;

        var parties = sp.GetRequiredService<PartiesDbContext>();
        var customers = new[]
        {
            MakeOrg("Grupo Financiero Azteca",    "MX", PartyRoleType.Customer, 200_000m, "MXN", 30,  tid),
            MakeOrg("Banco Regional Norte",       "MX", PartyRoleType.Customer, 500_000m, "MXN", 45,  tid),
            MakeOrg("Seguros del Pacífico",       "MX", PartyRoleType.Customer, 150_000m, "MXN", 30,  tid),
            MakeOrg("Industrias Monterrey S.A.",  "MX", PartyRoleType.Customer, 300_000m, "MXN", 60,  tid),
            MakeOrg("Constructora Del Valle",     "MX", PartyRoleType.Customer, 120_000m, "MXN", 30,  tid),
        };
        await parties.Parties.AddRangeAsync(customers);
        await parties.SaveChangesAsync();

        var catalog = sp.GetRequiredService<CatalogDbContext>();
        var services = new[]
        {
            MakeService("SVC-CONS",  "Consultoría Estratégica",    "MX", "MXN", UnitOfMeasure.Hour,  tid),
            MakeService("SVC-AUD",   "Auditoría Financiera",       "MX", "MXN", UnitOfMeasure.Month, tid),
            MakeService("SVC-DEV",   "Desarrollo de Software",     "MX", "MXN", UnitOfMeasure.Hour,  tid),
            MakeService("SVC-TRAIN", "Capacitación Empresarial",   "MX", "MXN", UnitOfMeasure.Hour,  tid),
            MakeService("SVC-PMO",   "Gestión de Proyectos PMO",   "MX", "MXN", UnitOfMeasure.Month, tid),
        };
        await catalog.CatalogItems.AddRangeAsync(services);
        await catalog.SaveChangesAsync();

        var tax = sp.GetRequiredService<TaxDbContext>();
        await tax.TaxRates.AddRangeAsync(
            TaxRate.Create(tid, "IVA-16", "IVA México 16%",     16m, TaxApplicability.Both, "IVA estándar México"),
            TaxRate.Create(tid, "ISR-10", "Retención ISR 10%",  10m, TaxApplicability.Sales, "Retención de ISR en servicios profesionales"));
        await tax.SaveChangesAsync();

        var banking = sp.GetRequiredService<BankingDbContext>();
        var cta = BankAccount.Create(tid, "0021-0691-67-0100000000", "BBVA México", "MXN");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-60), "Saldo inicial",             500_000m, BankTransactionType.Credit, "OPEN-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-20), "Pago proyecto Azteca Q1",  180_000m, BankTransactionType.Credit, "COB-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-10), "Nómina Abril",              85_000m, BankTransactionType.Debit,  "NOM-APR");
        await banking.BankAccounts.AddAsync(cta);
        await banking.SaveChangesAsync();

        var hr = sp.GetRequiredService<HrDbContext>();
        var deptMgmt  = Department.Create(tid, "MGMT",  "Dirección");
        var deptCons  = Department.Create(tid, "CONS",  "Consultoría", costCenter: "CC-CONS");
        var deptAdmin = Department.Create(tid, "ADMIN", "Administración");
        await hr.Departments.AddRangeAsync(deptMgmt, deptCons, deptAdmin);
        await hr.SaveChangesAsync();

        var ceo      = Employee.Hire(tid, "NX-001", "Alejandro", "Vargas",    "avargas@nexo.mx",    deptMgmt.Id,  "CEO / Socio Director",    EmploymentType.FullTime, new DateOnly(2018, 4, 1));
        var cons1    = Employee.Hire(tid, "NX-002", "Valeria",   "Morales",   "vmorales@nexo.mx",   deptCons.Id,  "Consultora Senior",       EmploymentType.FullTime, new DateOnly(2019, 9, 1), managerEmployeeId: ceo.EmployeeNumber);
        var cons2    = Employee.Hire(tid, "NX-003", "Ricardo",   "Ríos",      "rrios@nexo.mx",      deptCons.Id,  "Consultor Junior",        EmploymentType.FullTime, new DateOnly(2022, 2, 1), managerEmployeeId: cons1.EmployeeNumber);
        var admin    = Employee.Hire(tid, "NX-004", "Sofía",     "Pérez",     "sperez@nexo.mx",     deptAdmin.Id, "Coordinadora Administrativa", EmploymentType.FullTime, new DateOnly(2020, 1, 15));
        var finanzas = Employee.Hire(tid, "NX-005", "Jorge",     "Castillo",  "jcastillo@nexo.mx",  deptAdmin.Id, "Analista Financiero",     EmploymentType.FullTime, new DateOnly(2021, 7, 1));
        await hr.Employees.AddRangeAsync(ceo, cons1, cons2, admin, finanzas);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, ceo.Id,      "CT-NX-001", new DateOnly(2018, 4, 1),  85_000m, "MXN"),
            Contract.Create(tid, cons1.Id,    "CT-NX-002", new DateOnly(2019, 9, 1),  45_000m, "MXN"),
            Contract.Create(tid, cons2.Id,    "CT-NX-003", new DateOnly(2022, 2, 1),  28_000m, "MXN"),
            Contract.Create(tid, admin.Id,    "CT-NX-004", new DateOnly(2020, 1, 15), 22_000m, "MXN"),
            Contract.Create(tid, finanzas.Id, "CT-NX-005", new DateOnly(2021, 7, 1),  30_000m, "MXN"));
        await hr.SaveChangesAsync();
    }

    // ── Bella Moda Retail S.A. ────────────────────────────────────────────────

    private static async Task SeedBellaModaAsync(IServiceProvider sp)
    {
        var tid = BellaModaId;

        var parties = sp.GetRequiredService<PartiesDbContext>();
        var customers = new[]
        {
            MakeOrg("Tiendas El Ángel S.A.",     "AR", PartyRoleType.Customer, 500_000m,  "ARS", 30, tid),
            MakeOrg("Centro Comercial Palermo",  "AR", PartyRoleType.Customer, 300_000m,  "ARS", 15, tid),
            MakeOrg("Boutique Norte",            "AR", PartyRoleType.Customer, 120_000m,  "ARS", 30, tid),
            MakeOrg("Fashion Online S.R.L.",     "AR", PartyRoleType.Customer, 200_000m,  "ARS", 15, tid),
        };
        var suppliers = new[]
        {
            MakeOrg("Textil Andina S.A.",              "AR", PartyRoleType.Supplier, 0m, "ARS", 30, tid),
            MakeOrg("Calzados del Sur",                "AR", PartyRoleType.Supplier, 0m, "ARS", 30, tid),
            MakeOrg("Accesorios Fashion Import S.A.",  "AR", PartyRoleType.Supplier, 0m, "ARS", 45, tid),
        };
        await parties.Parties.AddRangeAsync([..customers, ..suppliers]);
        await parties.SaveChangesAsync();

        var catalog = sp.GetRequiredService<CatalogDbContext>();
        var items = new[]
        {
            MakeProduct("VES-VER-001", "Vestido de Verano",       "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 20m),
            MakeProduct("CAM-OXF-001", "Camisa Oxford",           "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 30m),
            MakeProduct("PAN-CHI-001", "Pantalón Chino",          "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 25m),
            MakeProduct("CHA-CAS-001", "Chaqueta Casual",         "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 15m),
            MakeProduct("ZAP-CUE-001", "Zapatos Cuero",           "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 10m),
            MakeProduct("CAR-PRE-001", "Cartera Cuero Premium",   "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 5m),
            MakeProduct("CIN-CUE-001", "Cinturón Cuero",          "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 15m),
            MakeProduct("PAN-SED-001", "Pañuelo de Seda",         "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 20m),
            MakeProduct("GAF-SOL-001", "Gafas de Sol",            "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 10m),
            MakeProduct("PER-EXC-001", "Perfume Exclusivo 50ml",  "AR", "ARS", UnitOfMeasure.Each, tid, trackInventory: true, reorderPoint: 5m),
        };
        await catalog.CatalogItems.AddRangeAsync(items);
        await catalog.SaveChangesAsync();

        var inventory = sp.GetRequiredService<InventoryDbContext>();
        var wh = Warehouse.Create("WH-MAIN", "Depósito Principal Buenos Aires", "Av. Corrientes 1500, CABA");
        wh.TenantId = tid;
        await inventory.Warehouses.AddAsync(wh);
        await inventory.SaveChangesAsync();

        foreach (var item in items)
        {
            var inv = MakeInventoryItem(item.Id, wh.Id, item.SKU, tid);
            inv.Receive(40, "SEED-INITIAL");
            await inventory.InventoryItems.AddAsync(inv);
        }
        await inventory.SaveChangesAsync();

        var tax = sp.GetRequiredService<TaxDbContext>();
        await tax.TaxRates.AddRangeAsync(
            TaxRate.Create(tid, "IVA-21", "IVA 21%",  21m, TaxApplicability.Both, "IVA general Argentina"),
            TaxRate.Create(tid, "IIBB-3", "IIBB 3%",   3m, TaxApplicability.Sales, "Ingresos Brutos Buenos Aires"));
        await tax.SaveChangesAsync();

        var banking = sp.GetRequiredService<BankingDbContext>();
        var ctaArs = BankAccount.Create(tid, "0011-0009-98-0100000000", "Banco Nación Argentina", "ARS");
        ctaArs.AddTransaction(DateTime.UtcNow.AddDays(-45), "Saldo inicial",           1_200_000m, BankTransactionType.Credit, "OPEN-001");
        ctaArs.AddTransaction(DateTime.UtcNow.AddDays(-12), "Cobro Tiendas El Ángel",    480_000m, BankTransactionType.Credit, "COB-001");
        ctaArs.AddTransaction(DateTime.UtcNow.AddDays(-8),  "Pago Textil Andina",        320_000m, BankTransactionType.Debit,  "PAG-001");
        var ctaUsd = BankAccount.Create(tid, "0027-1234-98-0001000000", "Banco Galicia", "USD");
        ctaUsd.AddTransaction(DateTime.UtcNow.AddDays(-30), "Reserva dólares",           10_000m, BankTransactionType.Credit, "OPEN-002");
        await banking.BankAccounts.AddRangeAsync(ctaArs, ctaUsd);
        await banking.SaveChangesAsync();

        var hr = sp.GetRequiredService<HrDbContext>();
        var deptMgmt  = Department.Create(tid, "MGMT",  "Gerencia");
        var deptSales = Department.Create(tid, "SALES", "Ventas", costCenter: "CC-SALES");
        var deptWare  = Department.Create(tid, "WARE",  "Depósito");
        await hr.Departments.AddRangeAsync(deptMgmt, deptSales, deptWare);
        await hr.SaveChangesAsync();

        var gerente  = Employee.Hire(tid, "BM-001", "Valentina", "Ruiz",      "vruiz@bellamoda.ar",     deptMgmt.Id,  "Gerente General",  EmploymentType.FullTime, new DateOnly(2019, 3, 1));
        var vend1    = Employee.Hire(tid, "BM-002", "Camila",    "Flores",    "cflores@bellamoda.ar",   deptSales.Id, "Vendedora Senior", EmploymentType.FullTime, new DateOnly(2020, 8, 1), managerEmployeeId: gerente.EmployeeNumber);
        var vend2    = Employee.Hire(tid, "BM-003", "Lucía",     "Medina",    "lmedina@bellamoda.ar",   deptSales.Id, "Vendedora",        EmploymentType.FullTime, new DateOnly(2022, 11, 1), managerEmployeeId: vend1.EmployeeNumber);
        var repos    = Employee.Hire(tid, "BM-004", "Diego",     "Herrera",   "dherrera@bellamoda.ar",  deptWare.Id,  "Repositor",        EmploymentType.PartTime, new DateOnly(2023, 4, 1));
        await hr.Employees.AddRangeAsync(gerente, vend1, vend2, repos);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, gerente.Id, "CT-BM-001", new DateOnly(2019, 3, 1),  180_000m, "ARS"),
            Contract.Create(tid, vend1.Id,   "CT-BM-002", new DateOnly(2020, 8, 1),  120_000m, "ARS"),
            Contract.Create(tid, vend2.Id,   "CT-BM-003", new DateOnly(2022, 11, 1),  95_000m, "ARS"),
            Contract.Create(tid, repos.Id,   "CT-BM-004", new DateOnly(2023, 4, 1),   60_000m, "ARS"));
        await hr.SaveChangesAsync();
    }

    // ── La Mesa Gourmet S.R.L. ────────────────────────────────────────────────

    private static async Task SeedMesaGourmetAsync(IServiceProvider sp)
    {
        var tid = MesaGourmetId;

        var parties = sp.GetRequiredService<PartiesDbContext>();
        var customers = new[]
        {
            MakeOrg("Empresa Eventos Solaris",    "ES", PartyRoleType.Customer, 15_000m, "EUR", 15, tid),
            MakeOrg("Hotel Gran Vía Madrid",      "ES", PartyRoleType.Customer, 30_000m, "EUR", 30, tid),
            MakeOrg("Corporación Azul Catering",  "ES", PartyRoleType.Customer, 10_000m, "EUR", 15, tid),
        };
        var suppliers = new[]
        {
            MakeOrg("Mercado Central de Abastos",  "ES", PartyRoleType.Supplier, 0m, "EUR", 7,  tid),
            MakeOrg("Distribuidora Lácteos Premium","ES", PartyRoleType.Supplier, 0m, "EUR", 15, tid),
            MakeOrg("Bodega Los Viñedos",          "ES", PartyRoleType.Supplier, 0m, "EUR", 30, tid),
            MakeOrg("Carnes Selectas del Norte",   "ES", PartyRoleType.Supplier, 0m, "EUR", 7,  tid),
            MakeOrg("Cafés & Tés Especialidad",    "ES", PartyRoleType.Supplier, 0m, "EUR", 15, tid),
        };
        await parties.Parties.AddRangeAsync([..customers, ..suppliers]);
        await parties.SaveChangesAsync();

        var catalog = sp.GetRequiredService<CatalogDbContext>();
        var items = new[]
        {
            MakeService("MENU-DIA",  "Menú del Día",              "ES", "EUR", UnitOfMeasure.Each,     tid),
            MakeService("ENT-TEMP",  "Entrante de Temporada",     "ES", "EUR", UnitOfMeasure.Each,     tid),
            MakeService("POS-CHEF",  "Postre del Chef",           "ES", "EUR", UnitOfMeasure.Each,     tid),
            MakeService("MENU-DEG",  "Menú Degustación 7 Platos", "ES", "EUR", UnitOfMeasure.Each,     tid),
            MakeService("SVC-CATER", "Servicio de Catering",      "ES", "EUR", UnitOfMeasure.Hour,     tid),
            MakeProduct("VIN-SELEC", "Vino Selección de la Casa", "ES", "EUR", UnitOfMeasure.Each,     tid, trackInventory: true, reorderPoint: 12m),
            MakeProduct("CAF-ESP",   "Café de Especialidad",      "ES", "EUR", UnitOfMeasure.Kilogram, tid, trackInventory: true, reorderPoint: 2m),
            MakeProduct("CAR-TEMP",  "Carne de Temporada",        "ES", "EUR", UnitOfMeasure.Kilogram, tid, trackInventory: true, reorderPoint: 5m),
            MakeProduct("SAL-FRE",   "Salmón Fresco",             "ES", "EUR", UnitOfMeasure.Kilogram, tid, trackInventory: true, reorderPoint: 3m),
            MakeProduct("BEB-ART",   "Bebida Artesanal",          "ES", "EUR", UnitOfMeasure.Liter,    tid, trackInventory: true, reorderPoint: 5m),
            MakeProduct("PAN-REP",   "Pan y Repostería del Día",  "ES", "EUR", UnitOfMeasure.Kilogram, tid, trackInventory: true, reorderPoint: 1m),
            MakeProduct("LACT-ART",  "Lácteos Artesanales",       "ES", "EUR", UnitOfMeasure.Kilogram, tid, trackInventory: true, reorderPoint: 3m),
        };
        await catalog.CatalogItems.AddRangeAsync(items);
        await catalog.SaveChangesAsync();

        var inventory = sp.GetRequiredService<InventoryDbContext>();
        var cocina = Warehouse.Create("COCINA", "Cocina y Bodega", "Calle Mayor 12, Madrid");
        cocina.TenantId = tid;
        await inventory.Warehouses.AddAsync(cocina);
        await inventory.SaveChangesAsync();

        foreach (var item in items.Where(i => i.TrackInventory))
        {
            var inv = MakeInventoryItem(item.Id, cocina.Id, item.SKU, tid);
            inv.Receive(20, "SEED-INITIAL");
            await inventory.InventoryItems.AddAsync(inv);
        }
        await inventory.SaveChangesAsync();

        var tax = sp.GetRequiredService<TaxDbContext>();
        await tax.TaxRates.AddRangeAsync(
            TaxRate.Create(tid, "IVA-10", "IVA Reducido Hostelería 10%", 10m, TaxApplicability.Both, "Tipo reducido para alimentación y restauración"),
            TaxRate.Create(tid, "IVA-21", "IVA General Bebidas 21%",     21m, TaxApplicability.Sales, "Tipo general para bebidas alcohólicas"));
        await tax.SaveChangesAsync();

        var banking = sp.GetRequiredService<BankingDbContext>();
        var cta = BankAccount.Create(tid, "2100-0418-42-0200000000", "CaixaBank", "EUR");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-90), "Saldo inicial",              30_000m, BankTransactionType.Credit, "OPEN-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-7),  "Recaudación semana",          8_400m, BankTransactionType.Credit, "REC-WK01");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-5),  "Compra Mercado Central",      1_200m, BankTransactionType.Debit,  "COM-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-3),  "Compra Bodega Los Viñedos",     800m, BankTransactionType.Debit,  "COM-002");
        await banking.BankAccounts.AddAsync(cta);
        await banking.SaveChangesAsync();

        var hr = sp.GetRequiredService<HrDbContext>();
        var deptCocina = Department.Create(tid, "COCI", "Cocina",  costCenter: "CC-COCI");
        var deptSala   = Department.Create(tid, "SALA", "Sala y Bar");
        var deptMgmt   = Department.Create(tid, "MGMT", "Gerencia");
        await hr.Departments.AddRangeAsync(deptCocina, deptSala, deptMgmt);
        await hr.SaveChangesAsync();

        var gerente  = Employee.Hire(tid, "MG-001", "Francisco", "Delgado",  "fdelgado@mesagourmet.es",  deptMgmt.Id,   "Gerente",           EmploymentType.FullTime, new DateOnly(2017, 5, 1));
        var chef     = Employee.Hire(tid, "MG-002", "Beatriz",   "Navarro",  "bnavarro@mesagourmet.es",  deptCocina.Id, "Chef Ejecutiva",    EmploymentType.FullTime, new DateOnly(2017, 5, 1));
        var souschef = Employee.Hire(tid, "MG-003", "Iván",      "Ramos",    "iramos@mesagourmet.es",    deptCocina.Id, "Sous Chef",         EmploymentType.FullTime, new DateOnly(2019, 3, 1), managerEmployeeId: chef.EmployeeNumber);
        var cam1     = Employee.Hire(tid, "MG-004", "Elena",     "Vidal",    "evidal@mesagourmet.es",    deptSala.Id,   "Camarera Senior",   EmploymentType.FullTime, new DateOnly(2018, 9, 1));
        var cam2     = Employee.Hire(tid, "MG-005", "Pablo",     "Ortega",   "portega@mesagourmet.es",   deptSala.Id,   "Camarero",          EmploymentType.PartTime, new DateOnly(2023, 6, 1), managerEmployeeId: cam1.EmployeeNumber);
        await hr.Employees.AddRangeAsync(gerente, chef, souschef, cam1, cam2);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, gerente.Id,  "CT-MG-001", new DateOnly(2017, 5, 1),  3_200m, "EUR"),
            Contract.Create(tid, chef.Id,     "CT-MG-002", new DateOnly(2017, 5, 1),  3_800m, "EUR"),
            Contract.Create(tid, souschef.Id, "CT-MG-003", new DateOnly(2019, 3, 1),  2_400m, "EUR"),
            Contract.Create(tid, cam1.Id,     "CT-MG-004", new DateOnly(2018, 9, 1),  1_800m, "EUR"),
            Contract.Create(tid, cam2.Id,     "CT-MG-005", new DateOnly(2023, 6, 1),  1_200m, "EUR"));
        await hr.SaveChangesAsync();
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    private static Party MakeOrg(
        string name, string country, PartyRoleType roleType,
        decimal creditLimit, string currency, int paymentDays, Guid tenantId)
    {
        var role = PartyRole.Create(
            roleType,
            creditLimit > 0 ? Money.Of(creditLimit, currency) : null,
            paymentDays);

        var party = Party.Create(name, PartyType.Organization, country, role);
        party.TenantId = tenantId;
        return party;
    }

    private static CatalogItem MakeProduct(
        string sku, string name, string country, string currency,
        UnitOfMeasure uom, Guid tenantId,
        bool trackInventory = false, decimal? reorderPoint = null)
    {
        var item = CatalogItem.Create(
            sku, name, description: null, ItemType.Product,
            uom, "STANDARD", currency, country, trackInventory, reorderPoint);
        item.TenantId = tenantId;
        return item;
    }

    private static CatalogItem MakeService(
        string sku, string name, string country, string currency,
        UnitOfMeasure uom, Guid tenantId)
    {
        var item = CatalogItem.Create(
            sku, name, description: null, ItemType.Service,
            uom, "STANDARD", currency, country, trackInventory: false);
        item.TenantId = tenantId;
        return item;
    }

    private static Inventory.Domain.Inventory.InventoryItem MakeInventoryItem(
        Guid catalogItemId, Guid warehouseId, string sku, Guid tenantId)
    {
        var inv = Inventory.Domain.Inventory.InventoryItem.Create(catalogItemId, warehouseId, sku);
        inv.TenantId = tenantId;
        return inv;
    }
}
