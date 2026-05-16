using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sales.Domain.Repositories;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Persistence.Repositories;

namespace Sales.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<SalesDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("SalesDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();

        return services;
    }
}
