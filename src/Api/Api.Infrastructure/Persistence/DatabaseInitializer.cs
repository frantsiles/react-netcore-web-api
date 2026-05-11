using Api.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds initial data.
    /// If DB_RESET=true (env var) or Seed:Reset=true (config), wipes the database first.
    /// </summary>
    public static async Task InitializeAsync(
        AppDbContext context,
        IConfiguration configuration,
        ILogger logger)
    {
        var resetDb = string.Equals(Environment.GetEnvironmentVariable("DB_RESET"), "true", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(configuration["Seed:Reset"], "true", StringComparison.OrdinalIgnoreCase);

        if (context.Database.IsRelational())
        {
            if (resetDb)
            {
                logger.LogWarning("DB_RESET activo: eliminando y recreando la base de datos...");
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();

            if (resetDb)
                logger.LogInformation("Base de datos recreada y datos semilla aplicados.");
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        await DataSeeder.SeedAsync(context);
    }
}
