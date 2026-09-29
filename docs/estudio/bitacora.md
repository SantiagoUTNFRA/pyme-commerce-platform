# 📓 Bitácora

Registro cronológico de lo que **agregamos, cambiamos o sacamos**, y **por qué**. Las entradas más nuevas van arriba.

Formato de cada entrada:

```
### AAAA-MM-DD: Título corto
- **Qué:** agregamos | cambiamos | sacamos …
- **Por qué:** …
- **Impacto:** qué archivos, fases o notas de estudio toca.
```

---

## 2026-09-29: Fase 0 terminada ✅

Los tres criterios de terminado se cumplen: `docker compose up` migra y levanta la API, la CI está en verde en `main` y el test de arquitectura falla con una violación a propósito. Estructura documentada en el [ADR-0002](../adr/0002-estructura-de-modulos-y-persistencia.md), y el [ADR-0001](../adr/0001-arquitectura.md) quedó revisado.

#### Agregamos la solución .NET 10 con configuración centralizada
- **Qué:**
  - `global.json`, que fija el SDK 10.0.300;
  - `Directory.Build.props`, con nullable, warnings como errores y analizadores recomendados;
  - `Directory.Packages.props`, con CPM y transitive pinning;
  - `.slnx`;
  - `dotnet-tools.json`, con `dotnet-ef`.
- **Por qué:** mismo SDK en todos lados, una sola versión de cada paquete y calidad desde el primer commit. Con el transitive pinning, EF Core queda en 10.0.12 en toda la solución, aunque Npgsql pida ≥ 10.0.4.
- **Impacto:** [.NET 10 y estructura de la solución](backend/dotnet-10-y-estructura-de-la-solucion.md).

#### Agregamos los módulos Catálogo y Stock, con Minimal APIs y `/health`
- **Qué:** cada módulo tiene un `Contracts` (`I<Modulo>Api` + DTO), un interior `internal`, una entidad semilla (`Producto`, `Deposito`), un endpoint de listado y un health check de base.
- **Por qué:** hacen falta al menos dos módulos para que el test de límites tenga sentido. Son los módulos de la Fase 1. Los demás se crean cuando llegue su fase.
- **Impacto:** [Minimal APIs y health checks](backend/minimal-apis-y-health-checks.md).

#### Agregamos EF Core + Npgsql, un esquema por módulo y `snake_case`
- **Qué:** un `DbContext` por módulo, con su esquema y su propio `__EFMigrationsHistory`. Se usa `EFCore.NamingConventions` y los IDs son UUID v7.
- **Por qué:** es el aislamiento de datos que pide el ADR-0001. `snake_case` es la convención de Postgres y cambiarla después obliga a renombrar todo el esquema. UUID v7 fragmenta menos los índices que v4.
- **Impacto:** [EF Core y migraciones](backend/ef-core-y-migraciones.md).

#### Elegimos Postgres 18
- **Qué:** `postgres:18-alpine` en local. Cuando se cree la base en Neon (Fase 1), hay que elegir también la 18.
- **Por qué:** es la versión más nueva que soporta Neon (14 a 18, verificado) y genera UUID v7 nativo.

#### Cambiamos NetArchTest por ArchUnitNET
- **Qué:** los tests de arquitectura usan ArchUnitNET con xUnit v3.
- **Por qué:** la última versión de NetArchTest.Rules es de mayo de 2021 (verificado en NuGet). ArchUnitNET sigue mantenido.
- **Impacto:** [Módulos y tests de arquitectura](arquitectura/modulos-y-tests-de-arquitectura.md), [roadmap](../roadmap.md).

#### Agregamos xUnit v3 con Microsoft.Testing.Platform
- **Qué:** reemplazamos la plantilla (xUnit v2 + VSTest) por xUnit v3 4.x + MTP, declarado en `global.json`.
- **Por qué:** es la versión mantenida de xUnit, y el SDK 10 soporta MTP de forma nativa en `dotnet test`.
- **Impacto:** [Pirámide de tests](testing/piramide-de-tests.md).

#### Agregamos Dockerfile multi-stage, `.dockerignore` y compose (Postgres + migrador + API)
- **Qué:**
  - build con `sdk:10.0` y runtime con `aspnet:10.0`, usuario no-root;
  - restore cacheado con `COPY --parents`;
  - migrador con la misma imagen y `--migrate`.
- **Por qué:** es el criterio 1 de la fase y la imagen que va a correr en Cloud Run.
- **Impacto:** [Docker y Docker Compose](infraestructura/docker-y-compose.md).

#### Agregamos la CI con GitHub Actions y la publicación en GHCR
- **Qué:** tres jobs:
  - build + tests;
  - smoke con `docker compose up` + `/health`;
  - en `main`, publicación en `ghcr.io/santiagoutnfra/pyme-commerce-api`, con tags `sha-…` y `latest`.
- **Por qué:** es el criterio 2 de la fase. El smoke test además verifica el criterio 1 en cada push, no solo una vez a mano.
- **Impacto:** [GitHub Actions y GHCR](infraestructura/github-actions-y-ghcr.md).

#### Agregamos notas de estudio y un README
- **Qué:** notas nuevas: ADRs, módulos y tests de arquitectura, .NET 10, EF Core, Minimal APIs, pirámide de tests y GitHub Actions. Se actualizaron la nota de Docker y el índice, y se agregó un `README.md` en la raíz.

---

## 2026-09-29: Arranque de la Fase 0

#### Cada módulo son dos proyectos: `Contracts` (público) + interior
- **Qué:** por ejemplo, `PymeCommerce.Catalogo.Contracts` (interfaces y DTOs) y `PymeCommerce.Catalogo` (dominio, EF y endpoints, todo `internal`). Los otros módulos solo pueden referenciar el `Contracts`.
- **Por qué:** el compilador ya impide usar el interior de otro módulo, y el test de arquitectura queda como segunda barrera por si alguien agrega una referencia "para salir del paso". La alternativa (un solo proyecto con `public`/`internal`) depende solo de la disciplina y del test.
- **Impacto:** estructura de `src/Modules/`, ADR-0002.

#### Las migraciones las aplica un "migrador" separado, no la API al arrancar
- **Qué:** en Compose, un servicio que aplica las migraciones y termina. La API arranca recién cuando ese servicio terminó bien.
- **Por qué:** si la API migra al arrancar y hay varias instancias (Cloud Run), las migraciones corren en paralelo. El mismo migrador se reutiliza como paso del pipeline en la Fase 3.

#### `TenantId` entra en la Fase 1, no en la Fase 0
- **Qué:** las entidades semilla de la Fase 0 (`Producto`, `Deposito`) todavía no tienen `TenantId`.
- **Por qué:** el multi-tenancy es un concepto completo (columna + resolución del tenant en cada request + filtros globales de EF + test de aislamiento). Agregar solo la columna ahora daría una falsa sensación de aislamiento. Entra entero en la Fase 1.

#### Repo público en GitHub y commits frecuentes
- **Qué:** el repo pasa a ser público en GitHub. Hacemos un commit con push por cada paso verificable, y se actualiza la regla 8 del CLAUDE.md.
- **Por qué:** en un repo público, GitHub Actions no tiene límite de minutos y el proyecto es visible para el portfolio. Con commits chicos, el historial cuenta cómo se construyó el proyecto.

---

## 2026-09-29: Definición inicial del proyecto

### Cambios tras la revisión del plan

#### Repo en `~/Proyectos .NET/pyme-commerce-platform`
- **Qué:** el proyecto vive en un repo nuevo y dedicado.
- **Por qué:** separarlo del proyecto anterior de microservicios, que queda solo como referencia.

#### RabbitMQ, outbox/inbox y el worker pasan de la Fase 0 a la Fase 2
- **Qué:** la Fase 0 queda con la solución, los módulos, Postgres, EF, los tests de arquitectura, Docker/Compose y la CI. La mensajería entra en la Fase 2.
- **Por qué:** en la Fase 2 aparece el **primer caso de uso real** que la necesita: confirmar una orden → descontar stock de forma asíncrona. Meter infraestructura antes de tener un problema que la justifique es complejidad sin beneficio y, además, no se aprende *para qué* sirve.
- **Impacto:** [roadmap](../roadmap.md), fases 0 y 2. La decisión sobre la librería de mensajería también pasa a la Fase 2.

#### Deploy mínimo al final de la Fase 1
- **Qué:** API en Cloud Run, base en Neon y backoffice en Vercel, ya en la Fase 1. La Fase 3 suma el worker, CloudAMQP y el deploy automático completo.
- **Por qué:** desplegar **temprano**, cuando hay pocas piezas, hace que los problemas de deploy (configuración, secretos, conexión a la base) aparezcan de a uno y no todos juntos al final.
- **Impacto:** [roadmap](../roadmap.md), fases 1 y 3.

#### Nuevo hito "MVP demostrable" al terminar la Fase 5
- **Qué:** un hito explícito con datos de demo, README guiado y video.
- **Por qué:** después de pagos y facturación, el flujo principal (vender → cobrar → facturar) ya está completo y es mostrable. Tener un hito claro evita seguir sumando cosas sin cerrar nada presentable.

### Decisiones de la definición inicial

#### .NET 10 LTS en vez de .NET 8
- **Qué:** cambiamos la versión pedida originalmente (.NET 8) por .NET 10.
- **Por qué:** .NET 8 deja de tener soporte el **10/11/2026**, a semanas de arrancar. .NET 10 es LTS, con soporte hasta noviembre de 2028.

#### Monolito modular + hexagonal selectivo + mensajería con RabbitMQ
- **Qué:** arquitectura base del proyecto.
- **Por qué:** equipo de una persona, necesidad de consistencia entre pagos, stock y facturas, límites todavía inciertos, y el objetivo de aprender mensajería real. Detalle en [ADR-0001](../adr/0001-arquitectura.md) y en [Estilos de arquitectura](arquitectura/estilos-de-arquitectura.md).

#### Odoo como ERP
- **Qué:** integramos con Odoo Community.
- **Por qué:** es open source, corre en Docker y tiene API externa. Tango Gestión es más común en PyMEs argentinas, pero requiere licencia y es difícil de montar para un portfolio.

#### ARCA con adaptador simulado primero
- **Qué:** la facturación arranca con un fake. WSAA + WSFEv1 en homologación se conectan cuando haya certificado.
- **Por qué:** todavía no hay un certificado de homologación. Con el puerto definido, cambiar el fake por el real no toca el dominio.

#### Keycloak pasa a extensión; el núcleo usa ASP.NET Core Identity + JWT
- **Qué:** sacamos Keycloak del núcleo.
- **Por qué:** es pesado para correr gratis en Cloud Run (memoria, arranque en frío). Identity + JWT alcanza para el núcleo, y Keycloak queda como extensión de aprendizaje.

#### Aspire y Kubernetes pasan a extensiones; la Fase 0 usa Docker + Compose
- **Qué:** la base de contenedores es Docker + Compose.
- **Por qué:** primero hay que entender bien los contenedores. Kubernetes y Aspire suman conceptos encima de eso y no son necesarios para el deploy elegido (Cloud Run). Ver [Docker y Compose](infraestructura/docker-y-compose.md) y [Kubernetes (intro)](infraestructura/kubernetes-intro.md).

#### Primer deploy en Cloud Run + Neon + Vercel
- **Qué:** hosting gratuito con servicios gestionados.
- **Por qué:** costo $0, sin servidores que administrar, y cada uno es una tecnología nueva para aprender. Ver [Cloud Run, Neon y Vercel](infraestructura/cloud-run-neon-vercel.md).

#### Roadmap dividido en "Núcleo" y "Extensiones opcionales"
- **Qué:** el núcleo tiene criterios de terminado por fase y las extensiones se eligen según interés.
- **Por qué:** separar lo necesario de lo deseable hace que el proyecto sea terminable.

#### Se crea `docs/estudio/`
- **Qué:** carpeta de notas por tecnología o concepto, más esta bitácora.
- **Por qué:** que el repo documente **qué** se usó y **por qué**, no solo el código.
