using Catalog.Domain.Repositories;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<CatalogDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("CatalogDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<ICatalogItemRepository, CatalogItemRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();

        return services;
    }
}
