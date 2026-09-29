# ADR-0002: Estructura de los módulos, persistencia y migraciones

- **Estado:** Aceptado
- **Fecha:** 2026-09-29
- **Relacionado:** [ADR-0001](0001-arquitectura.md) · [Módulos y tests de arquitectura](../estudio/arquitectura/modulos-y-tests-de-arquitectura.md) · [EF Core y migraciones](../estudio/backend/ef-core-y-migraciones.md)

## Contexto

El [ADR-0001](0001-arquitectura.md) decidió un **monolito modular**: cada módulo tiene una API pública, su interior es privado y tiene sus propios datos. Falta decidir **cómo se implementa eso en .NET**:

- ¿Cómo se separa, en proyectos, lo público de lo privado?
- ¿Cómo se aíslan los datos de cada módulo dentro de una sola base?
- ¿Quién aplica las migraciones y cuándo?
- ¿Cómo se verifica que los límites se respeten?

## Decisión

### 1. Dos proyectos por módulo

```
src/Modules/<Modulo>/
├── PymeCommerce.<Modulo>.Contracts/   # API pública: interfaces (I<Modulo>Api) y DTOs
└── PymeCommerce.<Modulo>/             # interior: dominio, EF, endpoints
```

- Otros módulos **solo** referencian el `Contracts`.
- En el interior **todo es `internal`**, salvo la clase `<Modulo>Module`, que el host usa para registrar el módulo (`Add<Modulo>Module`, `Map<Modulo>Endpoints`, `Migrar<Modulo>Async`).
- El host (`PymeCommerce.Api`) es el único proyecto que referencia los interiores.

### 2. Un `DbContext` y un esquema de Postgres por módulo

- Cada módulo tiene su `DbContext` interno, con `HasDefaultSchema("<modulo>")`.
- **Cada módulo tiene su propia tabla `__EFMigrationsHistory` dentro de su esquema.** Si no, los módulos compartirían un historial y la migración de uno podría confundirse con la de otro.
- Una sola base y una sola connection string (`ConnectionStrings:Postgres`).
- Los nombres van en `snake_case` (`EFCore.NamingConventions`), que es la convención de Postgres y evita tener que poner comillas en el SQL.

### 3. Las migraciones las aplica un migrador, no la API al arrancar

- Si la API se ejecuta con `--migrate`, aplica las migraciones de todos los módulos y termina.
- En Compose, el servicio `migrator` usa la **misma imagen** y la API arranca solo si terminó bien (`service_completed_successfully`).
- En la Fase 3, el mismo comando va a ser un paso del pipeline (p. ej. un Cloud Run Job).

### 4. Tests de arquitectura con ArchUnitNET

- Reglas:
  - el interior de un módulo no depende del interior de otro;
  - los `Contracts` no dependen de ningún interior.
- Los módulos se **descubren solos** a partir de los `PymeCommerce.*.Contracts.dll`: un módulo nuevo queda cubierto sin tocar los tests.
- Corren en la CI con cada push y cada PR.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| **Un proyecto por módulo**, con lo público `public` y lo demás `internal` | Menos proyectos, pero el límite depende solo del test y de la disciplina. Con dos proyectos, el compilador ya impide usar lo `internal` y el test cubre lo que queda (la clase pública de registro). |
| **Un proyecto por capa del módulo** (Domain, Application, Infrastructure, Api) | Es la estructura de Clean Architecture completa: 4 proyectos por módulo. Para módulos CRUD es ceremonia sin beneficio ([ADR-0001](0001-arquitectura.md): hexagonal solo donde hay integraciones). Si Pagos o Facturación lo necesitan, se separan carpetas internas, no proyectos. |
| **Un solo `DbContext` para toda la app** | Cualquier módulo podría consultar cualquier tabla con un `Include`. |
| **Una base por módulo** | Es lo que haría falta para microservicios, pero en Neon free significa N bases y se pierden las transacciones locales. El esquema por módulo deja preparada esa separación sin pagarla hoy. |
| **`Database.Migrate()` al arrancar la API** | Con varias instancias (Cloud Run), las migraciones correrían en paralelo. Además, la API necesitaría permisos de DDL en la base. |
| **EF Core migration bundles** (`dotnet ef migrations bundle`) | Generan un ejecutable por `DbContext`: con N módulos serían N ejecutables. El `--migrate` del host migra todos los módulos con una sola imagen. |
| **NetArchTest** | La última versión es de mayo de 2021. ArchUnitNET se sigue manteniendo y tiene integración con xUnit v3. |

## Consecuencias

**Positivas**
- El compilador impide usar tipos internos de otro módulo, y el test de arquitectura cubre lo que el compilador no ve.
- Mover un módulo a su propia base es cambiar su connection string: sus tablas y su historial ya están separados.
- Una sola imagen sirve para la API y para el migrador.

**Negativas / costos**
- Dos proyectos por módulo: con los 8 módulos del núcleo son 16 proyectos.
- Al agregar un módulo hay que acordarse de registrarlo en `Program.cs`, incluida su migración en el bloque `--migrate`.
- La primera vez que se migra una base vacía, EF Core loguea un `fail:` inofensivo, porque intenta leer una tabla de historial que todavía no existe. Ver la nota de [EF Core y migraciones](../estudio/backend/ef-core-y-migraciones.md).
