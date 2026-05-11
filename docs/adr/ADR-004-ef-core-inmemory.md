# ADR-004 — EF Core InMemory como capa de persistencia

| Campo | Valor |
|-------|-------|
| Estado | Aceptado |
| Fecha | 2025-04 |
| Ámbito | Capa de persistencia del Backend API |

---

## Contexto

El proyecto es una demo técnica y un proyecto de referencia. Necesita datos persistidos entre requests pero no necesita sobrevivir a reinicios. Cualquier base de datos real (PostgreSQL, SQL Server) añadiría una dependencia de infraestructura que complica el setup local, el CI y los Codespaces.

## Decisión

Usar **EF Core InMemory** como proveedor de base de datos. Los datos se inicializan en cada arranque del servicio mediante un `UserSeeder` que crea los usuarios de demo con contraseñas BCrypt.

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("DemoDb"));
```

Las interfaces de repositorio están en `Api.Domain`, las implementaciones en `Api.Infrastructure`. Si en el futuro se quisiera reemplazar InMemory por PostgreSQL, solo cambia la implementación del repositorio y la configuración del `AddDbContext` — los handlers no se tocan.

## Consecuencias

**Positivas:**
- Zero dependencias externas: la app arranca con `dotnet run` sin instalar nada más.
- Los tests de integración (`WebApplicationFactory`) tienen una base de datos aislada por test, sin necesidad de cleanup.
- Funciona en GitHub Codespaces, CI y local sin cambios.

**Negativas:**
- Los datos se pierden en cada reinicio — intencionado para una demo, problemático para producción.
- EF Core InMemory no valida constraints de base de datos (unique, foreign key). Los tests de integración no detectarían violaciones de constraints que sí fallarían en PostgreSQL.
- Las queries LINQ que funcionan en InMemory pueden fallar con proveedores SQL reales (funciones no traducibles). Esto es un riesgo si alguna query se pone compleja.

## Alternativas descartadas

**SQLite en archivo:** Mantendría los datos entre reinicios y soportaría constraints. Pero añade el archivo de base de datos al estado local y complica el cleanup en tests. Para los objetivos de esta demo, la complejidad no vale.

**PostgreSQL en Docker:** Opción production-grade que se añadirá si el proyecto evoluciona más allá de demo. El diseño actual (interfaces de repositorio en Domain) está preparado para este cambio con mínimo impacto.

**Testcontainers para tests de integración:** Añadiría una base de datos real en tests sin necesidad de InMemory. Considerado para una evolución futura del proyecto.
