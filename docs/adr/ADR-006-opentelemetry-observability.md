# ADR-006 — OpenTelemetry como estándar de observabilidad

| Campo | Valor |
|-------|-------|
| Estado | Aceptado |
| Fecha | 2025-05 |
| Ámbito | Observabilidad de todos los servicios .NET |

---

## Contexto

Un sistema distribuido con Gateway + BFF + API + Worker es difícil de depurar sin observabilidad. Cuando una petición falla, necesitas saber en qué servicio falló, cuánto tardó cada hop, y qué logs se generaron en ese contexto. Sin correlación entre logs, métricas y trazas, la depuración es manual y costosa.

Además, el backend de observabilidad debería poder cambiar (Jaeger, Zipkin, Datadog, New Relic) sin tocar el código de los servicios.

## Decisión

Integrar el **OpenTelemetry SDK para .NET** en todos los servicios con exportación OTLP a un **OTel Collector**. El Collector hace el routing hacia los backends específicos:

- Métricas → Prometheus
- Logs → Grafana Loki
- Trazas → Grafana Tempo

**Serilog** se usa para el API de logging (más ergonómico que `ILogger` para logging estructurado), con el sink OpenTelemetry para que los logs lleguen al Collector con el `traceId` correcto.

El resultado es correlación completa: desde un log en Loki puedes saltar a la traza en Tempo, y desde la traza puedes filtrar los logs del mismo `traceId`.

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("demo-api"))
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(b => b
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

## Consecuencias

**Positivas:**
- OTel es el estándar de la industria, adoptado por todos los vendors de observabilidad. El código no cambia si el backend de observabilidad cambia.
- La correlación log-traza es automática: Serilog enriquece cada log con el `traceId` activo del contexto OTel.
- El OTel Collector hace de buffer y puede aplicar sampling, filtrado y transformación antes de enviar a los backends.
- En desarrollo local sin Docker, los servicios arrancan sin OTel Collector disponible — Serilog falla silenciosamente (no crashea).

**Negativas:**
- El stack de observabilidad completo (Collector + Prometheus + Loki + Tempo + Grafana) añade 5 contenedores al Docker Compose.
- La configuración inicial del OTel Collector tiene mucha fricción de sintaxis (pipelines, receivers, exporters).
- Para ver la observabilidad funcionar hay que usar el perfil `--profile observability` en Docker Compose — no es obvio para alguien que lo baja por primera vez.

## Alternativas descartadas

**Application Insights (Azure Monitor):** Excelente en producción Azure, pero ata la observabilidad a Azure. En local requiere connection string o emulador. OTel permite exportar a Application Insights también via el exporter de Azure Monitor.

**Jaeger standalone:** Más simple de configurar, pero solo resuelve trazas. No hay correlación con logs ni métricas.

**Logging a archivos + grep:** El approach más simple. Descartado porque no escala a sistemas distribuidos y no permite correlación entre servicios.
