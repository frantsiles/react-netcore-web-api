using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Accounting.Infrastructure.Persistence;

public sealed class AccountingDbContextFactory : IDesignTimeDbContextFactory<AccountingDbContext>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=demodb;Username=demo;Password=demo123";

    public AccountingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AccountingDbContext(options, new DesignTimeTenantContext());
    }
}
