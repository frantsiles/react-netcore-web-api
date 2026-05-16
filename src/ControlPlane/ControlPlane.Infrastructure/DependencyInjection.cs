using Api.Application.Common.Interfaces;
using ControlPlane.Application.Common.Interfaces;
using ControlPlane.Domain.Repositories;
using ControlPlane.Domain.Tenants;
using ControlPlane.Infrastructure.Persistence;
using ControlPlane.Infrastructure.Persistence.Repositories;
using ControlPlane.Infrastructure.TenantSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlPlane.Infrastructure;

public static class DependencyInjection
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static IServiceCollection AddControlPlaneInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            services.AddDbContext<ControlPlaneDbContext>(o => o.UseInMemoryDatabase("ControlPlane"));
        else
            services.AddDbContext<ControlPlaneDbContext>(o =>
                o.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ControlPlaneDbContext).Assembly.FullName)));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantSettingsCache, MemoryTenantSettingsCache>();

        // Override DefaultTenantSettings registered by Api.Infrastructure
        services.AddScoped<ITenantSettings, DbTenantSettings>();

        return services;
    }

    public static async Task SeedDefaultTenantAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Tenants.AnyAsync(t => t.Id == DefaultTenantId))
        {
            var tenant = Tenant.CreateWithId(
                DefaultTenantId,
                name: "Demo Company",
                slug: "demo",
                countryCode: "US",
                currencyCode: "USD",
                plan: TenantPlan.Standard);

            await db.Tenants.AddAsync(tenant);
            await db.SaveChangesAsync();
        }
    }
}
