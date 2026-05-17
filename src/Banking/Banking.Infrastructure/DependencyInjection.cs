using Banking.Domain.Repositories;
using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBankingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddDbContext<BankingDbContext>(o => o.UseInMemoryDatabase("Banking"));
        else
            services.AddDbContext<BankingDbContext>(o =>
                o.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(BankingDbContext).Assembly.FullName)));

        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        return services;
    }
}
