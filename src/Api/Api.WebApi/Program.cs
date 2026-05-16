using Api.Application;
using Api.Application.Common.Interfaces;
using Catalog.Application;
using Catalog.Infrastructure;
using Parties.Application;
using Parties.Infrastructure;
using Sales.Application;
using Sales.Infrastructure;
using Api.Infrastructure;
using Api.Infrastructure.Persistence;
using Api.WebApi.Hubs;
using Api.WebApi.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.OpenTelemetry;
using System.Text;

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
            ["service.name"] = "demo-api",
            ["service.version"] = "1.0.0"
        };
    }));

// ── OpenTelemetry — traces + metrics ─────────────────────────────────────────

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("demo-api", serviceVersion: "1.0.0"))
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
        .AddHttpClientInstrumentation()
.AddOtlpExporter())           // reads OTEL_EXPORTER_OTLP_ENDPOINT env var
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// ── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPartiesApplication();
builder.Services.AddPartiesInfrastructure(builder.Configuration);
builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddSalesApplication();
builder.Services.AddSalesInfrastructure(builder.Configuration);

// SignalR — Azure SignalR Service compatible
var azureSignalRConnectionString = builder.Configuration["AzureSignalR:ConnectionString"];
var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(azureSignalRConnectionString))
    signalRBuilder.AddAzureSignalR(azureSignalRConnectionString);

// Session notifier: depends on SignalR hub, registered here (not in Infrastructure)
builder.Services.AddScoped<ISessionNotifier, SignalRSessionNotifier>();

builder.Services.AddControllers();

// JWT Authentication — token issued by this API, validated by BFF
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidAudience            = jwtSettings["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// CORS: allow BFF, frontend and direct Swagger access — SignalR requires credentials
builder.Services.AddCors(options =>
    options.AddPolicy("Dev", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── App ───────────────────────────────────────────────────────────────────────

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseInitializer.InitializeAsync(db, config, logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Dev");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantEnrichmentMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.MapControllers();
app.MapHub<SessionHub>("/hubs/sessions");

app.Run();

// Required by WebApplicationFactory in integration tests
public partial class Program;
