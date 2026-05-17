using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tax.Domain.Repositories;
using Tax.Infrastructure.Persistence;
using Tax.Infrastructure.Persistence.Repositories;

namespace Tax.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaxInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TaxDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("TaxDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        return services;
    }
}
