# ADR-003 — MassTransit como abstracción del message bus

| Campo | Valor |
|-------|-------|
| Estado | Aceptado |
| Fecha | 2025-05 |
| Ámbito | Worker Service y mensajería asíncrona |

---

## Contexto

El sistema necesita procesamiento asíncrono de eventos de dominio (UserCreated, UserDeleted, UserRoleChanged). En local, RabbitMQ es la opción natural — fácil de correr en Docker, interfaz de administración incluida. En Azure, el servicio equivalente es Azure Service Bus.

El problema: si los consumers dependen directamente del cliente de RabbitMQ o del SDK de Azure Service Bus, el código del Worker queda atado a la infraestructura. Cambiar de entorno implica reescribir los consumers.

## Decisión

Usar **MassTransit** como capa de abstracción sobre el message bus. Los consumers implementan `IConsumer<TMessage>` de MassTransit, sin referencias al transporte subyacente. La elección de transporte (RabbitMQ vs Azure Service Bus) se hace en la configuración:

```json
"MessageBus": {
  "Transport": "RabbitMQ"   // o "AzureServiceBus"
}
```

El `Program.cs` del Worker lee esta configuración y llama a `UseMassTransit(x => x.UsingRabbitMq(...))` o `UsingAzureServiceBus(...)` según corresponda.

```csharp
public class UserCreatedConsumer : IConsumer<UserCreated>
{
    public async Task Consume(ConsumeContext<UserCreated> context)
    {
        // Mismo código, cualquier transporte
    }
}
```

## Consecuencias

**Positivas:**
- El mismo binario del Worker corre en local (RabbitMQ) y en Azure (Service Bus) cambiando solo variables de entorno.
- MassTransit gestiona reconexiones, retries y dead-letter automáticamente.
- Los consumers son POCO testeables — MassTransit provee `InMemoryTestHarness` para tests sin infraestructura.
- Las convenciones de naming de queues/topics son automáticas (snake_case del tipo de mensaje).

**Negativas:**
- MassTransit tiene una curva de aprendizaje propia (ConsumeContext, Sagas, Outbox pattern).
- Las convenciones automáticas de naming pueden confundir si el topic de Azure Service Bus tiene un nombre diferente al esperado — hay que configurar explícitamente en ese caso.

## Alternativas descartadas

**Acceso directo a RabbitMQ.Client:** Menos dependencias, más control, pero requiere reimplementar reconexión, retry y serialización. Atado a RabbitMQ.

**Azure Service Bus SDK directo:** Mismo problema inverso — el código del Worker no funcionaría en local sin Service Bus.

**NServiceBus:** Alternativa enterprise con más features (Sagas, Outbox nativo). Descartado por licencia de pago y complejidad innecesaria para este contexto.
