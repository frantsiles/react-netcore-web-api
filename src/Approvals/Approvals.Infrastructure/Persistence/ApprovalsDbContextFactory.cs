using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Approvals.Infrastructure.Persistence;

public sealed class ApprovalsDbContextFactory : IDesignTimeDbContextFactory<ApprovalsDbContext>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=demodb;Username=demo;Password=demo123";

    public ApprovalsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApprovalsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ApprovalsDbContext(options, new DesignTimeTenantContext());
    }
}
