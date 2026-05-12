using Api.Application.Assistant;
using Api.Application.Common.Interfaces;
using Api.Domain.Permissions.Repositories;
using Api.Domain.Roles.Repositories;
using Api.Domain.Sessions.Repositories;
using Api.Domain.Users.Repositories;
using Api.Infrastructure.Assistant;
using Api.Infrastructure.Assistant.Providers;
using Api.Infrastructure.Caching;
using Api.Infrastructure.Messaging;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Repositories;
using Api.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("AppDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();

        services.AddMemoryCache();
        services.AddScoped<IIdempotencyCache, InMemoryIdempotencyCache>();

        AddAssistant(services, configuration);
        AddMessaging(services, configuration);

        return services;
    }

    private static void AddAssistant(IServiceCollection services, IConfiguration configuration)
    {
        string provider = configuration["Assistant:Provider"] ?? "Ollama";

        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IKernelFactory, OpenAIKernelFactory>();
        else if (provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IKernelFactory, AzureOpenAIKernelFactory>();
        else
            services.AddSingleton<IKernelFactory, OllamaKernelFactory>();

        services.AddScoped<IAssistantService, AssistantService>();
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            string transport = configuration["MessageBus:Transport"] ?? "RabbitMQ";

            if (transport.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase))
            {
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    string connStr = configuration["MessageBus:AzureServiceBus:ConnectionString"]
                        ?? throw new InvalidOperationException("MessageBus:AzureServiceBus:ConnectionString not configured.");
                    cfg.Host(connStr);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(
                        configuration["MessageBus:RabbitMQ:Host"] ?? "localhost",
                        configuration["MessageBus:RabbitMQ:VirtualHost"] ?? "/",
                        h =>
                        {
                            h.Username(configuration["MessageBus:RabbitMQ:Username"] ?? "guest");
                            h.Password(configuration["MessageBus:RabbitMQ:Password"] ?? "guest");
                        });
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });
    }
}
