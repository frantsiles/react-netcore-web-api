using Accounting.Domain.Repositories;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddDbContext<AccountingDbContext>(o => o.UseInMemoryDatabase("Accounting"));
        else
            services.AddDbContext<AccountingDbContext>(o =>
                o.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(AccountingDbContext).Assembly.FullName)));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        return services;
    }
}
