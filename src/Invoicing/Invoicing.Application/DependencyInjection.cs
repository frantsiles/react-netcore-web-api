using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInvoicingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
