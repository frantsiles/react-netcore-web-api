using Api.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiscalMX.Infrastructure.Persistence;

public class FiscalMxDbContextFactory : IDesignTimeDbContextFactory<FiscalMxDbContext>
{
    public FiscalMxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FiscalMxDbContext>()
            .UseNpgsql("Host=localhost;Database=erpdev;Username=postgres;Password=postgres")
            .Options;
        return new FiscalMxDbContext(options, new NullTenantContext());
    }

    private sealed class NullTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public string CountryCode => "MX";
    }
}
