using Invoicing.Domain.Repositories;
using Invoicing.Infrastructure.Persistence;
using Invoicing.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInvoicingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<InvoicingDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("InvoicingDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        return services;
    }
}
