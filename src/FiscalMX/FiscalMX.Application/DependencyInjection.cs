using Microsoft.Extensions.DependencyInjection;

namespace FiscalMX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFiscalMxApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
