using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Parties.Infrastructure.Persistence;

public sealed class PartiesDbContextFactory : IDesignTimeDbContextFactory<PartiesDbContext>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=demodb;Username=demo;Password=demo123";

    public PartiesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new PartiesDbContext(options, new DesignTimeTenantContext());
    }
}
