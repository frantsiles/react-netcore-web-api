# ADR-005 — YARP como API Gateway

| Campo | Valor |
|-------|-------|
| Estado | Aceptado |
| Fecha | 2025-05 |
| Ámbito | Punto de entrada en Docker y Kubernetes |

---

## Contexto

En el entorno de desarrollo local sin Docker, el Vite proxy hace el routing del frontend hacia el BFF. Pero en Docker y Kubernetes, el cliente necesita un punto de entrada único que sepa hacia dónde enrutar `/bff/*` y `/api/*`. Además, en producción queremos centralizar preocupaciones cross-cutting (health checks activos de upstream, rate limiting, transformación de headers) sin tocar los servicios individuales.

## Decisión

Usar **YARP (Yet Another Reverse Proxy)** de Microsoft como API Gateway. YARP es una librería .NET que convierte una aplicación ASP.NET Core en un proxy inverso configurable.

La configuración de rutas vive en `appsettings.json`:

```json
"ReverseProxy": {
  "Routes": {
    "bff-route": {
      "ClusterId": "bff-cluster",
      "Match": { "Path": "/bff/{**catch-all}" }
    }
  },
  "Clusters": {
    "bff-cluster": {
      "Destinations": {
        "bff": { "Address": "http://bff:8080/" }
      },
      "HealthCheck": {
        "Active": { "Enabled": true, "Path": "/bff/health" }
      }
    }
  }
}
```

En Docker/Kubernetes, el destino `Address` se sobreescribe con variables de entorno apuntando al nombre de servicio interno.

## Consecuencias

**Positivas:**
- YARP es .NET nativo — no necesita aprender Nginx config ni Envoy. El Gateway se configura con C# y JSON, mismo tooling que el resto del proyecto.
- Health checks activos: el Gateway detecta automáticamente cuando un upstream cae y puede dejar de enrutarle tráfico.
- Extensible: añadir rate limiting, circuit breaker o transformaciones de request/response es cuestión de middleware en el pipeline ASP.NET Core del Gateway.

**Negativas:**
- Es un servicio más en el stack — en Kubernetes, un pod adicional con su Deployment y Service.
- Para casos simples, YARP puede ser sobre-ingeniería. En este proyecto justifica su presencia como demonstración de patrones cloud-native.

## Alternativas descartadas

**Nginx como reverse proxy:** Probado en la imagen Docker del frontend para el proxy `/bff/*`. Nginx es excelente para servir archivos estáticos pero su configuración para proxy dinámico es menos flexible que YARP. No tiene health checks activos nativos de upstream.

**API Gateway gestionado de Azure (APIM):** Production-grade pero costoso y requiere provisioning en Azure. Tiene sentido en proyectos reales; en una demo añade friction innecesaria.

**Envoy Proxy:** Muy poderoso (base de Istio), pero la curva de configuración en YAML es alta y queda fuera del ecosistema .NET del proyecto.
