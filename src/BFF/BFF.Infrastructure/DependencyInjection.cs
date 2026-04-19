using BFF.Domain.Interfaces;
using BFF.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BFF.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBffInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var apiBaseUrl = configuration["BackendApi:BaseUrl"]
            ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");

        services.AddHttpClient("BackendApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddScoped<IApiClient, ApiClient>();

        return services;
    }
}
