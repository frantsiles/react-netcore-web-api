using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Seed;
using ControlPlane.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests.Common;

/// <summary>
/// Creates an in-process test server using the real application startup,
/// replacing the EF Core database with a fresh InMemory instance per test class.
/// MassTransit is rewired to the in-memory test harness so events are observable.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";
    private readonly string _cpDbName = $"ControlPlane-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace AppDbContext with a unique InMemory instance per factory
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbOptionsDescriptor is not null) services.Remove(dbOptionsDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace ControlPlaneDbContext with a unique InMemory instance per factory
            // to avoid duplicate-key races when SeedDefaultTenantAsync runs in parallel
            var cpOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ControlPlaneDbContext>));
            if (cpOptionsDescriptor is not null) services.Remove(cpOptionsDescriptor);

            services.AddDbContext<ControlPlaneDbContext>(options =>
                options.UseInMemoryDatabase(_cpDbName));

            RemoveMassTransitServices(services);
            services.AddMassTransitTestHarness();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DataSeeder.SeedAsync(db).GetAwaiter().GetResult();
        });
    }

    private static void RemoveMassTransitServices(IServiceCollection services)
    {
        var toRemove = services
            .Where(d => d.ServiceType.FullName is not null &&
                        (d.ServiceType.FullName.StartsWith("MassTransit", StringComparison.Ordinal) ||
                         d.ImplementationType?.FullName?.StartsWith("MassTransit", StringComparison.Ordinal) == true))
            .ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);
    }
}
