using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Invoicing.Infrastructure.Persistence;

public sealed class InvoicingDbContextFactory : IDesignTimeDbContextFactory<InvoicingDbContext>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=demodb;Username=demo;Password=demo123";

    public InvoicingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new InvoicingDbContext(options, new DesignTimeTenantContext());
    }
}
