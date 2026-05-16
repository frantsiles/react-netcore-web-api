using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parties.Domain.Repositories;
using Parties.Infrastructure.Persistence;
using Parties.Infrastructure.Persistence.Repositories;

namespace Parties.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPartiesInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<PartiesDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("PartiesDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IPartyRepository, PartyRepository>();

        return services;
    }
}
