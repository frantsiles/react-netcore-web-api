using FiscalCR.Domain.Services;
using FiscalCR.Infrastructure.Persistence;
using FiscalCR.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiscalCR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFiscalCrInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<FiscalCrDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("FiscalCrDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IElectronicDocumentRepository, ElectronicDocumentRepository>();
        services.AddSingleton<IFeCrXmlGenerator, FeCrXmlGenerator>();

        // Use stub implementations in development (no real Hacienda credentials needed)
        // Replace with real implementations when ATV credentials are configured
        var mode = configuration["FiscalCR:Mode"] ?? "Development";
        if (mode == "Production")
        {
            // TODO: wire real XmlSigner and HaciendaClient once ATV credentials exist
            services.AddSingleton<IXmlSigner, StubXmlSigner>();
            services.AddSingleton<IHaciendaClient, StubHaciendaClient>();
        }
        else
        {
            services.AddSingleton<IXmlSigner, StubXmlSigner>();
            services.AddSingleton<IHaciendaClient, StubHaciendaClient>();
        }

        return services;
    }
}
