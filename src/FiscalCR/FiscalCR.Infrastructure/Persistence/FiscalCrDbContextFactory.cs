using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiscalCR.Infrastructure.Persistence;

public class FiscalCrDbContextFactory : IDesignTimeDbContextFactory<FiscalCrDbContext>
{
    public FiscalCrDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FiscalCrDbContext>()
            .UseNpgsql("Host=localhost;Database=erpdev;Username=postgres;Password=postgres")
            .Options;
        return new FiscalCrDbContext(options);
    }
}
