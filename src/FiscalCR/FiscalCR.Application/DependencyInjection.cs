using Microsoft.Extensions.DependencyInjection;

namespace FiscalCR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFiscalCrApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
