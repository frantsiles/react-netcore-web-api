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
        services.AddDbContext<TaxDbContext>(opt =>
            opt.UseInMemoryDatabase("TaxDb"));

        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        return services;
    }
}
