using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payroll.Infrastructure.Persistence;

public class PayrollDbContextFactory : IDesignTimeDbContextFactory<PayrollDbContext>
{
    public PayrollDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseNpgsql("Host=localhost;Database=erpdev;Username=postgres;Password=postgres")
            .Options;
        // Design-time factory: no real ITenantContext needed
        return new PayrollDbContext(options, new NullTenantContext());
    }

    private sealed class NullTenantContext : Api.Application.Common.Interfaces.ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public string CountryCode => "CR";
    }
}
