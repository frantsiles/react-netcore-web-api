using Api.Application.Common.Interfaces;
using Serilog.Context;
using System.Diagnostics;

namespace Api.WebApi.Middleware;

/// <summary>
/// Adds tenant_id and country_code to every OTel span and Serilog log scope
/// so all telemetry is filterable by tenant in Grafana/Tempo.
/// </summary>
public sealed class TenantEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tenantId = tenantContext.TenantId.ToString();
        var countryCode = tenantContext.CountryCode;

        // Tag the current OTel activity (span)
        var activity = Activity.Current;
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("tenant.country", countryCode);

        // Push into Serilog LogContext so every log line in this request carries the values
        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("CountryCode", countryCode))
        {
            await next(context);
        }
    }
}
