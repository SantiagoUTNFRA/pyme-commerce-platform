# EF Core y migraciones

> **Categoría:** Backend
> **Fase del roadmap:** 0 · **Estado:** 📝 escrito
> **Última actualización:** 2026-09-29
> **Decisión asociada:** [ADR-0002](../../adr/0002-estructura-de-modulos-y-persistencia.md)

## Qué es (en criollo)
**Entity Framework Core (EF Core)** es un **ORM**: traduce entre objetos de C# y tablas de la base. Escribís `db.Productos.Where(p => p.Nombre == "X")` y EF genera el SQL.

Una **migración** es un archivo C# que describe **un cambio en el esquema de la base**: crear una tabla, agregar una columna. Las migraciones se versionan en git junto al código, así la base evoluciona de forma reproducible y no a mano.

**Npgsql** es el proveedor que conecta EF Core con PostgreSQL.

## Qué problema resuelve
- **Sin ORM:** hay que escribir SQL y mapear cada columna a mano.
- **Sin migraciones:** alguien corre un `ALTER TABLE` en su máquina, se olvida de avisar y en la nube la base queda distinta. Con migraciones, cualquier base (local, CI, Neon) se lleva a la misma versión con un comando.
- **Historial:** EF registra en `__EFMigrationsHistory` qué migraciones ya se aplicaron, así aplicar dos veces no hace nada (es idempotente).

## Cómo lo usamos en este proyecto
- **Un `DbContext` por módulo**, interno: [`CatalogoDbContext`](../../../src/Modules/Catalogo/PymeCommerce.Catalogo/Persistencia/CatalogoDbContext.cs) y [`StockDbContext`](../../../src/Modules/Stock/PymeCommerce.Stock/Persistencia/StockDbContext.cs).
- **Un esquema por módulo** (`HasDefaultSchema("catalogo")`) **y su propio historial** (`MigrationsHistoryTable(..., "catalogo")`), configurado en [`CatalogoModule`](../../../src/Modules/Catalogo/PymeCommerce.Catalogo/CatalogoModule.cs).
- **`snake_case`** con `EFCore.NamingConventions`: `Producto.Nombre` → `catalogo.productos.nombre`.
- **Migraciones** en `Persistencia/Migraciones/` de cada módulo.
- **IDs:** `Guid.CreateVersion7()`. Los UUID v7 son ordenables por tiempo, así que los índices se fragmentan menos que con UUID v4 aleatorios.
- **Aplicación:** `dotnet run --project src/Api/PymeCommerce.Api -- --migrate`, o el servicio `migrator` de Compose.

### Comandos
```bash
dotnet tool restore   # instala dotnet-ef (versión fijada en dotnet-tools.json)

# Nueva migración en un módulo
dotnet ef migrations add <Nombre> \
  --project src/Modules/Catalogo/PymeCommerce.Catalogo \
  --startup-project src/Api/PymeCommerce.Api \
  --context CatalogoDbContext \
  --output-dir Persistencia/Migraciones

# Ver el SQL que generaría (útil para revisar antes de aplicar)
dotnet ef migrations script --project ... --startup-project ... --context CatalogoDbContext
```

- `--project` indica dónde viven la migración y el `DbContext`.
- `--startup-project` indica qué app se ejecuta para leer la configuración. Por eso la API referencia `Microsoft.EntityFrameworkCore.Design`.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **DbContext** | La "sesión" con la base: conoce las entidades, rastrea cambios y guarda con `SaveChanges`. |
| **DbSet** | Una colección consultable, equivalente a una tabla (`db.Productos`). |
| **Model snapshot** | Archivo generado con el modelo actual. EF lo compara con el código para saber qué cambió en la próxima migración. |
| **Up / Down** | Qué hace la migración y cómo se deshace. |
| **`AsNoTracking()`** | Consulta de solo lectura: EF no rastrea cambios, así que es más rápida. |
| **Esquema (schema)** | "Carpeta" de tablas dentro de una base de Postgres. |
| **UUID v7** | Identificador único que empieza con un timestamp. Postgres 18 lo genera nativo con `uuidv7()`. |

## Alternativas y por qué no
| Alternativa | Por qué no |
|---|---|
| **Dapper** (SQL a mano) | Más control y velocidad, pero sin migraciones ni seguimiento de cambios. Se puede sumar para consultas puntuales. |
| **Scripts SQL versionados** (DbUp, Flyway) | Muy buena opción, pero duplica el modelo (C# + SQL). EF lo genera desde el código. |
| **Migrar al arrancar la API** | Ver [ADR-0002](../../adr/0002-estructura-de-modulos-y-persistencia.md): hay riesgo con varias instancias. |
| **Migration bundles** | Un ejecutable por `DbContext`. Con `--migrate` en el host alcanza. |

## Errores comunes
- **El `fail:` en la primera migración** de una base vacía. EF intenta leer `__EFMigrationsHistory`, que todavía no existe, lo loguea como error, la crea y sigue. **Es inofensivo**: en la segunda corrida no aparece. Lo vimos en local y en Compose.
- **Olvidarse `--context`** con varios `DbContext`: `dotnet ef` no sabe cuál usar.
- **Editar una migración ya aplicada** en otra base. Se crea una nueva.
- **Historial compartido** entre módulos: por eso cada uno tiene el suyo en su esquema.
- **Consultar entidades de otro módulo.** Se usa su `I<Modulo>Api`.

## Qué aprendí / dudas abiertas
- Conviene decidir `snake_case` **antes** de la primera migración: cambiarlo después obliga a renombrar todo.
- Pendiente para la Fase 1: el `TenantId` y los *query filters* globales.

## Para profundizar
- [EF Core: migraciones](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Aplicar migraciones (estrategias)](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)
- [Npgsql EF Core provider](https://www.npgsql.org/efcore/)
- [EFCore.NamingConventions](https://github.com/efcore/EFCore.NamingConventions)
