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
        services.AddScoped<ITenantFiscalCrConfigRepository, TenantFiscalCrConfigRepository>();
        services.AddSingleton<IFeCrXmlGenerator, FeCrXmlGenerator>();

        // Real implementations: use Scoped (not Singleton) because they depend on ITenantFiscalCrConfigRepository.
        // XmlSignerService falls back to no-signing when the tenant has no certificate configured.
        // HaciendaClient falls back to stub-acceptance when no Hacienda credentials are configured.
        services.AddScoped<IXmlSigner, XmlSignerService>();
        services.AddScoped<IHaciendaClient, HaciendaClient>();

        services.AddHttpClient("Hacienda");

        return services;
    }
}
