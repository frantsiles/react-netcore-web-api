using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests.Common;

/// <summary>
/// Creates an in-process test server using the real application startup,
/// replacing the EF Core database with a fresh InMemory instance per test class.
/// The GUID is fixed at class level so all scopes within a test run share the same database.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    // Fixed name so all service scopes in the same test run share one InMemory DB
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Seed once — all scopes read from the same named InMemory DB
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DataSeeder.SeedAsync(db).GetAwaiter().GetResult();
        });
    }
}
