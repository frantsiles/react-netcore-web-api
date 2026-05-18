using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Domain.Services;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPayrollInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PayrollDbContext>((sp, opt) =>
        {
            var connStr = config.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=erpdev;Username=postgres;Password=postgres";
            opt.UseNpgsql(connStr);
        });

        services.AddScoped<IPayrollRunRepository, PayrollRunRepository>();
        return services;
    }
}
