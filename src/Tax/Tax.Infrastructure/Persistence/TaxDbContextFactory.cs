using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tax.Infrastructure.Persistence;

public sealed class TaxDbContextFactory : IDesignTimeDbContextFactory<TaxDbContext>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=demodb;Username=demo;Password=demo123";

    public TaxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaxDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new TaxDbContext(options, new DesignTimeTenantContext());
    }
}
