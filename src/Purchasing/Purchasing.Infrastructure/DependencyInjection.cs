using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purchasing.Domain.Repositories;
using Purchasing.Infrastructure.Persistence;
using Purchasing.Infrastructure.Persistence.Repositories;

namespace Purchasing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPurchasingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddDbContext<PurchasingDbContext>(o => o.UseInMemoryDatabase("Purchasing"));
        else
            services.AddDbContext<PurchasingDbContext>(o =>
                o.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(PurchasingDbContext).Assembly.FullName)));

        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        return services;
    }
}
