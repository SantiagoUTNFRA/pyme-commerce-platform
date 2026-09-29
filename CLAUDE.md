# CLAUDE.md

Plataforma SaaS multi-tenant de gestión de ventas para PyMEs argentinas: catálogo, stock, órdenes, pagos con Mercado Pago, facturación electrónica ARCA e integración con Odoo. Es un **proyecto de portfolio y de aprendizaje**, no un producto en producción.

## Leer primero
- [docs/vision.md](docs/vision.md): qué es, para quién, alcance y qué NO es.
- [docs/roadmap.md](docs/roadmap.md): fases del Núcleo con criterios de terminado, y Extensiones opcionales.
- [docs/adr/](docs/adr/): decisiones de arquitectura (ADR-0001: monolito modular + hexagonal selectivo + RabbitMQ).
- [docs/estudio/bitacora.md](docs/estudio/bitacora.md): historial de decisiones y cambios.

Antes de proponer trabajo, identificá **en qué fase del roadmap estamos** y qué ítems del criterio de terminado faltan.

## Stack y arquitectura
- **Backend:** .NET 10 LTS, **monolito modular**, PostgreSQL + EF Core con un esquema por módulo.
- **Frontend:** Next.js.
- **Hexagonal (puertos y adaptadores)** solo en Pagos, Facturación e Integración ERP. En los módulos CRUD simples, capas directas.
- **Límites entre módulos:** un módulo solo usa la API pública de otro y nunca lee sus tablas. Tests de arquitectura lo verifican.
- **Mensajería:** RabbitMQ + outbox/inbox + worker, **desde la Fase 2** (no antes).
- **Auth:** ASP.NET Core Identity + JWT. Keycloak es una extensión opcional.
- **Integraciones externas** siempre detrás de un puerto y con un adaptador fake.
- **Contenedores y deploy:**
  - Docker + Compose en local.
  - Deploy en Cloud Run + Neon + Vercel, y CloudAMQP desde la Fase 3.
  - Aspire y Kubernetes son extensiones opcionales.

## Reglas de trabajo
1. **Este proyecto es para aprender.** Explicá el **porqué** de cada decisión técnica (qué problema resuelve y qué alternativas hay), no solo el qué. Respondé en español rioplatense.
2. **Cada tecnología, patrón o concepto nuevo que entra al código se documenta** en `docs/estudio/`, siguiendo [`_plantilla.md`](docs/estudio/_plantilla.md), y se actualiza el índice en [`docs/estudio/README.md`](docs/estudio/README.md).
3. **Todo lo que se agrega, cambia o saca se registra** en [`docs/estudio/bitacora.md`](docs/estudio/bitacora.md) con fecha y porqué (entradas nuevas arriba).
4. **Una fase no está terminada** si no se cumplen sus criterios del roadmap **y** no se actualizaron el estudio y la bitácora.
5. **No agregar infraestructura sin un caso de uso real que la justifique.**
6. **Infraestructura a costo $0:** solo planes gratuitos.
7. **Las decisiones de arquitectura relevantes** se registran como ADR nuevo en `docs/adr/` (`000N-titulo.md`).
8. **Commits chicos y frecuentes:** un commit por paso verificable (build y tests en verde) y push a `origin/main`. Nada destructivo (force push, reescribir historia) sin confirmar.
9. **Idempotencia** en todo lo que entra por webhook o por cola.
