# Minimal APIs y health checks

> **Categoría:** Backend
> **Fase del roadmap:** 0 · **Estado:** 📝 escrito
> **Última actualización:** 2026-09-29

## Qué es (en criollo)
**Minimal APIs** es la forma de definir endpoints HTTP en ASP.NET Core con una función, sin clases controller:

```csharp
app.MapGet("/api/catalogo/productos", async (CatalogoDbContext db, CancellationToken ct) =>
    await db.Productos.ToListAsync(ct));
```

ASP.NET arma los parámetros solo: los que están registrados en DI (como el `DbContext`) los inyecta, y los que vienen de la URL o del body los lee de la request.

Un **health check** es un endpoint (`/health`) que responde si la app está sana. En nuestro caso, si llega a la base de cada módulo.

## Qué problema resuelve
- **Minimal APIs:** con controllers, ASP.NET busca por reflexión todas las clases `Controller` de todos los ensamblados. Con Minimal APIs, **cada módulo registra explícitamente sus endpoints** (`MapCatalogoEndpoints`). El host decide qué se expone, y el endpoint puede quedar `internal` en el módulo.
- **Health checks:**
  - Docker, Cloud Run o Kubernetes necesitan saber si el contenedor **funciona**, no solo si el proceso existe.
  - Una API que arrancó pero no llega a la base está "viva" y rota a la vez.

## Cómo lo usamos en este proyecto
- [`Program.cs`](../../../src/Api/PymeCommerce.Api/Program.cs) registra cada módulo y mapea sus endpoints.
- Cada módulo define los suyos en `<Modulo>Endpoints.cs`, bajo un grupo (`MapGroup("/api/catalogo")`).
- Cada módulo agrega su propio health check de base (`AddDbContextCheck<CatalogoDbContext>("catalogo-db")`), y el host los expone todos juntos en `/health`.
- `/health` se verifica en la CI ([`ci.yml`](../../../.github/workflows/ci.yml)) después del `docker compose up`. En la Fase 1 lo va a usar Cloud Run.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Endpoint** | Combinación de método HTTP + ruta + función que responde. |
| **Route group** | `MapGroup("/api/x")`: prefijo común para varios endpoints. |
| **Binding de parámetros** | Cómo ASP.NET decide de dónde sale cada parámetro: ruta, query, body, DI o `CancellationToken`. |
| **`CancellationToken`** | Se cancela si el cliente corta la request, así no se sigue consultando la base para nadie. |
| **Liveness / readiness** | "¿Está vivo el proceso?" y "¿Puede atender tráfico?". Por ahora `/health` cubre las dos cosas. |
| **Healthy / Degraded / Unhealthy** | Los tres estados posibles de un health check. |

## Alternativas y por qué no
| Alternativa | Por qué no |
|---|---|
| Controllers MVC | Más ceremonia y descubrimiento por reflexión en todos los ensamblados, que desdibuja los límites. Siguen siendo válidos y se usan mucho. |
| FastEndpoints / Carter | Librerías sobre Minimal APIs. No hacen falta todavía. |
| Health check que solo devuelve 200 | No detecta que la base no está disponible. |

## Errores comunes
- **Poner lógica de negocio en la lambda del endpoint.** Cuando crezca, va a un servicio o handler del módulo.
- **No pasar el `CancellationToken`** a las consultas.
- **Health checks caros** (p. ej. consultar tablas grandes): se llaman muy seguido.
- **Exponer detalles internos** en `/health` en producción (versiones, connection strings).

## Qué aprendí / dudas abiertas
- Pendiente: OpenAPI (documentación de la API) se suma cuando haya endpoints de escritura en la Fase 1.

## Para profundizar
- [Minimal APIs: overview](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview)
- [Health checks en ASP.NET Core](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
