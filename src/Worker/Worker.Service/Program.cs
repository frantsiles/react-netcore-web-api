using MassTransit;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenTelemetry;
using Worker.Service.Consumers;
using Worker.Service.Workers;

var builder = new HostApplicationBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.OpenTelemetry(opt =>
    {
        opt.Endpoint = builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317";
        opt.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
        opt.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "demo-worker",
            ["service.version"] = "1.0.0"
        };
    })
    .CreateLogger();

builder.Services.AddSerilog();

// ── OpenTelemetry ─────────────────────────────────────────────────────────────

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("demo-worker", serviceVersion: "1.0.0"))
    .WithTracing(b => b
        .AddSource("MassTransit")
        .AddOtlpExporter())
    .WithMetrics(b => b
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// ── MassTransit — RabbitMQ (local) or Azure Service Bus (Azure) ───────────────

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<UserDeletedConsumer>();
    x.AddConsumer<UserRoleChangedConsumer>();

    var transport = builder.Configuration["MessageBus:Transport"] ?? "RabbitMQ";

    if (transport.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingAzureServiceBus((ctx, cfg) =>
        {
            var connStr = builder.Configuration["MessageBus:AzureServiceBus:ConnectionString"]
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
                builder.Configuration["MessageBus:RabbitMQ:Host"] ?? "localhost",
                builder.Configuration["MessageBus:RabbitMQ:VirtualHost"] ?? "/",
                h =>
                {
                    h.Username(builder.Configuration["MessageBus:RabbitMQ:Username"] ?? "guest");
                    h.Password(builder.Configuration["MessageBus:RabbitMQ:Password"] ?? "guest");
                });
            cfg.ConfigureEndpoints(ctx);
        });
    }
});

builder.Services.AddHostedService<HeartbeatWorker>();

var host = builder.Build();
await host.RunAsync();
