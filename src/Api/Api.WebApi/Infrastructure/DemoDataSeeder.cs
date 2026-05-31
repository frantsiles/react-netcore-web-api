using Accounting.Domain.Accounts;
using FiscalCR.Domain.TenantConfig;
using FiscalCR.Infrastructure.Persistence;
using Accounting.Domain.JournalEntries;
using Accounting.Infrastructure.Persistence;
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
using Invoicing.Domain.Invoices;
using Invoicing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Parties.Domain.Parties;
using Parties.Domain.ValueObjects;
using Parties.Infrastructure.Persistence;
using Purchasing.Domain.PurchaseOrders;
using Purchasing.Infrastructure.Persistence;
using Sales.Domain.Orders;
using Sales.Domain.Quotes;
using Sales.Infrastructure.Persistence;
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

        await PatchInvoicesAsync(sp, logger);
    }

    // Idempotent invoice patch — runs even for existing tenants that predate invoice seeding.
    private static async Task PatchInvoicesAsync(IServiceProvider sp, ILogger logger)
    {
        var invoicing = sp.GetRequiredService<InvoicingDbContext>();
        var salesDb = sp.GetRequiredService<SalesDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var patched = 0;

        foreach (var (tenantId, prefix, currency) in new (Guid, string, string)[]
        {
            (TechSolId,     "FAC-TS", "EUR"),
            (NexoId,        "FAC-NX", "MXN"),
            (BellaModaId,   "FAC-BM", "ARS"),
            (MesaGourmetId, "FAC-MG", "EUR"),
            (MercaMasId,    "FAC-MM", "CRC"),
        })
        {
            if (await invoicing.Invoices.AnyAsync(i => i.TenantId == tenantId)) continue;

            var orders = await salesDb.SalesOrders
                .Where(o => o.TenantId == tenantId)
                .OrderBy(o => o.OrderNumber)
                .ToListAsync();

            if (orders.Count == 0) continue;

            var num = 1;
            foreach (var order in orders.Take(4))
            {
                var issueOffset = -20 + (num * 5);
                var dueOffset = issueOffset + 30;
                var inv = Invoice.Create(
                    $"{prefix}-{num:000}", order.CustomerId, currency,
                    today.AddDays(issueOffset), today.AddDays(dueOffset), order.Id);
                inv.TenantId = tenantId;
                foreach (var line in order.Lines)
                    inv.AddLine(InvoiceLine.Create(
                        line.ItemName, line.Quantity,
                        Money.Of(line.UnitPrice.Amount, currency),
                        line.DiscountPercent, line.CatalogItemId, line.SKU));
                inv.Issue();
                if (num == 1)
                    inv.RecordPayment(inv.TotalAmount, today.AddDays(issueOffset + 10), $"SEED-PAY-{num}");
                await invoicing.Invoices.AddAsync(inv);
                num++;
            }
            await invoicing.SaveChangesAsync();
            patched++;
        }

        if (patched > 0)
            logger.LogInformation("Invoice patch: {Count} tenant(s) updated with demo invoices.", patched);
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

        // Sales — quotes + orders
        var sales = sp.GetRequiredService<SalesDbContext>();

        var q1 = Quote.Create("COT-TS-001", customers[0].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), "EUR", "ES");
        q1.TenantId = tid;
        q1.AddLine(QuoteLine.Create(items[0].Id, items[0].SKU, items[0].Name, 3, Money.Of(1_200m, "EUR")));
        q1.AddLine(QuoteLine.Create(items[1].Id, items[1].SKU, items[1].Name, 2, Money.Of(450m, "EUR")));

        var q2 = Quote.Create("COT-TS-002", customers[1].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)), "EUR", "ES");
        q2.TenantId = tid;
        q2.AddLine(QuoteLine.Create(items[2].Id, items[2].SKU, items[2].Name, 10, Money.Of(89m, "EUR")));
        q2.AddLine(QuoteLine.Create(items[3].Id, items[3].SKU, items[3].Name, 10, Money.Of(35m, "EUR")));
        q2.Send();
        q2.Accept();
        q2.MarkConvertedToOrder();

        var q3 = Quote.Create("COT-TS-003", customers[2].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)), "EUR", "ES");
        q3.TenantId = tid;
        q3.AddLine(QuoteLine.Create(items[5].Id, items[5].SKU, items[5].Name, 5, Money.Of(75m, "EUR")));
        q3.Send();

        await sales.Quotes.AddRangeAsync(q1, q2, q3);

        var so1 = SalesOrder.Create("OV-TS-001", customers[1].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), "EUR", "ES",
            requestedDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            originQuoteId: q2.Id);
        so1.TenantId = tid;
        so1.AddLine(SalesOrderLine.Create(items[2].Id, items[2].SKU, items[2].Name, 10, Money.Of(89m, "EUR")));
        so1.AddLine(SalesOrderLine.Create(items[3].Id, items[3].SKU, items[3].Name, 10, Money.Of(35m, "EUR")));
        so1.Confirm();

        var so2 = SalesOrder.Create("OV-TS-002", customers[3].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), "EUR", "ES");
        so2.TenantId = tid;
        so2.AddLine(SalesOrderLine.Create(items[0].Id, items[0].SKU, items[0].Name, 2, Money.Of(1_200m, "EUR")));
        so2.Confirm();

        await sales.SalesOrders.AddRangeAsync(so1, so2);
        await sales.SaveChangesAsync();

        // Purchasing — purchase orders
        var purchasing = sp.GetRequiredService<PurchasingDbContext>();

        var po1 = PurchaseOrder.Create(tid, "OC-TS-001", suppliers[0].Id, "EUR", "ES",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(14));
        po1.AddLine(items[0].Id, items[0].SKU, items[0].Name, 20, Money.Of(800m, "EUR"));
        po1.AddLine(items[1].Id, items[1].SKU, items[1].Name, 15, Money.Of(280m, "EUR"));
        po1.Send();
        po1.Confirm();

        var po2 = PurchaseOrder.Create(tid, "OC-TS-002", suppliers[2].Id, "EUR", "ES",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(7));
        po2.AddLine(items[2].Id, items[2].SKU, items[2].Name, 30, Money.Of(55m, "EUR"));
        po2.AddLine(items[3].Id, items[3].SKU, items[3].Name, 30, Money.Of(22m, "EUR"));
        po2.Send();

        await purchasing.PurchaseOrders.AddRangeAsync(po1, po2);
        await purchasing.SaveChangesAsync();

        // Accounting — chart of accounts + opening entries
        var accounting = sp.GetRequiredService<AccountingDbContext>();
        var acctBank   = Account.Create(tid, "1100", "Banco Santander",          AccountType.Asset,     "EUR");
        var acctAR     = Account.Create(tid, "1200", "Cuentas por Cobrar",       AccountType.Asset,     "EUR");
        var acctInv    = Account.Create(tid, "1300", "Inventario",               AccountType.Asset,     "EUR");
        var acctAP     = Account.Create(tid, "2100", "Cuentas por Pagar",        AccountType.Liability, "EUR");
        var acctCap    = Account.Create(tid, "3000", "Capital Social",           AccountType.Equity,    "EUR");
        var acctRev    = Account.Create(tid, "4000", "Ventas",                   AccountType.Revenue,   "EUR");
        var acctCOGS   = Account.Create(tid, "5000", "Costo de Ventas",          AccountType.Expense,   "EUR");
        var acctSalary = Account.Create(tid, "6100", "Gastos de Personal",       AccountType.Expense,   "EUR");
        var acctRent   = Account.Create(tid, "6200", "Alquiler y Arrendamiento", AccountType.Expense,   "EUR");
        await accounting.Accounts.AddRangeAsync(acctBank, acctAR, acctInv, acctAP, acctCap, acctRev, acctCOGS, acctSalary, acctRent);
        await accounting.SaveChangesAsync();

        var je1 = JournalEntry.Create(tid, "JE-TS-001",
            DateTime.UtcNow.AddDays(-30), "Saldo inicial — constitución de capital");
        je1.AddLine(acctBank.Id, acctBank.AccountNumber, acctBank.Name, EntrySide.Debit, 150_000m);
        je1.AddLine(acctCap.Id, acctCap.AccountNumber, acctCap.Name, EntrySide.Credit, 150_000m);
        var je1Lines = je1.Post();
        foreach (var l in je1Lines)
        {
            if (l.AccountId == acctBank.Id) acctBank.ApplyDebit(l.Amount);
            else acctCap.ApplyCredit(l.Amount);
        }

        var je2 = JournalEntry.Create(tid, "JE-TS-002",
            DateTime.UtcNow.AddDays(-10), "Venta OV-TS-001 — Monitor y Teclado");
        je2.AddLine(acctAR.Id,  acctAR.AccountNumber,  acctAR.Name,  EntrySide.Debit,  1_240m);
        je2.AddLine(acctRev.Id, acctRev.AccountNumber, acctRev.Name, EntrySide.Credit, 1_240m);
        var je2Lines = je2.Post();
        foreach (var l in je2Lines)
        {
            if (l.AccountId == acctAR.Id) acctAR.ApplyDebit(l.Amount);
            else acctRev.ApplyCredit(l.Amount);
        }

        var je3 = JournalEntry.Create(tid, "JE-TS-003",
            DateTime.UtcNow.AddDays(-5), "Venta OV-TS-002 — Laptops");
        je3.AddLine(acctAR.Id,  acctAR.AccountNumber,  acctAR.Name,  EntrySide.Debit,  2_400m);
        je3.AddLine(acctRev.Id, acctRev.AccountNumber, acctRev.Name, EntrySide.Credit, 2_400m);
        var je3Lines = je3.Post();
        foreach (var l in je3Lines)
        {
            if (l.AccountId == acctAR.Id) acctAR.ApplyDebit(l.Amount);
            else acctRev.ApplyCredit(l.Amount);
        }

        var je4 = JournalEntry.Create(tid, "JE-TS-004",
            DateTime.UtcNow.AddDays(-8), "Gastos de personal — Abril");
        je4.AddLine(acctSalary.Id, acctSalary.AccountNumber, acctSalary.Name, EntrySide.Debit,  19_900m);
        je4.AddLine(acctBank.Id,   acctBank.AccountNumber,   acctBank.Name,   EntrySide.Credit, 19_900m);
        var je4Lines = je4.Post();
        foreach (var l in je4Lines)
        {
            if (l.AccountId == acctSalary.Id) acctSalary.ApplyDebit(l.Amount);
            else acctBank.ApplyCredit(l.Amount);
        }

        await accounting.JournalEntries.AddRangeAsync(je1, je2, je3, je4);
        accounting.Accounts.UpdateRange(acctBank, acctAR, acctInv, acctAP, acctCap, acctRev, acctCOGS, acctSalary, acctRent);
        await accounting.SaveChangesAsync();

        // Invoicing — facturas basadas en órdenes de venta
        var invoicing = sp.GetRequiredService<InvoicingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // FAC-TS-001: teclado + mouse (de OV-TS-001) — Paid
        var inv1 = Invoice.Create("FAC-TS-001", customers[1].Id, "EUR",
            today.AddDays(-10), today.AddDays(20), so1.Id);
        inv1.TenantId = tid;
        inv1.AddLine(InvoiceLine.Create(items[2].Name, 10, Money.Of(89m, "EUR"), catalogItemId: items[2].Id, sku: items[2].SKU));
        inv1.AddLine(InvoiceLine.Create(items[3].Name, 10, Money.Of(35m, "EUR"), catalogItemId: items[3].Id, sku: items[3].SKU));
        inv1.Issue();
        inv1.RecordPayment(inv1.TotalAmount, today.AddDays(-5), "TRANSF-TS-001");

        // FAC-TS-002: laptops (de OV-TS-002) — Issued (pendiente de pago)
        var inv2 = Invoice.Create("FAC-TS-002", customers[3].Id, "EUR",
            today.AddDays(-5), today.AddDays(25), so2.Id);
        inv2.TenantId = tid;
        inv2.AddLine(InvoiceLine.Create(items[0].Name, 2, Money.Of(1_200m, "EUR"), catalogItemId: items[0].Id, sku: items[0].SKU));
        inv2.Issue();

        // FAC-TS-003: switches + routers — Draft
        var inv3 = Invoice.Create("FAC-TS-003", customers[0].Id, "EUR",
            today, today.AddDays(30));
        inv3.TenantId = tid;
        inv3.AddLine(InvoiceLine.Create(items[6].Name, 3, Money.Of(320m, "EUR"), catalogItemId: items[6].Id, sku: items[6].SKU));
        inv3.AddLine(InvoiceLine.Create(items[5].Name, 2, Money.Of(75m, "EUR"), catalogItemId: items[5].Id, sku: items[5].SKU));

        // FAC-TS-004: monitores 4K — Partially paid
        var inv4 = Invoice.Create("FAC-TS-004", customers[4].Id, "EUR",
            today.AddDays(-20), today.AddDays(10));
        inv4.TenantId = tid;
        inv4.AddLine(InvoiceLine.Create(items[1].Name, 5, Money.Of(450m, "EUR"), catalogItemId: items[1].Id, sku: items[1].SKU));
        inv4.Issue();
        inv4.RecordPayment(Money.Of(900m, "EUR"), today.AddDays(-10), "PARTIAL-001");

        await invoicing.Invoices.AddRangeAsync(inv1, inv2, inv3, inv4);
        await invoicing.SaveChangesAsync();
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

        // Sales — consulting proposals + confirmed projects
        var salesNx = sp.GetRequiredService<SalesDbContext>();

        var qNx1 = Quote.Create("COT-NX-001", customers[0].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), "MXN", "MX");
        qNx1.TenantId = tid;
        qNx1.AddLine(QuoteLine.Create(services[0].Id, services[0].SKU, services[0].Name, 80, Money.Of(2_500m, "MXN")));
        qNx1.AddLine(QuoteLine.Create(services[4].Id, services[4].SKU, services[4].Name, 3, Money.Of(45_000m, "MXN")));
        qNx1.Send();
        qNx1.Accept();
        qNx1.MarkConvertedToOrder();

        var qNx2 = Quote.Create("COT-NX-002", customers[2].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), "MXN", "MX");
        qNx2.TenantId = tid;
        qNx2.AddLine(QuoteLine.Create(services[1].Id, services[1].SKU, services[1].Name, 2, Money.Of(85_000m, "MXN")));
        qNx2.Send();

        var qNx3 = Quote.Create("COT-NX-003", customers[3].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)), "MXN", "MX");
        qNx3.TenantId = tid;
        qNx3.AddLine(QuoteLine.Create(services[2].Id, services[2].SKU, services[2].Name, 200, Money.Of(1_800m, "MXN")));
        qNx3.AddLine(QuoteLine.Create(services[3].Id, services[3].SKU, services[3].Name, 40, Money.Of(2_200m, "MXN")));

        await salesNx.Quotes.AddRangeAsync(qNx1, qNx2, qNx3);

        var soNx1 = SalesOrder.Create("OV-NX-001", customers[0].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)), "MXN", "MX",
            originQuoteId: qNx1.Id);
        soNx1.TenantId = tid;
        soNx1.AddLine(SalesOrderLine.Create(services[0].Id, services[0].SKU, services[0].Name, 80, Money.Of(2_500m, "MXN")));
        soNx1.AddLine(SalesOrderLine.Create(services[4].Id, services[4].SKU, services[4].Name, 3, Money.Of(45_000m, "MXN")));
        soNx1.Confirm();

        var soNx2 = SalesOrder.Create("OV-NX-002", customers[1].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-8)), "MXN", "MX");
        soNx2.TenantId = tid;
        soNx2.AddLine(SalesOrderLine.Create(services[0].Id, services[0].SKU, services[0].Name, 40, Money.Of(2_500m, "MXN")));
        soNx2.Confirm();

        await salesNx.SalesOrders.AddRangeAsync(soNx1, soNx2);
        await salesNx.SaveChangesAsync();

        // Accounting — professional services chart of accounts
        var accountingNx = sp.GetRequiredService<AccountingDbContext>();
        var nxBank   = Account.Create(tid, "1100", "BBVA México — cuenta operativa", AccountType.Asset,     "MXN");
        var nxAR     = Account.Create(tid, "1200", "Cuentas por Cobrar Clientes",    AccountType.Asset,     "MXN");
        var nxAP     = Account.Create(tid, "2100", "Cuentas por Pagar",              AccountType.Liability, "MXN");
        var nxCap    = Account.Create(tid, "3000", "Capital Social",                 AccountType.Equity,    "MXN");
        var nxRev    = Account.Create(tid, "4000", "Ingresos por Consultoría",       AccountType.Revenue,   "MXN");
        var nxSalary = Account.Create(tid, "6100", "Sueldos y Honorarios",           AccountType.Expense,   "MXN");
        await accountingNx.Accounts.AddRangeAsync(nxBank, nxAR, nxAP, nxCap, nxRev, nxSalary);
        await accountingNx.SaveChangesAsync();

        var jeNx1 = JournalEntry.Create(tid, "JE-NX-001",
            DateTime.UtcNow.AddDays(-60), "Saldo inicial");
        jeNx1.AddLine(nxBank.Id, nxBank.AccountNumber, nxBank.Name, EntrySide.Debit,  500_000m);
        jeNx1.AddLine(nxCap.Id,  nxCap.AccountNumber,  nxCap.Name,  EntrySide.Credit, 500_000m);
        var jeNx1Lines = jeNx1.Post();
        foreach (var l in jeNx1Lines)
        {
            if (l.AccountId == nxBank.Id) nxBank.ApplyDebit(l.Amount);
            else nxCap.ApplyCredit(l.Amount);
        }

        var jeNx2 = JournalEntry.Create(tid, "JE-NX-002",
            DateTime.UtcNow.AddDays(-20), "Cobro proyecto Azteca Q1");
        jeNx2.AddLine(nxBank.Id, nxBank.AccountNumber, nxBank.Name, EntrySide.Debit,  180_000m);
        jeNx2.AddLine(nxRev.Id,  nxRev.AccountNumber,  nxRev.Name,  EntrySide.Credit, 180_000m);
        var jeNx2Lines = jeNx2.Post();
        foreach (var l in jeNx2Lines)
        {
            if (l.AccountId == nxBank.Id) nxBank.ApplyDebit(l.Amount);
            else nxRev.ApplyCredit(l.Amount);
        }

        var jeNx3 = JournalEntry.Create(tid, "JE-NX-003",
            DateTime.UtcNow.AddDays(-10), "Nómina Abril");
        jeNx3.AddLine(nxSalary.Id, nxSalary.AccountNumber, nxSalary.Name, EntrySide.Debit,  85_000m);
        jeNx3.AddLine(nxBank.Id,   nxBank.AccountNumber,   nxBank.Name,   EntrySide.Credit, 85_000m);
        var jeNx3Lines = jeNx3.Post();
        foreach (var l in jeNx3Lines)
        {
            if (l.AccountId == nxSalary.Id) nxSalary.ApplyDebit(l.Amount);
            else nxBank.ApplyCredit(l.Amount);
        }

        await accountingNx.JournalEntries.AddRangeAsync(jeNx1, jeNx2, jeNx3);
        accountingNx.Accounts.UpdateRange(nxBank, nxAR, nxAP, nxCap, nxRev, nxSalary);
        await accountingNx.SaveChangesAsync();

        // Invoicing — honorarios profesionales
        var invoicingNx = sp.GetRequiredService<InvoicingDbContext>();
        var todayNx = DateOnly.FromDateTime(DateTime.UtcNow);

        // FAC-NX-001: consultoría estratégica Grupo Azteca Q1 — Paid
        var nxInv1 = Invoice.Create("FAC-NX-001", customers[0].Id, "MXN",
            todayNx.AddDays(-20), todayNx.AddDays(10), soNx1.Id);
        nxInv1.TenantId = tid;
        nxInv1.AddLine(InvoiceLine.Create(services[0].Name, 80, Money.Of(2_500m, "MXN"), catalogItemId: services[0].Id, sku: services[0].SKU));
        nxInv1.AddLine(InvoiceLine.Create(services[4].Name, 3, Money.Of(45_000m, "MXN"), catalogItemId: services[4].Id, sku: services[4].SKU));
        nxInv1.Issue();
        nxInv1.RecordPayment(nxInv1.TotalAmount, todayNx.AddDays(-10), "SPEI-NX-001");

        // FAC-NX-002: consultoría Banco Regional — Issued
        var nxInv2 = Invoice.Create("FAC-NX-002", customers[1].Id, "MXN",
            todayNx.AddDays(-8), todayNx.AddDays(22));
        nxInv2.TenantId = tid;
        nxInv2.AddLine(InvoiceLine.Create(services[0].Name, 40, Money.Of(2_500m, "MXN"), catalogItemId: services[0].Id, sku: services[0].SKU));
        nxInv2.Issue();

        // FAC-NX-003: auditoría Seguros del Pacífico — Draft
        var nxInv3 = Invoice.Create("FAC-NX-003", customers[2].Id, "MXN",
            todayNx, todayNx.AddDays(30));
        nxInv3.TenantId = tid;
        nxInv3.AddLine(InvoiceLine.Create(services[1].Name, 2, Money.Of(85_000m, "MXN"), catalogItemId: services[1].Id, sku: services[1].SKU));

        await invoicingNx.Invoices.AddRangeAsync(nxInv1, nxInv2, nxInv3);
        await invoicingNx.SaveChangesAsync();
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

        // Sales — cotizaciones y órdenes de venta moda
        var salesBm = sp.GetRequiredService<SalesDbContext>();
        var todayBm = DateOnly.FromDateTime(DateTime.UtcNow);

        var qBm1 = Quote.Create("COT-BM-001", customers[0].Id,
            todayBm.AddDays(30), "ARS", "AR");
        qBm1.TenantId = tid;
        qBm1.AddLine(QuoteLine.Create(items[0].Id, items[0].SKU, items[0].Name, 50, Money.Of(18_500m, "ARS")));
        qBm1.AddLine(QuoteLine.Create(items[1].Id, items[1].SKU, items[1].Name, 40, Money.Of(12_200m, "ARS")));
        qBm1.Send();
        qBm1.Accept();
        qBm1.MarkConvertedToOrder();

        var qBm2 = Quote.Create("COT-BM-002", customers[1].Id,
            todayBm.AddDays(20), "ARS", "AR");
        qBm2.TenantId = tid;
        qBm2.AddLine(QuoteLine.Create(items[3].Id, items[3].SKU, items[3].Name, 20, Money.Of(28_000m, "ARS")));
        qBm2.AddLine(QuoteLine.Create(items[4].Id, items[4].SKU, items[4].Name, 15, Money.Of(42_000m, "ARS")));
        qBm2.Send();

        var qBm3 = Quote.Create("COT-BM-003", customers[3].Id,
            todayBm.AddDays(45), "ARS", "AR");
        qBm3.TenantId = tid;
        qBm3.AddLine(QuoteLine.Create(items[5].Id, items[5].SKU, items[5].Name, 10, Money.Of(85_000m, "ARS")));
        qBm3.AddLine(QuoteLine.Create(items[6].Id, items[6].SKU, items[6].Name, 25, Money.Of(9_500m, "ARS")));

        await salesBm.Quotes.AddRangeAsync(qBm1, qBm2, qBm3);

        var soBm1 = SalesOrder.Create("OV-BM-001", customers[0].Id,
            todayBm.AddDays(-12), "ARS", "AR",
            requestedDeliveryDate: todayBm.AddDays(3), originQuoteId: qBm1.Id);
        soBm1.TenantId = tid;
        soBm1.AddLine(SalesOrderLine.Create(items[0].Id, items[0].SKU, items[0].Name, 50, Money.Of(18_500m, "ARS")));
        soBm1.AddLine(SalesOrderLine.Create(items[1].Id, items[1].SKU, items[1].Name, 40, Money.Of(12_200m, "ARS")));
        soBm1.Confirm();

        var soBm2 = SalesOrder.Create("OV-BM-002", customers[2].Id,
            todayBm.AddDays(-4), "ARS", "AR");
        soBm2.TenantId = tid;
        soBm2.AddLine(SalesOrderLine.Create(items[7].Id, items[7].SKU, items[7].Name, 30, Money.Of(4_800m, "ARS")));
        soBm2.AddLine(SalesOrderLine.Create(items[8].Id, items[8].SKU, items[8].Name, 20, Money.Of(11_500m, "ARS")));
        soBm2.Confirm();

        await salesBm.SalesOrders.AddRangeAsync(soBm1, soBm2);
        await salesBm.SaveChangesAsync();

        // Purchasing
        var purchasingBm = sp.GetRequiredService<PurchasingDbContext>();
        var poBm1 = PurchaseOrder.Create(tid, "OC-BM-001", suppliers[0].Id, "ARS", "AR",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(10));
        poBm1.AddLine(items[0].Id, items[0].SKU, items[0].Name, 100, Money.Of(9_200m, "ARS"));
        poBm1.AddLine(items[1].Id, items[1].SKU, items[1].Name, 80, Money.Of(6_100m, "ARS"));
        poBm1.Send();
        poBm1.Confirm();

        var poBm2 = PurchaseOrder.Create(tid, "OC-BM-002", suppliers[1].Id, "ARS", "AR",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(7));
        poBm2.AddLine(items[4].Id, items[4].SKU, items[4].Name, 30, Money.Of(22_000m, "ARS"));
        poBm2.Send();

        await purchasingBm.PurchaseOrders.AddRangeAsync(poBm1, poBm2);
        await purchasingBm.SaveChangesAsync();

        // Accounting
        var accountingBm = sp.GetRequiredService<AccountingDbContext>();
        var bmBank  = Account.Create(tid, "1100", "Banco Nación — cuenta corriente ARS", AccountType.Asset,     "ARS");
        var bmAR    = Account.Create(tid, "1200", "Cuentas por Cobrar",                  AccountType.Asset,     "ARS");
        var bmInv   = Account.Create(tid, "1300", "Inventario Productos",                AccountType.Asset,     "ARS");
        var bmAP    = Account.Create(tid, "2100", "Cuentas por Pagar",                   AccountType.Liability, "ARS");
        var bmCap   = Account.Create(tid, "3000", "Capital Social",                      AccountType.Equity,    "ARS");
        var bmRev   = Account.Create(tid, "4000", "Ventas",                              AccountType.Revenue,   "ARS");
        var bmCOGS  = Account.Create(tid, "5000", "Costo de Mercadería Vendida",         AccountType.Expense,   "ARS");
        var bmSal   = Account.Create(tid, "6100", "Sueldos y Cargas Sociales",           AccountType.Expense,   "ARS");
        await accountingBm.Accounts.AddRangeAsync(bmBank, bmAR, bmInv, bmAP, bmCap, bmRev, bmCOGS, bmSal);
        await accountingBm.SaveChangesAsync();

        var jeBm1 = JournalEntry.Create(tid, "JE-BM-001",
            DateTime.UtcNow.AddDays(-45), "Saldo inicial");
        jeBm1.AddLine(bmBank.Id, bmBank.AccountNumber, bmBank.Name, EntrySide.Debit,  1_200_000m);
        jeBm1.AddLine(bmCap.Id,  bmCap.AccountNumber,  bmCap.Name,  EntrySide.Credit, 1_200_000m);
        var jeBm1Lines = jeBm1.Post();
        foreach (var l in jeBm1Lines)
        {
            if (l.AccountId == bmBank.Id) bmBank.ApplyDebit(l.Amount);
            else bmCap.ApplyCredit(l.Amount);
        }

        var jeBm2 = JournalEntry.Create(tid, "JE-BM-002",
            DateTime.UtcNow.AddDays(-12), "Venta Tiendas El Ángel");
        jeBm2.AddLine(bmAR.Id,  bmAR.AccountNumber,  bmAR.Name,  EntrySide.Debit,  1_413_000m);
        jeBm2.AddLine(bmRev.Id, bmRev.AccountNumber, bmRev.Name, EntrySide.Credit, 1_413_000m);
        var jeBm2Lines = jeBm2.Post();
        foreach (var l in jeBm2Lines)
        {
            if (l.AccountId == bmAR.Id) bmAR.ApplyDebit(l.Amount);
            else bmRev.ApplyCredit(l.Amount);
        }

        var jeBm3 = JournalEntry.Create(tid, "JE-BM-003",
            DateTime.UtcNow.AddDays(-8), "Cobro Tiendas El Ángel");
        jeBm3.AddLine(bmBank.Id, bmBank.AccountNumber, bmBank.Name, EntrySide.Debit,  480_000m);
        jeBm3.AddLine(bmAR.Id,   bmAR.AccountNumber,   bmAR.Name,   EntrySide.Credit, 480_000m);
        var jeBm3Lines = jeBm3.Post();
        foreach (var l in jeBm3Lines)
        {
            if (l.AccountId == bmBank.Id) bmBank.ApplyDebit(l.Amount);
            else bmAR.ApplyCredit(l.Amount);
        }

        var jeBm4 = JournalEntry.Create(tid, "JE-BM-004",
            DateTime.UtcNow.AddDays(-5), "Pago Textil Andina");
        jeBm4.AddLine(bmAP.Id,   bmAP.AccountNumber,   bmAP.Name,   EntrySide.Debit,  320_000m);
        jeBm4.AddLine(bmBank.Id, bmBank.AccountNumber, bmBank.Name, EntrySide.Credit, 320_000m);
        var jeBm4Lines = jeBm4.Post();
        foreach (var l in jeBm4Lines)
        {
            if (l.AccountId == bmAP.Id) bmAP.ApplyDebit(l.Amount);
            else bmBank.ApplyCredit(l.Amount);
        }

        await accountingBm.JournalEntries.AddRangeAsync(jeBm1, jeBm2, jeBm3, jeBm4);
        accountingBm.Accounts.UpdateRange(bmBank, bmAR, bmInv, bmAP, bmCap, bmRev, bmCOGS, bmSal);
        await accountingBm.SaveChangesAsync();

        // Invoicing
        var invoicingBm = sp.GetRequiredService<InvoicingDbContext>();

        var bmInv1 = Invoice.Create("FAC-BM-001", customers[0].Id, "ARS",
            todayBm.AddDays(-12), todayBm.AddDays(18), soBm1.Id);
        bmInv1.TenantId = tid;
        bmInv1.AddLine(InvoiceLine.Create(items[0].Name, 50, Money.Of(18_500m, "ARS"), catalogItemId: items[0].Id, sku: items[0].SKU));
        bmInv1.AddLine(InvoiceLine.Create(items[1].Name, 40, Money.Of(12_200m, "ARS"), catalogItemId: items[1].Id, sku: items[1].SKU));
        bmInv1.Issue();
        bmInv1.RecordPayment(Money.Of(480_000m, "ARS"), todayBm.AddDays(-8), "TRANSF-BM-001");

        var bmInv2 = Invoice.Create("FAC-BM-002", customers[2].Id, "ARS",
            todayBm.AddDays(-4), todayBm.AddDays(26), soBm2.Id);
        bmInv2.TenantId = tid;
        bmInv2.AddLine(InvoiceLine.Create(items[7].Name, 30, Money.Of(4_800m, "ARS"), catalogItemId: items[7].Id, sku: items[7].SKU));
        bmInv2.AddLine(InvoiceLine.Create(items[8].Name, 20, Money.Of(11_500m, "ARS"), catalogItemId: items[8].Id, sku: items[8].SKU));
        bmInv2.Issue();

        await invoicingBm.Invoices.AddRangeAsync(bmInv1, bmInv2);
        await invoicingBm.SaveChangesAsync();
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

        // Sales — catering y menús para empresas
        var salesMg = sp.GetRequiredService<SalesDbContext>();
        var todayMg = DateOnly.FromDateTime(DateTime.UtcNow);

        var qMg1 = Quote.Create("COT-MG-001", customers[1].Id,
            todayMg.AddDays(15), "EUR", "ES");
        qMg1.TenantId = tid;
        qMg1.AddLine(QuoteLine.Create(items[3].Id, items[3].SKU, items[3].Name, 30, Money.Of(68m, "EUR")));
        qMg1.AddLine(QuoteLine.Create(items[4].Id, items[4].SKU, items[4].Name, 8, Money.Of(320m, "EUR")));
        qMg1.Send();
        qMg1.Accept();
        qMg1.MarkConvertedToOrder();

        var qMg2 = Quote.Create("COT-MG-002", customers[0].Id,
            todayMg.AddDays(30), "EUR", "ES");
        qMg2.TenantId = tid;
        qMg2.AddLine(QuoteLine.Create(items[0].Id, items[0].SKU, items[0].Name, 100, Money.Of(18m, "EUR")));
        qMg2.AddLine(QuoteLine.Create(items[1].Id, items[1].SKU, items[1].Name, 100, Money.Of(12m, "EUR")));
        qMg2.Send();

        await salesMg.Quotes.AddRangeAsync(qMg1, qMg2);

        var soMg1 = SalesOrder.Create("OV-MG-001", customers[1].Id,
            todayMg.AddDays(-7), "EUR", "ES",
            requestedDeliveryDate: todayMg.AddDays(8), originQuoteId: qMg1.Id);
        soMg1.TenantId = tid;
        soMg1.AddLine(SalesOrderLine.Create(items[3].Id, items[3].SKU, items[3].Name, 30, Money.Of(68m, "EUR")));
        soMg1.AddLine(SalesOrderLine.Create(items[4].Id, items[4].SKU, items[4].Name, 8, Money.Of(320m, "EUR")));
        soMg1.Confirm();

        await salesMg.SalesOrders.AddAsync(soMg1);
        await salesMg.SaveChangesAsync();

        // Purchasing — ingredientes de temporada
        var purchasingMg = sp.GetRequiredService<PurchasingDbContext>();
        var poMg1 = PurchaseOrder.Create(tid, "OC-MG-001", suppliers[0].Id, "EUR", "ES",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(2));
        poMg1.AddLine(items[7].Id, items[7].SKU, items[7].Name, 10, Money.Of(18m, "EUR"));
        poMg1.AddLine(items[8].Id, items[8].SKU, items[8].Name, 8, Money.Of(22m, "EUR"));
        poMg1.Send();
        poMg1.Confirm();

        var poMg2 = PurchaseOrder.Create(tid, "OC-MG-002", suppliers[2].Id, "EUR", "ES",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(4));
        poMg2.AddLine(items[5].Id, items[5].SKU, items[5].Name, 20, Money.Of(12m, "EUR"));
        poMg2.Send();

        await purchasingMg.PurchaseOrders.AddRangeAsync(poMg1, poMg2);
        await purchasingMg.SaveChangesAsync();

        // Accounting
        var accountingMg = sp.GetRequiredService<AccountingDbContext>();
        var mgBank  = Account.Create(tid, "1100", "CaixaBank — cuenta corriente EUR", AccountType.Asset,     "EUR");
        var mgAR    = Account.Create(tid, "1200", "Cuentas por Cobrar",               AccountType.Asset,     "EUR");
        var mgInv   = Account.Create(tid, "1300", "Inventario Cocina",                AccountType.Asset,     "EUR");
        var mgAP    = Account.Create(tid, "2100", "Cuentas por Pagar Proveedores",    AccountType.Liability, "EUR");
        var mgCap   = Account.Create(tid, "3000", "Capital Social",                   AccountType.Equity,    "EUR");
        var mgRev   = Account.Create(tid, "4000", "Ingresos Restauración",            AccountType.Revenue,   "EUR");
        var mgCOGS  = Account.Create(tid, "5000", "Coste de Materias Primas",         AccountType.Expense,   "EUR");
        var mgSal   = Account.Create(tid, "6100", "Sueldos y Seguridad Social",       AccountType.Expense,   "EUR");
        await accountingMg.Accounts.AddRangeAsync(mgBank, mgAR, mgInv, mgAP, mgCap, mgRev, mgCOGS, mgSal);
        await accountingMg.SaveChangesAsync();

        var jeMg1 = JournalEntry.Create(tid, "JE-MG-001",
            DateTime.UtcNow.AddDays(-90), "Saldo inicial");
        jeMg1.AddLine(mgBank.Id, mgBank.AccountNumber, mgBank.Name, EntrySide.Debit,  30_000m);
        jeMg1.AddLine(mgCap.Id,  mgCap.AccountNumber,  mgCap.Name,  EntrySide.Credit, 30_000m);
        var jeMg1Lines = jeMg1.Post();
        foreach (var l in jeMg1Lines)
        {
            if (l.AccountId == mgBank.Id) mgBank.ApplyDebit(l.Amount);
            else mgCap.ApplyCredit(l.Amount);
        }

        var jeMg2 = JournalEntry.Create(tid, "JE-MG-002",
            DateTime.UtcNow.AddDays(-7), "Recaudación semana");
        jeMg2.AddLine(mgBank.Id, mgBank.AccountNumber, mgBank.Name, EntrySide.Debit,  8_400m);
        jeMg2.AddLine(mgRev.Id,  mgRev.AccountNumber,  mgRev.Name,  EntrySide.Credit, 8_400m);
        var jeMg2Lines = jeMg2.Post();
        foreach (var l in jeMg2Lines)
        {
            if (l.AccountId == mgBank.Id) mgBank.ApplyDebit(l.Amount);
            else mgRev.ApplyCredit(l.Amount);
        }

        var jeMg3 = JournalEntry.Create(tid, "JE-MG-003",
            DateTime.UtcNow.AddDays(-5), "Compras semana");
        jeMg3.AddLine(mgCOGS.Id, mgCOGS.AccountNumber, mgCOGS.Name, EntrySide.Debit,  2_000m);
        jeMg3.AddLine(mgBank.Id, mgBank.AccountNumber,  mgBank.Name, EntrySide.Credit, 2_000m);
        var jeMg3Lines = jeMg3.Post();
        foreach (var l in jeMg3Lines)
        {
            if (l.AccountId == mgCOGS.Id) mgCOGS.ApplyDebit(l.Amount);
            else mgBank.ApplyCredit(l.Amount);
        }

        await accountingMg.JournalEntries.AddRangeAsync(jeMg1, jeMg2, jeMg3);
        accountingMg.Accounts.UpdateRange(mgBank, mgAR, mgInv, mgAP, mgCap, mgRev, mgCOGS, mgSal);
        await accountingMg.SaveChangesAsync();

        // Invoicing — menú degustación y catering
        var invoicingMg = sp.GetRequiredService<InvoicingDbContext>();

        var mgInv1 = Invoice.Create("FAC-MG-001", customers[1].Id, "EUR",
            todayMg.AddDays(-7), todayMg.AddDays(8), soMg1.Id);
        mgInv1.TenantId = tid;
        mgInv1.AddLine(InvoiceLine.Create(items[3].Name, 30, Money.Of(68m, "EUR"), catalogItemId: items[3].Id, sku: items[3].SKU));
        mgInv1.AddLine(InvoiceLine.Create(items[4].Name, 8, Money.Of(320m, "EUR"), catalogItemId: items[4].Id, sku: items[4].SKU));
        mgInv1.Issue();
        mgInv1.RecordPayment(mgInv1.TotalAmount, todayMg.AddDays(-2), "TRANSFER-MG-001");

        var mgInv2 = Invoice.Create("FAC-MG-002", customers[0].Id, "EUR",
            todayMg.AddDays(-3), todayMg.AddDays(12));
        mgInv2.TenantId = tid;
        mgInv2.AddLine(InvoiceLine.Create(items[0].Name, 100, Money.Of(18m, "EUR"), catalogItemId: items[0].Id, sku: items[0].SKU));
        mgInv2.Issue();

        await invoicingMg.Invoices.AddRangeAsync(mgInv1, mgInv2);
        await invoicingMg.SaveChangesAsync();
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

        // Sales — B2B wholesale quotes and orders
        var salesMm = sp.GetRequiredService<SalesDbContext>();

        var qMm1 = Quote.Create("COT-MM-001", customers[0].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), "CRC", "CR");
        qMm1.TenantId = tid;
        qMm1.AddLine(QuoteLine.Create(items[0].Id, items[0].SKU, items[0].Name, 200, Money.Of(2_500m, "CRC")));
        qMm1.AddLine(QuoteLine.Create(items[1].Id, items[1].SKU, items[1].Name, 100, Money.Of(1_800m, "CRC")));
        qMm1.AddLine(QuoteLine.Create(items[5].Id, items[5].SKU, items[5].Name, 150, Money.Of(1_200m, "CRC")));
        qMm1.Send();
        qMm1.Accept();
        qMm1.MarkConvertedToOrder();

        var qMm2 = Quote.Create("COT-MM-002", customers[2].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)), "CRC", "CR");
        qMm2.TenantId = tid;
        qMm2.AddLine(QuoteLine.Create(items[13].Id, items[13].SKU, items[13].Name, 500, Money.Of(8_500m, "CRC")));
        qMm2.AddLine(QuoteLine.Create(items[14].Id, items[14].SKU, items[14].Name, 300, Money.Of(5_200m, "CRC")));
        qMm2.Send();

        var qMm3 = Quote.Create("COT-MM-003", customers[3].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), "CRC", "CR");
        qMm3.TenantId = tid;
        qMm3.AddLine(QuoteLine.Create(items[17].Id, items[17].SKU, items[17].Name, 300, Money.Of(3_200m, "CRC")));
        qMm3.AddLine(QuoteLine.Create(items[18].Id, items[18].SKU, items[18].Name, 200, Money.Of(4_500m, "CRC")));

        await salesMm.Quotes.AddRangeAsync(qMm1, qMm2, qMm3);

        var soMm1 = SalesOrder.Create("OV-MM-001", customers[0].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15)), "CRC", "CR",
            requestedDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            originQuoteId: qMm1.Id);
        soMm1.TenantId = tid;
        soMm1.AddLine(SalesOrderLine.Create(items[0].Id, items[0].SKU, items[0].Name, 200, Money.Of(2_500m, "CRC")));
        soMm1.AddLine(SalesOrderLine.Create(items[1].Id, items[1].SKU, items[1].Name, 100, Money.Of(1_800m, "CRC")));
        soMm1.AddLine(SalesOrderLine.Create(items[5].Id, items[5].SKU, items[5].Name, 150, Money.Of(1_200m, "CRC")));
        soMm1.Confirm();

        var soMm2 = SalesOrder.Create("OV-MM-002", customers[1].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), "CRC", "CR");
        soMm2.TenantId = tid;
        soMm2.AddLine(SalesOrderLine.Create(items[6].Id, items[6].SKU, items[6].Name, 10, Money.Of(195_000m, "CRC")));
        soMm2.AddLine(SalesOrderLine.Create(items[7].Id, items[7].SKU, items[7].Name, 20, Money.Of(42_000m, "CRC")));
        soMm2.Confirm();

        var soMm3 = SalesOrder.Create("OV-MM-003", customers[4].Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), "CRC", "CR");
        soMm3.TenantId = tid;
        soMm3.AddLine(SalesOrderLine.Create(items[0].Id, items[0].SKU, items[0].Name, 100, Money.Of(2_500m, "CRC")));
        soMm3.AddLine(SalesOrderLine.Create(items[2].Id, items[2].SKU, items[2].Name, 80, Money.Of(1_650m, "CRC")));

        await salesMm.SalesOrders.AddRangeAsync(soMm1, soMm2, soMm3);
        await salesMm.SaveChangesAsync();

        // Purchasing — replenishment orders from suppliers
        var purchasingMm = sp.GetRequiredService<PurchasingDbContext>();

        var poMm1 = PurchaseOrder.Create(tid, "OC-MM-001", suppliers[0].Id, "CRC", "CR",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(3));
        poMm1.AddLine(items[0].Id, items[0].SKU, items[0].Name, 500, Money.Of(1_400m, "CRC"));
        poMm1.AddLine(items[1].Id, items[1].SKU, items[1].Name, 300, Money.Of(980m, "CRC"));
        poMm1.AddLine(items[5].Id, items[5].SKU, items[5].Name, 400, Money.Of(720m, "CRC"));
        poMm1.Send();
        poMm1.Confirm();

        var poMm2 = PurchaseOrder.Create(tid, "OC-MM-002", suppliers[1].Id, "CRC", "CR",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(10));
        poMm2.AddLine(items[6].Id, items[6].SKU, items[6].Name, 20, Money.Of(120_000m, "CRC"));
        poMm2.AddLine(items[7].Id, items[7].SKU, items[7].Name, 30, Money.Of(25_000m, "CRC"));
        poMm2.AddLine(items[8].Id, items[8].SKU, items[8].Name, 15, Money.Of(55_000m, "CRC"));
        poMm2.Send();

        var poMm3 = PurchaseOrder.Create(tid, "OC-MM-003", suppliers[4].Id, "CRC", "CR",
            expectedDeliveryDate: DateTime.UtcNow.AddDays(5));
        poMm3.AddLine(items[10].Id, items[10].SKU, items[10].Name, 200, Money.Of(3_200m, "CRC"));
        poMm3.AddLine(items[11].Id, items[11].SKU, items[11].Name, 150, Money.Of(4_800m, "CRC"));
        poMm3.Send();
        poMm3.Confirm();

        await purchasingMm.PurchaseOrders.AddRangeAsync(poMm1, poMm2, poMm3);
        await purchasingMm.SaveChangesAsync();

        // Accounting — balance sheet in CRC
        var accountingMm = sp.GetRequiredService<AccountingDbContext>();
        var mmBank   = Account.Create(tid, "1100", "Banco Nacional — cuenta corriente CRC", AccountType.Asset,     "CRC");
        var mmBankUSD= Account.Create(tid, "1101", "BAC San José — cuenta USD",             AccountType.Asset,     "USD");
        var mmAR     = Account.Create(tid, "1200", "Cuentas por Cobrar Clientes",           AccountType.Asset,     "CRC");
        var mmInv    = Account.Create(tid, "1300", "Inventario de Mercancía",                AccountType.Asset,     "CRC");
        var mmAP     = Account.Create(tid, "2100", "Cuentas por Pagar Proveedores",          AccountType.Liability, "CRC");
        var mmCap    = Account.Create(tid, "3000", "Capital Social",                         AccountType.Equity,    "CRC");
        var mmRev    = Account.Create(tid, "4000", "Ventas Netas",                           AccountType.Revenue,   "CRC");
        var mmCOGS   = Account.Create(tid, "5000", "Costo de Ventas",                        AccountType.Expense,   "CRC");
        var mmSalary = Account.Create(tid, "6100", "Sueldos y Cargas Sociales",              AccountType.Expense,   "CRC");
        var mmRent   = Account.Create(tid, "6200", "Alquiler Local Comercial",               AccountType.Expense,   "CRC");
        await accountingMm.Accounts.AddRangeAsync(mmBank, mmBankUSD, mmAR, mmInv, mmAP, mmCap, mmRev, mmCOGS, mmSalary, mmRent);
        await accountingMm.SaveChangesAsync();

        var jeMm1 = JournalEntry.Create(tid, "JE-MM-001",
            DateTime.UtcNow.AddDays(-60), "Saldo inicial");
        jeMm1.AddLine(mmBank.Id, mmBank.AccountNumber, mmBank.Name, EntrySide.Debit,  80_000_000m);
        jeMm1.AddLine(mmCap.Id,  mmCap.AccountNumber,  mmCap.Name,  EntrySide.Credit, 80_000_000m);
        var jeMm1Lines = jeMm1.Post();
        foreach (var l in jeMm1Lines)
        {
            if (l.AccountId == mmBank.Id) mmBank.ApplyDebit(l.Amount);
            else mmCap.ApplyCredit(l.Amount);
        }

        var jeMm2 = JournalEntry.Create(tid, "JE-MM-002",
            DateTime.UtcNow.AddDays(-15), "Ventas semana 18");
        jeMm2.AddLine(mmBank.Id, mmBank.AccountNumber, mmBank.Name, EntrySide.Debit,  6_250_000m);
        jeMm2.AddLine(mmRev.Id,  mmRev.AccountNumber,  mmRev.Name,  EntrySide.Credit, 6_250_000m);
        var jeMm2Lines = jeMm2.Post();
        foreach (var l in jeMm2Lines)
        {
            if (l.AccountId == mmBank.Id) mmBank.ApplyDebit(l.Amount);
            else mmRev.ApplyCredit(l.Amount);
        }

        var jeMm3 = JournalEntry.Create(tid, "JE-MM-003",
            DateTime.UtcNow.AddDays(-7), "Ventas semana 19");
        jeMm3.AddLine(mmBank.Id, mmBank.AccountNumber, mmBank.Name, EntrySide.Debit,  7_100_000m);
        jeMm3.AddLine(mmRev.Id,  mmRev.AccountNumber,  mmRev.Name,  EntrySide.Credit, 7_100_000m);
        var jeMm3Lines = jeMm3.Post();
        foreach (var l in jeMm3Lines)
        {
            if (l.AccountId == mmBank.Id) mmBank.ApplyDebit(l.Amount);
            else mmRev.ApplyCredit(l.Amount);
        }

        var jeMm4 = JournalEntry.Create(tid, "JE-MM-004",
            DateTime.UtcNow.AddDays(-6), "Nómina quincenal");
        jeMm4.AddLine(mmSalary.Id, mmSalary.AccountNumber, mmSalary.Name, EntrySide.Debit,  4_200_000m);
        jeMm4.AddLine(mmBank.Id,   mmBank.AccountNumber,   mmBank.Name,   EntrySide.Credit, 4_200_000m);
        var jeMm4Lines = jeMm4.Post();
        foreach (var l in jeMm4Lines)
        {
            if (l.AccountId == mmSalary.Id) mmSalary.ApplyDebit(l.Amount);
            else mmBank.ApplyCredit(l.Amount);
        }

        var jeMm5 = JournalEntry.Create(tid, "JE-MM-005",
            DateTime.UtcNow.AddDays(-14), "Pago proveedor Dist. Nacional Alimentos");
        jeMm5.AddLine(mmAP.Id,   mmAP.AccountNumber,   mmAP.Name,   EntrySide.Debit,  3_800_000m);
        jeMm5.AddLine(mmBank.Id, mmBank.AccountNumber, mmBank.Name, EntrySide.Credit, 3_800_000m);
        var jeMm5Lines = jeMm5.Post();
        foreach (var l in jeMm5Lines)
        {
            if (l.AccountId == mmAP.Id) mmAP.ApplyDebit(l.Amount);
            else mmBank.ApplyCredit(l.Amount);
        }

        await accountingMm.JournalEntries.AddRangeAsync(jeMm1, jeMm2, jeMm3, jeMm4, jeMm5);
        accountingMm.Accounts.UpdateRange(mmBank, mmBankUSD, mmAR, mmInv, mmAP, mmCap, mmRev, mmCOGS, mmSalary, mmRent);
        await accountingMm.SaveChangesAsync();

        // Invoicing — B2B mayoreo Costa Rica
        var invoicingMm = sp.GetRequiredService<InvoicingDbContext>();
        var todayMm = DateOnly.FromDateTime(DateTime.UtcNow);

        // FAC-MM-001: canasta básica Hoteles Playa Dorada — Paid
        var mmFac1 = Invoice.Create("FAC-MM-001", customers[0].Id, "CRC",
            todayMm.AddDays(-15), todayMm.AddDays(15), soMm1.Id);
        mmFac1.TenantId = tid;
        mmFac1.AddLine(InvoiceLine.Create(items[0].Name, 200, Money.Of(2_500m, "CRC"), catalogItemId: items[0].Id, sku: items[0].SKU));
        mmFac1.AddLine(InvoiceLine.Create(items[1].Name, 100, Money.Of(1_800m, "CRC"), catalogItemId: items[1].Id, sku: items[1].SKU));
        mmFac1.AddLine(InvoiceLine.Create(items[5].Name, 150, Money.Of(1_200m, "CRC"), catalogItemId: items[5].Id, sku: items[5].SKU));
        mmFac1.Issue();
        mmFac1.RecordPayment(mmFac1.TotalAmount, todayMm.AddDays(-7), "SINPE-MM-001");

        // FAC-MM-002: electrodomésticos Corporación Universitaria — Issued
        var mmFac2 = Invoice.Create("FAC-MM-002", customers[1].Id, "CRC",
            todayMm.AddDays(-7), todayMm.AddDays(23), soMm2.Id);
        mmFac2.TenantId = tid;
        mmFac2.AddLine(InvoiceLine.Create(items[6].Name, 10, Money.Of(195_000m, "CRC"), catalogItemId: items[6].Id, sku: items[6].SKU));
        mmFac2.AddLine(InvoiceLine.Create(items[7].Name, 20, Money.Of(42_000m, "CRC"), catalogItemId: items[7].Id, sku: items[7].SKU));
        mmFac2.Issue();

        // FAC-MM-003: canasta básica Club Mayoreo del Pacífico — Draft
        var mmFac3 = Invoice.Create("FAC-MM-003", customers[4].Id, "CRC",
            todayMm.AddDays(-3), todayMm.AddDays(27));
        mmFac3.TenantId = tid;
        mmFac3.AddLine(InvoiceLine.Create(items[0].Name, 100, Money.Of(2_500m, "CRC"), catalogItemId: items[0].Id, sku: items[0].SKU));
        mmFac3.AddLine(InvoiceLine.Create(items[2].Name, 80, Money.Of(1_650m, "CRC"), catalogItemId: items[2].Id, sku: items[2].SKU));

        // FAC-MM-004: artículos limpieza Municipalidad San José — Paid
        var mmFac4 = Invoice.Create("FAC-MM-004", customers[2].Id, "CRC",
            todayMm.AddDays(-25), todayMm.AddDays(-5));
        mmFac4.TenantId = tid;
        mmFac4.AddLine(InvoiceLine.Create(items[10].Name, 200, Money.Of(3_200m, "CRC"), catalogItemId: items[10].Id, sku: items[10].SKU));
        mmFac4.AddLine(InvoiceLine.Create(items[11].Name, 150, Money.Of(4_800m, "CRC"), catalogItemId: items[11].Id, sku: items[11].SKU));
        mmFac4.Issue();
        mmFac4.RecordPayment(mmFac4.TotalAmount, todayMm.AddDays(-10), "SINPE-MM-002");

        await invoicingMm.Invoices.AddRangeAsync(mmFac1, mmFac2, mmFac3, mmFac4);
        await invoicingMm.SaveChangesAsync();

        // FiscalCR tenant config — sandbox mode without real credentials.
        // Upload a real .p12 + Hacienda credentials via the admin API to enable actual timbrado.
        var fiscalCrDb = sp.GetRequiredService<FiscalCrDbContext>();
        if (!await fiscalCrDb.TenantConfigs.AnyAsync(c => c.TenantId == tid))
        {
            var fiscalCrConfig = TenantFiscalCrConfig.Create(
                tenantId: tid,
                razonSocial: "MercaMás S.A.",
                nombreComercial: "MercaMás",
                tipoIdentificacion: "02",
                numeroIdentificacion: "3101999001",
                codigoActividad: "461101",   // Comercio al por mayor de alimentos
                provincia: "1",
                canton: "01",
                distrito: "01",
                otrasSenas: "San José, Barrio Escalante, Costa Rica",
                email: "facturacion@mercamas.cr",
                telefono: "+506 2222-1111",
                haciendaUsername: null,
                haciendaPassword: null,
                certificateBytes: null,
                certificatePassword: null,
                environment: "sandbox");

            await fiscalCrDb.TenantConfigs.AddAsync(fiscalCrConfig);
            await fiscalCrDb.SaveChangesAsync();
        }
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
