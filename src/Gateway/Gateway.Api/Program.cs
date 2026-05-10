using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog — structured JSON + OTel OTLP sink ───────────────────────────────

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.OpenTelemetry(opt =>
    {
        opt.Endpoint = ctx.Configuration["Otel:Endpoint"] ?? "http://localhost:4317";
        opt.Protocol = OtlpProtocol.Grpc;
        opt.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "demo-gateway",
            ["service.version"] = "1.0.0"
        };
    }));

// ── OpenTelemetry — traces + metrics, export via OTLP ────────────────────────

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "demo-gateway",
        serviceVersion: "1.0.0"))
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())           // reads OTEL_EXPORTER_OTLP_ENDPOINT env var
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// ── YARP reverse proxy — routes & clusters from appsettings.json ─────────────

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── App ───────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.MapReverseProxy();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "demo-gateway",
    timestamp = DateTime.UtcNow
})).WithTags("health").ExcludeFromDescription();

app.Run();
