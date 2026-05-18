using FiscalMX.Domain.Services;
using FiscalMX.Infrastructure.Persistence;
using FiscalMX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiscalMX.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFiscalMxInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<FiscalMxDbContext>((sp, opt) =>
        {
            if (string.IsNullOrEmpty(connStr))
                opt.UseInMemoryDatabase("FiscalMxDb");
            else
                opt.UseNpgsql(connStr);
        });

        services.AddScoped<ICfdiDocumentRepository, CfdiDocumentRepository>();
        services.AddSingleton<ICfdiXmlGenerator, CfdiXmlGenerator>();

        // Stub PAC for all modes until real PAC credentials are configured
        services.AddSingleton<IPacClient, StubPacClient>();

        return services;
    }
}
