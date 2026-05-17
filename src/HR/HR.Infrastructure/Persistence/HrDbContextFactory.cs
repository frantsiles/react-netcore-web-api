using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HR.Infrastructure.Persistence;

public sealed class HrDbContextFactory : IDesignTimeDbContextFactory<HrDbContext>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=demodb;Username=demo;Password=demo123";

    public HrDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HrDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new HrDbContext(options, new DesignTimeTenantContext());
    }
}
