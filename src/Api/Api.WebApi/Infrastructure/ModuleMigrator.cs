using Invoicing.Infrastructure.Persistence;
using Accounting.Infrastructure.Persistence;
using Approvals.Infrastructure.Persistence;
using Banking.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence;
using ControlPlane.Infrastructure.Persistence;
using HR.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Parties.Infrastructure.Persistence;
using Purchasing.Infrastructure.Persistence;
using Sales.Infrastructure.Persistence;
using Tax.Infrastructure.Persistence;

namespace Api.WebApi.Infrastructure;

internal static class ModuleMigrator
{
    internal static async Task MigrateAllAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        await MigrateAsync<PartiesDbContext>(sp, logger);
        await MigrateAsync<CatalogDbContext>(sp, logger);
        await MigrateAsync<SalesDbContext>(sp, logger);
        await MigrateAsync<PurchasingDbContext>(sp, logger);
        await MigrateAsync<InventoryDbContext>(sp, logger);
        await MigrateAsync<AccountingDbContext>(sp, logger);
        await MigrateAsync<BankingDbContext>(sp, logger);
        await MigrateAsync<TaxDbContext>(sp, logger);
        await MigrateAsync<ApprovalsDbContext>(sp, logger);
        await MigrateAsync<HrDbContext>(sp, logger);
        await MigrateAsync<ControlPlaneDbContext>(sp, logger);
        await MigrateAsync<InvoicingDbContext>(sp, logger);
    }

    private static async Task MigrateAsync<TContext>(IServiceProvider sp, ILogger logger)
        where TContext : DbContext
    {
        var db = sp.GetRequiredService<TContext>();
        if (!db.Database.IsRelational()) return;

        logger.LogInformation("Applying migrations for {Context}…", typeof(TContext).Name);
        await db.Database.MigrateAsync();
    }
}
