using Api.Domain.Common;
using Api.Domain.Users;
using Api.Infrastructure.Persistence;
using BCrypt.Net;
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
    private static readonly Guid TechSolId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid NexoId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid BellaModaId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid MesaGourmetId = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static readonly Guid MercaMasId = Guid.Parse("00000000-0000-0000-0000-000000000006");

    // Each company is seeded independently — adding a new company never requires a DB reset.
    private static readonly (Guid Id, string Name, string Slug, string Country, string Currency, TenantPlan Plan,
        string Email, string Password, string AdminFirst, string AdminLast,
        Func<IServiceProvider, Task> Seeder)[] Companies =
    [
        (TechSolId,    "TechSol Distribuciones S.A.", "techsol",    "ES", "EUR", TenantPlan.Enterprise,
         "admin@techsol.es",     "TechSol123!",    "Admin", "TechSol",     sp => SeedTechSolAsync(sp)),
        (NexoId,       "Nexo Consulting Group",       "nexo",       "MX", "MXN", TenantPlan.Standard,
         "admin@nexo.mx",        "Nexo123!",       "Admin", "Nexo",        sp => SeedNexoAsync(sp)),
        (BellaModaId,  "Bella Moda Retail S.A.",      "bellamoda",  "AR", "ARS", TenantPlan.Standard,
         "admin@bellamoda.ar",   "BellaModa123!",  "Admin", "BellaModa",   sp => SeedBellaModaAsync(sp)),
        (MesaGourmetId,"La Mesa Gourmet S.R.L.",      "mesagourmet","ES", "EUR", TenantPlan.Free,
         "admin@mesagourmet.es", "MesaGourmet123!","Admin", "MesaGourmet", sp => SeedMesaGourmetAsync(sp)),
        (MercaMasId,   "MercaMás S.A.",               "mercamas",   "CR", "CRC", TenantPlan.Enterprise,
         "admin@mercamas.cr",    "MercaMas123!",   "Admin", "MercaMas",    sp => SeedMercaMasAsync(sp)),
    ];

    public static async Task SeedAsync(IServiceProvider provider, ILogger logger)
    {
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var controlPlane = sp.GetRequiredService<ControlPlaneDbContext>();
        var appDb = sp.GetRequiredService<AppDbContext>();
        var adminRole = await appDb.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");

        var seeded = 0;
        foreach (var c in Companies)
        {
            if (await controlPlane.Tenants.AnyAsync(t => t.Id == c.Id)) continue;

            var tenant = Tenant.CreateWithId(c.Id, c.Name, c.Slug, c.Country, c.Currency, c.Plan);
            await controlPlane.Tenants.AddAsync(tenant);
            await controlPlane.SaveChangesAsync();

            if (adminRole is not null && !await appDb.Users.AnyAsync(u => u.Email.Value == c.Email))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(c.Password);
                var user = User.Create(c.AdminFirst, c.AdminLast, c.Email, hash, c.Id, c.Country);
                user.AssignRole(adminRole);
                await appDb.Users.AddAsync(user);
                await appDb.SaveChangesAsync();
            }

            await c.Seeder(sp);
            logger.LogInformation("Demo company seeded: {Name}", c.Name);
            seeded++;
        }

        if (seeded > 0)
            logger.LogInformation("Demo seed completed: {Count} new companies added.", seeded);
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
        await parties.Parties.AddRangeAsync([.. customers, .. suppliers]);
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
            TaxRate.Create(tid, "IVA-21", "IVA General 21%", 21m, TaxApplicability.Both, "Tipo general del IVA español"),
            TaxRate.Create(tid, "IVA-10", "IVA Reducido 10%", 10m, TaxApplicability.Both, "Tipo reducido equipos eléctricos"),
            TaxRate.Create(tid, "IVA-0", "Exento 0%", 0m, TaxApplicability.Both, "Operaciones exentas"));
        await tax.SaveChangesAsync();

        // Banking
        var banking = sp.GetRequiredService<BankingDbContext>();
        var cta1 = BankAccount.Create(tid, "0049-2323-81-2310000000", "Banco Santander", "EUR");
        cta1.AddTransaction(DateTime.UtcNow.AddDays(-30), "Saldo inicial", 150_000m, BankTransactionType.Credit, "OPEN-001");
        cta1.AddTransaction(DateTime.UtcNow.AddDays(-15), "Cobro cliente TS-001", 18_500m, BankTransactionType.Credit, "COB-001");
        cta1.AddTransaction(DateTime.UtcNow.AddDays(-10), "Pago proveedor Samsung", 45_000m, BankTransactionType.Debit, "PAG-001");
        var cta2 = BankAccount.Create(tid, "0182-1234-90-0100000001", "BBVA", "USD");
        cta2.AddTransaction(DateTime.UtcNow.AddDays(-20), "Transferencia USD inicial", 50_000m, BankTransactionType.Credit, "OPEN-002");
        await banking.BankAccounts.AddRangeAsync(cta1, cta2);
        await banking.SaveChangesAsync();

        // HR
        var hr = sp.GetRequiredService<HrDbContext>();
        var deptMgmt = Department.Create(tid, "MGMT", "Dirección General");
        var deptSales = Department.Create(tid, "SALES", "Ventas", costCenter: "CC-SALES");
        var deptWare = Department.Create(tid, "WARE", "Almacén y Logística", costCenter: "CC-WARE");
        var deptIT = Department.Create(tid, "IT", "Tecnología", costCenter: "CC-IT");
        await hr.Departments.AddRangeAsync(deptMgmt, deptSales, deptWare, deptIT);
        await hr.SaveChangesAsync();

        var hireDate = new DateOnly(2020, 1, 15);
        var director = Employee.Hire(tid, "TS-001", "Carlos", "Martínez", "cmartinez@techsol.es", deptMgmt.Id, "Director General", EmploymentType.FullTime, hireDate);
        var sales1 = Employee.Hire(tid, "TS-002", "Laura", "Sánchez", "lsanchez@techsol.es", deptSales.Id, "Account Manager", EmploymentType.FullTime, new DateOnly(2021, 3, 1), managerEmployeeId: director.EmployeeNumber);
        var sales2 = Employee.Hire(tid, "TS-003", "Miguel", "Torres", "mtorres@techsol.es", deptSales.Id, "Sales Representative", EmploymentType.FullTime, new DateOnly(2022, 6, 1), managerEmployeeId: director.EmployeeNumber);
        var ware1 = Employee.Hire(tid, "TS-004", "Pedro", "López", "plopez@techsol.es", deptWare.Id, "Jefe de Almacén", EmploymentType.FullTime, new DateOnly(2020, 6, 1));
        var ware2 = Employee.Hire(tid, "TS-005", "Ana", "García", "agarcia@techsol.es", deptWare.Id, "Operaria de Almacén", EmploymentType.FullTime, new DateOnly(2023, 1, 9), managerEmployeeId: ware1.EmployeeNumber);
        var itEng = Employee.Hire(tid, "TS-006", "Roberto", "Fernández", "rfernandez@techsol.es", deptIT.Id, "Técnico de Sistemas", EmploymentType.FullTime, new DateOnly(2021, 9, 1));
        await hr.Employees.AddRangeAsync(director, sales1, sales2, ware1, ware2, itEng);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, director.Id, "CT-TS-001", new DateOnly(2020, 1, 15), 6_500m, "EUR"),
            Contract.Create(tid, sales1.Id, "CT-TS-002", new DateOnly(2021, 3, 1), 2_800m, "EUR"),
            Contract.Create(tid, sales2.Id, "CT-TS-003", new DateOnly(2022, 6, 1), 2_400m, "EUR"),
            Contract.Create(tid, ware1.Id, "CT-TS-004", new DateOnly(2020, 6, 1), 2_200m, "EUR"),
            Contract.Create(tid, ware2.Id, "CT-TS-005", new DateOnly(2023, 1, 9), 1_800m, "EUR"),
            Contract.Create(tid, itEng.Id, "CT-TS-006", new DateOnly(2021, 9, 1), 3_200m, "EUR"));
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
            TaxRate.Create(tid, "IVA-16", "IVA México 16%", 16m, TaxApplicability.Both, "IVA estándar México"),
            TaxRate.Create(tid, "ISR-10", "Retención ISR 10%", 10m, TaxApplicability.Sales, "Retención de ISR en servicios profesionales"));
        await tax.SaveChangesAsync();

        var banking = sp.GetRequiredService<BankingDbContext>();
        var cta = BankAccount.Create(tid, "0021-0691-67-0100000000", "BBVA México", "MXN");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-60), "Saldo inicial", 500_000m, BankTransactionType.Credit, "OPEN-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-20), "Pago proyecto Azteca Q1", 180_000m, BankTransactionType.Credit, "COB-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-10), "Nómina Abril", 85_000m, BankTransactionType.Debit, "NOM-APR");
        await banking.BankAccounts.AddAsync(cta);
        await banking.SaveChangesAsync();

        var hr = sp.GetRequiredService<HrDbContext>();
        var deptMgmt = Department.Create(tid, "MGMT", "Dirección");
        var deptCons = Department.Create(tid, "CONS", "Consultoría", costCenter: "CC-CONS");
        var deptAdmin = Department.Create(tid, "ADMIN", "Administración");
        await hr.Departments.AddRangeAsync(deptMgmt, deptCons, deptAdmin);
        await hr.SaveChangesAsync();

        var ceo = Employee.Hire(tid, "NX-001", "Alejandro", "Vargas", "avargas@nexo.mx", deptMgmt.Id, "CEO / Socio Director", EmploymentType.FullTime, new DateOnly(2018, 4, 1));
        var cons1 = Employee.Hire(tid, "NX-002", "Valeria", "Morales", "vmorales@nexo.mx", deptCons.Id, "Consultora Senior", EmploymentType.FullTime, new DateOnly(2019, 9, 1), managerEmployeeId: ceo.EmployeeNumber);
        var cons2 = Employee.Hire(tid, "NX-003", "Ricardo", "Ríos", "rrios@nexo.mx", deptCons.Id, "Consultor Junior", EmploymentType.FullTime, new DateOnly(2022, 2, 1), managerEmployeeId: cons1.EmployeeNumber);
        var admin = Employee.Hire(tid, "NX-004", "Sofía", "Pérez", "sperez@nexo.mx", deptAdmin.Id, "Coordinadora Administrativa", EmploymentType.FullTime, new DateOnly(2020, 1, 15));
        var finanzas = Employee.Hire(tid, "NX-005", "Jorge", "Castillo", "jcastillo@nexo.mx", deptAdmin.Id, "Analista Financiero", EmploymentType.FullTime, new DateOnly(2021, 7, 1));
        await hr.Employees.AddRangeAsync(ceo, cons1, cons2, admin, finanzas);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, ceo.Id, "CT-NX-001", new DateOnly(2018, 4, 1), 85_000m, "MXN"),
            Contract.Create(tid, cons1.Id, "CT-NX-002", new DateOnly(2019, 9, 1), 45_000m, "MXN"),
            Contract.Create(tid, cons2.Id, "CT-NX-003", new DateOnly(2022, 2, 1), 28_000m, "MXN"),
            Contract.Create(tid, admin.Id, "CT-NX-004", new DateOnly(2020, 1, 15), 22_000m, "MXN"),
            Contract.Create(tid, finanzas.Id, "CT-NX-005", new DateOnly(2021, 7, 1), 30_000m, "MXN"));
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
        await parties.Parties.AddRangeAsync([.. customers, .. suppliers]);
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
            TaxRate.Create(tid, "IVA-21", "IVA 21%", 21m, TaxApplicability.Both, "IVA general Argentina"),
            TaxRate.Create(tid, "IIBB-3", "IIBB 3%", 3m, TaxApplicability.Sales, "Ingresos Brutos Buenos Aires"));
        await tax.SaveChangesAsync();

        var banking = sp.GetRequiredService<BankingDbContext>();
        var ctaArs = BankAccount.Create(tid, "0011-0009-98-0100000000", "Banco Nación Argentina", "ARS");
        ctaArs.AddTransaction(DateTime.UtcNow.AddDays(-45), "Saldo inicial", 1_200_000m, BankTransactionType.Credit, "OPEN-001");
        ctaArs.AddTransaction(DateTime.UtcNow.AddDays(-12), "Cobro Tiendas El Ángel", 480_000m, BankTransactionType.Credit, "COB-001");
        ctaArs.AddTransaction(DateTime.UtcNow.AddDays(-8), "Pago Textil Andina", 320_000m, BankTransactionType.Debit, "PAG-001");
        var ctaUsd = BankAccount.Create(tid, "0027-1234-98-0001000000", "Banco Galicia", "USD");
        ctaUsd.AddTransaction(DateTime.UtcNow.AddDays(-30), "Reserva dólares", 10_000m, BankTransactionType.Credit, "OPEN-002");
        await banking.BankAccounts.AddRangeAsync(ctaArs, ctaUsd);
        await banking.SaveChangesAsync();

        var hr = sp.GetRequiredService<HrDbContext>();
        var deptMgmt = Department.Create(tid, "MGMT", "Gerencia");
        var deptSales = Department.Create(tid, "SALES", "Ventas", costCenter: "CC-SALES");
        var deptWare = Department.Create(tid, "WARE", "Depósito");
        await hr.Departments.AddRangeAsync(deptMgmt, deptSales, deptWare);
        await hr.SaveChangesAsync();

        var gerente = Employee.Hire(tid, "BM-001", "Valentina", "Ruiz", "vruiz@bellamoda.ar", deptMgmt.Id, "Gerente General", EmploymentType.FullTime, new DateOnly(2019, 3, 1));
        var vend1 = Employee.Hire(tid, "BM-002", "Camila", "Flores", "cflores@bellamoda.ar", deptSales.Id, "Vendedora Senior", EmploymentType.FullTime, new DateOnly(2020, 8, 1), managerEmployeeId: gerente.EmployeeNumber);
        var vend2 = Employee.Hire(tid, "BM-003", "Lucía", "Medina", "lmedina@bellamoda.ar", deptSales.Id, "Vendedora", EmploymentType.FullTime, new DateOnly(2022, 11, 1), managerEmployeeId: vend1.EmployeeNumber);
        var repos = Employee.Hire(tid, "BM-004", "Diego", "Herrera", "dherrera@bellamoda.ar", deptWare.Id, "Repositor", EmploymentType.PartTime, new DateOnly(2023, 4, 1));
        await hr.Employees.AddRangeAsync(gerente, vend1, vend2, repos);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, gerente.Id, "CT-BM-001", new DateOnly(2019, 3, 1), 180_000m, "ARS"),
            Contract.Create(tid, vend1.Id, "CT-BM-002", new DateOnly(2020, 8, 1), 120_000m, "ARS"),
            Contract.Create(tid, vend2.Id, "CT-BM-003", new DateOnly(2022, 11, 1), 95_000m, "ARS"),
            Contract.Create(tid, repos.Id, "CT-BM-004", new DateOnly(2023, 4, 1), 60_000m, "ARS"));
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
        await parties.Parties.AddRangeAsync([.. customers, .. suppliers]);
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
            TaxRate.Create(tid, "IVA-21", "IVA General Bebidas 21%", 21m, TaxApplicability.Sales, "Tipo general para bebidas alcohólicas"));
        await tax.SaveChangesAsync();

        var banking = sp.GetRequiredService<BankingDbContext>();
        var cta = BankAccount.Create(tid, "2100-0418-42-0200000000", "CaixaBank", "EUR");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-90), "Saldo inicial", 30_000m, BankTransactionType.Credit, "OPEN-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-7), "Recaudación semana", 8_400m, BankTransactionType.Credit, "REC-WK01");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-5), "Compra Mercado Central", 1_200m, BankTransactionType.Debit, "COM-001");
        cta.AddTransaction(DateTime.UtcNow.AddDays(-3), "Compra Bodega Los Viñedos", 800m, BankTransactionType.Debit, "COM-002");
        await banking.BankAccounts.AddAsync(cta);
        await banking.SaveChangesAsync();

        var hr = sp.GetRequiredService<HrDbContext>();
        var deptCocina = Department.Create(tid, "COCI", "Cocina", costCenter: "CC-COCI");
        var deptSala = Department.Create(tid, "SALA", "Sala y Bar");
        var deptMgmt = Department.Create(tid, "MGMT", "Gerencia");
        await hr.Departments.AddRangeAsync(deptCocina, deptSala, deptMgmt);
        await hr.SaveChangesAsync();

        var gerente = Employee.Hire(tid, "MG-001", "Francisco", "Delgado", "fdelgado@mesagourmet.es", deptMgmt.Id, "Gerente", EmploymentType.FullTime, new DateOnly(2017, 5, 1));
        var chef = Employee.Hire(tid, "MG-002", "Beatriz", "Navarro", "bnavarro@mesagourmet.es", deptCocina.Id, "Chef Ejecutiva", EmploymentType.FullTime, new DateOnly(2017, 5, 1));
        var souschef = Employee.Hire(tid, "MG-003", "Iván", "Ramos", "iramos@mesagourmet.es", deptCocina.Id, "Sous Chef", EmploymentType.FullTime, new DateOnly(2019, 3, 1), managerEmployeeId: chef.EmployeeNumber);
        var cam1 = Employee.Hire(tid, "MG-004", "Elena", "Vidal", "evidal@mesagourmet.es", deptSala.Id, "Camarera Senior", EmploymentType.FullTime, new DateOnly(2018, 9, 1));
        var cam2 = Employee.Hire(tid, "MG-005", "Pablo", "Ortega", "portega@mesagourmet.es", deptSala.Id, "Camarero", EmploymentType.PartTime, new DateOnly(2023, 6, 1), managerEmployeeId: cam1.EmployeeNumber);
        await hr.Employees.AddRangeAsync(gerente, chef, souschef, cam1, cam2);
        await hr.SaveChangesAsync();

        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, gerente.Id, "CT-MG-001", new DateOnly(2017, 5, 1), 3_200m, "EUR"),
            Contract.Create(tid, chef.Id, "CT-MG-002", new DateOnly(2017, 5, 1), 3_800m, "EUR"),
            Contract.Create(tid, souschef.Id, "CT-MG-003", new DateOnly(2019, 3, 1), 2_400m, "EUR"),
            Contract.Create(tid, cam1.Id, "CT-MG-004", new DateOnly(2018, 9, 1), 1_800m, "EUR"),
            Contract.Create(tid, cam2.Id, "CT-MG-005", new DateOnly(2023, 6, 1), 1_200m, "EUR"));
        await hr.SaveChangesAsync();
    }

    // ── MercaMás S.A. (Costa Rica) ───────────────────────────────────────────

    private static async Task SeedMercaMasAsync(IServiceProvider sp)
    {
        var tid = MercaMasId;

        // Parties — mix de mayoristas B2B y proveedores nacionales/importadores
        var parties = sp.GetRequiredService<PartiesDbContext>();
        var customers = new[]
        {
            MakeOrg("Hoteles Playa Dorada S.A.",       "CR", PartyRoleType.Customer,  8_000_000m, "CRC", 30, tid),
            MakeOrg("Corporación Universitaria CR",     "CR", PartyRoleType.Customer,  5_000_000m, "CRC", 30, tid),
            MakeOrg("Municipalidad de San José",        "CR", PartyRoleType.Customer, 15_000_000m, "CRC", 60, tid),
            MakeOrg("Suplidora Escolar Nacional S.A.", "CR", PartyRoleType.Customer,  6_000_000m, "CRC", 30, tid),
            MakeOrg("Club de Mayoreo del Pacífico",    "CR", PartyRoleType.Customer,  4_000_000m, "CRC", 15, tid),
        };
        var suppliers = new[]
        {
            MakeOrg("Distribuidora Nacional de Alimentos S.A.", "CR", PartyRoleType.Supplier, 0m, "CRC", 15, tid),
            MakeOrg("Importadora de Electrodomésticos CR",      "CR", PartyRoleType.Supplier, 0m, "CRC", 30, tid),
            MakeOrg("Textiles y Uniformes Import S.A.",         "CR", PartyRoleType.Supplier, 0m, "CRC", 30, tid),
            MakeOrg("Juguetería y Temporada Import CR",         "CR", PartyRoleType.Supplier, 0m, "CRC", 45, tid),
            MakeOrg("Limpieza y Cuidado del Hogar S.A.",        "CR", PartyRoleType.Supplier, 0m, "CRC", 15, tid),
            MakeOrg("Café Tarrazú Exportaciones",               "CR", PartyRoleType.Supplier, 0m, "CRC", 15, tid),
        };
        await parties.Parties.AddRangeAsync([.. customers, .. suppliers]);
        await parties.SaveChangesAsync();

        // Catalog — artículos varios al estilo Walmart/Más x Menos
        var catalog = sp.GetRequiredService<CatalogDbContext>();
        var items = new[]
        {
            // Alimentación básica (canasta)
            MakeProduct("ALI-ARR-5K",  "Arroz grano largo 5 kg",           "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 50m),
            MakeProduct("ALI-FRJ-1K",  "Frijoles negros 1 kg",             "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 50m),
            MakeProduct("ALI-ACE-1L",  "Aceite vegetal 1 L",               "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 40m),
            MakeProduct("ALI-CAF-500", "Café molido Tarrazú 500 g",        "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 30m),
            MakeProduct("ALI-SUC-SAL", "Salsa Lizano 350 ml (icónica CR)", "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 40m),
            MakeProduct("ALI-LEY-1L",  "Leche UHT 1 L",                   "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 60m),

            // Electrodomésticos y tecnología
            MakeProduct("ELE-TV-43",   "Televisor LED 43\"",               "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 5m),
            MakeProduct("ELE-LIC-10",  "Licuadora 10 velocidades",         "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 8m),
            MakeProduct("ELE-MIC-20L", "Microondas 20 L digital",          "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 5m),
            MakeProduct("ELE-VEN-PED", "Ventilador de pedestal 3 velocidades","CR","CRC",UnitOfMeasure.Each,   tid, trackInventory: true, reorderPoint: 8m),

            // Limpieza del hogar
            MakeProduct("LIM-DET-3L",  "Detergente líquido 3 L",          "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 40m),
            MakeProduct("LIM-PAP-12",  "Papel higiénico 12 rollos",        "CR", "CRC", UnitOfMeasure.Box,     tid, trackInventory: true, reorderPoint: 30m),
            MakeProduct("LIM-DES-1L",  "Desinfectante multiusos 1 L",      "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 30m),
            MakeProduct("LIM-ESC-JUI", "Escoba y recogedor plástico",      "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 15m),

            // Ropa y uniformes (muy demandado en CR para inicio de clases)
            MakeProduct("ROP-UNI-ESC", "Uniforme escolar completo",        "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 20m),
            MakeProduct("ROP-DEP-SET", "Set deportivo adulto",             "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 15m),
            MakeProduct("ROP-CAL-DEP", "Calzado deportivo",                "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 10m),

            // Decoración y artículos de temporada
            MakeProduct("DEC-NAV-SET", "Set de decoración navideña",       "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 20m),
            MakeProduct("DEC-ART-HOG", "Adornos y figuras decorativas",    "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 15m),
            MakeProduct("DEC-LUC-LED", "Luces LED decorativas 5 m",        "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 20m),

            // Juguetes y entretenimiento
            MakeProduct("JUG-SET-BAS", "Set de juguetes surtidos (caja)",  "CR", "CRC", UnitOfMeasure.Box,     tid, trackInventory: true, reorderPoint: 10m),
            MakeProduct("JUG-BIC-NIN", "Bicicleta para niño rodada 20",    "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 5m),

            // Jardín y ferretería básica
            MakeProduct("JAR-MAN-GUA", "Manguera de jardín 15 m",          "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 8m),
            MakeProduct("JAR-FER-KIT", "Kit herramientas básico jardín",   "CR", "CRC", UnitOfMeasure.Each,    tid, trackInventory: true, reorderPoint: 8m),
        };
        await catalog.CatalogItems.AddRangeAsync(items);
        await catalog.SaveChangesAsync();

        // Almacenes — centro de distribución + bodega regional
        var inventory = sp.GetRequiredService<InventoryDbContext>();
        var whSJ = Warehouse.Create("CD-SJ", "Centro de Distribución San José", "Ruta 27, La Uruca, San José");
        whSJ.TenantId = tid;
        var whCart = Warehouse.Create("BOD-CART", "Bodega Regional Cartago", "Zona Industrial Cartago");
        whCart.TenantId = tid;
        await inventory.Warehouses.AddRangeAsync(whSJ, whCart);
        await inventory.SaveChangesAsync();

        foreach (var item in items)
        {
            var invSJ = MakeInventoryItem(item.Id, whSJ.Id, item.SKU, tid);
            invSJ.Receive(80, "SEED-INITIAL");
            var invCart = MakeInventoryItem(item.Id, whCart.Id, item.SKU, tid);
            invCart.Receive(40, "SEED-INITIAL");
            await inventory.InventoryItems.AddRangeAsync(invSJ, invCart);
        }
        await inventory.SaveChangesAsync();

        // Impuestos — CR tiene IVA del 13% (Ley 9635)
        var tax = sp.GetRequiredService<TaxDbContext>();
        await tax.TaxRates.AddRangeAsync(
            TaxRate.Create(tid, "IVA-13", "IVA General 13%", 13m, TaxApplicability.Both, "Tipo general Costa Rica (Ley 9635)"),
            TaxRate.Create(tid, "IVA-4", "IVA Servicios Básicos 4%", 4m, TaxApplicability.Both, "Electricidad, agua, telefonía residencial"),
            TaxRate.Create(tid, "IVA-2", "IVA Canasta Básica 2%", 2m, TaxApplicability.Both, "Productos de canasta básica alimentaria"),
            TaxRate.Create(tid, "IVA-0", "Exento 0%", 0m, TaxApplicability.Both, "Medicamentos, libros, exportaciones"),
            TaxRate.Create(tid, "IMPO-15", "Impuesto Selectivo Consumo 15%", 15m, TaxApplicability.Purchase, "Artículos de lujo e importación"));
        await tax.SaveChangesAsync();

        // Cuentas bancarias — colones y dólares (muy común en CR)
        var banking = sp.GetRequiredService<BankingDbContext>();
        var ctaCRC = BankAccount.Create(tid, "15201-1-001001234-0", "Banco Nacional de Costa Rica", "CRC",
            iban: "CR21015201001001234567", swift: "BNCRCRSJ");
        ctaCRC.AddTransaction(DateTime.UtcNow.AddDays(-60), "Saldo inicial", 80_000_000m, BankTransactionType.Credit, "OPEN-001");
        ctaCRC.AddTransaction(DateTime.UtcNow.AddDays(-15), "Ventas semana 18 — TPV", 6_250_000m, BankTransactionType.Credit, "TPV-W18");
        ctaCRC.AddTransaction(DateTime.UtcNow.AddDays(-14), "Pago Dist. Nacional Alimentos", 3_800_000m, BankTransactionType.Debit, "PAG-001");
        ctaCRC.AddTransaction(DateTime.UtcNow.AddDays(-7), "Ventas semana 19 — TPV", 7_100_000m, BankTransactionType.Credit, "TPV-W19");
        ctaCRC.AddTransaction(DateTime.UtcNow.AddDays(-6), "Nómina quincenal", 4_200_000m, BankTransactionType.Debit, "NOM-Q1");
        ctaCRC.AddTransaction(DateTime.UtcNow.AddDays(-3), "Pago Importadora Electrodomésticos", 5_500_000m, BankTransactionType.Debit, "PAG-002");

        var ctaUSD = BankAccount.Create(tid, "15201-1-002001234-0", "BAC San José", "USD",
            iban: "CR05015201002001234567", swift: "BSCHCRSJ");
        ctaUSD.AddTransaction(DateTime.UtcNow.AddDays(-45), "Reserva dólares — importaciones", 40_000m, BankTransactionType.Credit, "OPEN-002");
        ctaUSD.AddTransaction(DateTime.UtcNow.AddDays(-10), "Pago importación juguetes", 12_500m, BankTransactionType.Debit, "IMP-001");

        await banking.BankAccounts.AddRangeAsync(ctaCRC, ctaUSD);
        await banking.SaveChangesAsync();

        // RRHH — estructura típica de supermercado / tienda de descuento
        var hr = sp.GetRequiredService<HrDbContext>();
        var deptGerencia = Department.Create(tid, "GER", "Gerencia General");
        var deptVentas = Department.Create(tid, "VEN", "Ventas y Atención al Cliente", costCenter: "CC-VEN");
        var deptBodega = Department.Create(tid, "BOD", "Bodega y Logística", costCenter: "CC-BOD");
        var deptCompras = Department.Create(tid, "COMP", "Compras y Proveeduría", costCenter: "CC-COMP");
        var deptAdmin = Department.Create(tid, "ADM", "Administración y Finanzas");
        await hr.Departments.AddRangeAsync(deptGerencia, deptVentas, deptBodega, deptCompras, deptAdmin);
        await hr.SaveChangesAsync();

        var gerente = Employee.Hire(tid, "MM-001", "Mauricio", "Quesada", "mquesada@mercamas.cr", deptGerencia.Id, "Gerente General", EmploymentType.FullTime, new DateOnly(2015, 3, 1));
        var jVentas = Employee.Hire(tid, "MM-002", "Karina", "Solano", "ksolano@mercamas.cr", deptVentas.Id, "Jefe de Ventas", EmploymentType.FullTime, new DateOnly(2017, 8, 1), managerEmployeeId: gerente.EmployeeNumber);
        var cajera1 = Employee.Hire(tid, "MM-003", "Yuliana", "Montero", "ymontero@mercamas.cr", deptVentas.Id, "Cajera Senior", EmploymentType.FullTime, new DateOnly(2019, 1, 15), managerEmployeeId: jVentas.EmployeeNumber);
        var cajera2 = Employee.Hire(tid, "MM-004", "Daniela", "Corrales", "dcorrales@mercamas.cr", deptVentas.Id, "Cajera", EmploymentType.PartTime, new DateOnly(2022, 6, 1), managerEmployeeId: jVentas.EmployeeNumber);
        var bodeguero = Employee.Hire(tid, "MM-005", "Josué", "Araya", "jaraya@mercamas.cr", deptBodega.Id, "Jefe de Bodega", EmploymentType.FullTime, new DateOnly(2018, 4, 1));
        var repo1 = Employee.Hire(tid, "MM-006", "Bryan", "Vargas", "bvargas@mercamas.cr", deptBodega.Id, "Repositor", EmploymentType.FullTime, new DateOnly(2021, 9, 1), managerEmployeeId: bodeguero.EmployeeNumber);
        var jCompras = Employee.Hire(tid, "MM-007", "Natalia", "Jiménez", "njimenez@mercamas.cr", deptCompras.Id, "Encargada de Compras", EmploymentType.FullTime, new DateOnly(2016, 7, 1), managerEmployeeId: gerente.EmployeeNumber);
        var contadora = Employee.Hire(tid, "MM-008", "Patricia", "Umaña", "pumana@mercamas.cr", deptAdmin.Id, "Contadora", EmploymentType.FullTime, new DateOnly(2015, 3, 1));
        await hr.Employees.AddRangeAsync(gerente, jVentas, cajera1, cajera2, bodeguero, repo1, jCompras, contadora);
        await hr.SaveChangesAsync();

        // Contratos — salarios en CRC (mínimos legales CR ~380k, promedio mercado 600k–1.8M)
        await hr.Contracts.AddRangeAsync(
            Contract.Create(tid, gerente.Id, "CT-MM-001", new DateOnly(2015, 3, 1), 1_800_000m, "CRC", notes: "Salario gerencial incluye bonos trimestrales"),
            Contract.Create(tid, jVentas.Id, "CT-MM-002", new DateOnly(2017, 8, 1), 1_100_000m, "CRC"),
            Contract.Create(tid, cajera1.Id, "CT-MM-003", new DateOnly(2019, 1, 15), 650_000m, "CRC"),
            Contract.Create(tid, cajera2.Id, "CT-MM-004", new DateOnly(2022, 6, 1), 420_000m, "CRC", notes: "Medio tiempo — 20h semanales"),
            Contract.Create(tid, bodeguero.Id, "CT-MM-005", new DateOnly(2018, 4, 1), 850_000m, "CRC"),
            Contract.Create(tid, repo1.Id, "CT-MM-006", new DateOnly(2021, 9, 1), 580_000m, "CRC"),
            Contract.Create(tid, jCompras.Id, "CT-MM-007", new DateOnly(2016, 7, 1), 1_050_000m, "CRC"),
            Contract.Create(tid, contadora.Id, "CT-MM-008", new DateOnly(2015, 3, 1), 1_200_000m, "CRC"));
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
        // Create a fresh UoM instance per item so EF Core never shares the same CLR object
        // across multiple owned-entity entries in the same DbContext scope.
        var item = CatalogItem.Create(
            sku, name, description: null, ItemType.Product,
            UnitOfMeasure.Create(uom.Code), "STANDARD", currency, country, trackInventory, reorderPoint);
        item.TenantId = tenantId;
        return item;
    }

    private static CatalogItem MakeService(
        string sku, string name, string country, string currency,
        UnitOfMeasure uom, Guid tenantId)
    {
        var item = CatalogItem.Create(
            sku, name, description: null, ItemType.Service,
            UnitOfMeasure.Create(uom.Code), "STANDARD", currency, country, trackInventory: false);
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
