using Microsoft.Extensions.DependencyInjection;

namespace Payroll.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPayrollApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
