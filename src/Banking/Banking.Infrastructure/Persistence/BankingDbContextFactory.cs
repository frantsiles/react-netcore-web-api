using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Banking.Infrastructure.Persistence;

public sealed class BankingDbContextFactory : IDesignTimeDbContextFactory<BankingDbContext>
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=demodb;Username=demo;Password=demo123";

    public BankingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new BankingDbContext(options, new DesignTimeTenantContext());
    }
}
