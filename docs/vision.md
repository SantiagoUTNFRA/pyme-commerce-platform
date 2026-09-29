# Visión: PyME Commerce Platform

> Estado: borrador v1 · 2026-09-29
> Relacionados: [Roadmap](roadmap.md) · [ADR-0001: Arquitectura](adr/0001-arquitectura.md) · [Estudio](estudio/README.md)

## Qué es

Una plataforma **SaaS multi-tenant** de gestión de ventas para PyMEs argentinas. Desde un mismo backoffice, cada empresa:

- administra su **catálogo** (productos, variantes, precios, alícuotas de IVA);
- controla su **stock** por depósito, con reservas y movimientos trazables;
- gestiona **órdenes** de venta de punta a punta;
- **cobra con Mercado Pago**;
- emite **factura electrónica ante ARCA** (ex AFIP);
- **sincroniza** productos, stock, clientes y comprobantes con su **ERP** (Odoo).

Es un proyecto de **portfolio y aprendizaje**: el dominio es real, pero no se ofrece como producto comercial.

## Para quién

**Personas usuarias (ficticias, para guiar el diseño):**

| Persona | Qué necesita |
|---|---|
| **Dueña/o o administrador/a de la PyME** | Ver qué vende, cuánto stock tiene y qué falta facturar, sin cargar lo mismo en cuatro sistemas. |
| **Vendedor/a** | Armar una orden rápido, mandar el link de pago y saber cuándo se cobró. |
| **Cliente final** | Pagar con Mercado Pago y recibir su factura. |

**Audiencia de portfolio:** reclutadores y entrevistadores técnicos que quieran ver decisiones de arquitectura justificadas, integraciones reales con servicios argentinos, mensajería asíncrona, tests y deploy en la nube.

## Problema

Una PyME típica trabaja hoy con herramientas sueltas:

- stock en una planilla;
- cobros en la app de Mercado Pago;
- facturas en el facturador web de ARCA;
- contabilidad en un ERP.

Cada venta implica **cargar los mismos datos varias veces**, con errores de stock, facturas que no coinciden con los cobros y poca visibilidad de lo que pasa.

## Objetivos

### De producto
- Que **una venta dispare sola** el cobro, el descuento de stock, la emisión de la factura y la sincronización con el ERP, sin carga manual.
- Que cada empresa (tenant) vea **solo sus datos**, con su propio CUIT, punto de venta y credenciales de Mercado Pago y ARCA.

### De aprendizaje
Practicar, con un caso real, tecnologías y patrones que todavía no usé. Cada uno queda explicado en [`docs/estudio/`](estudio/README.md):

- **Arquitectura:** monolito modular, puertos y adaptadores (hexagonal), eventos de dominio.
- **Mensajería:** RabbitMQ, workers, patrón outbox/inbox, idempotencia, DLQ, sagas.
- **Infraestructura:** Docker, Docker Compose, CI/CD con GitHub Actions, Cloud Run, Neon, Vercel. Como extensiones: Kubernetes, Aspire, OpenTelemetry.
- **Integraciones argentinas:** Mercado Pago (OAuth, Checkout, webhooks), ARCA (WSAA, WSFEv1, CAE, QR fiscal), Odoo.
- **Backend .NET 10:** EF Core, multi-tenancy, Identity/JWT, tests de arquitectura.

## Alcance

### Núcleo (lo que tiene que existir)

| Módulo | Responsabilidad |
|---|---|
| **Tenants / Identidad** | Alta de empresas, usuarios, roles por empresa, aislamiento de datos. |
| **Catálogo** | Productos, variantes, listas de precios, alícuotas de IVA. |
| **Stock** | Depósitos, movimientos (ingresos, egresos, ajustes), reservas por orden. |
| **Clientes** | CUIT/DNI, condición frente al IVA, datos de facturación. |
| **Órdenes** | Ciclo de vida de la orden de venta (borrador → confirmada → pagada → facturada / cancelada). |
| **Pagos** | Mercado Pago: conexión de la cuenta de cada tenant, links de pago, webhooks, reembolsos. |
| **Facturación** | Comprobantes A/B/C, notas de crédito y débito, CAE de ARCA, PDF con QR fiscal. |
| **Integración ERP** | Sincronización bidireccional con Odoo. |
| **Backoffice** | Interfaz web (Next.js) para operar todo lo anterior. |

### Extensiones posibles (si el núcleo está terminado)
- **Tienda online** pública por tenant, con checkout de Mercado Pago.
- **Punto de venta (POS)** para mostrador, con cobro por QR.
- Infraestructura avanzada: Kubernetes, .NET Aspire, observabilidad con OpenTelemetry, Keycloak, Redis.

El detalle está en el [roadmap](roadmap.md).

## Qué NO es

Estos límites existen para que el proyecto sea terminable:

- **No es un ERP.** No hace contabilidad, sueldos, compras a proveedores ni tesorería. Para eso está Odoo.
- **No es un producto en producción.** La facturación se prueba en el entorno de **homologación** de ARCA y no hay garantía de validez fiscal.
- **No es multi-país ni multi-moneda.** Solo Argentina y solo pesos (ARS).
- **No hace logística ni envíos.** Mercado Envíos y los couriers quedan afuera.
- **No es un marketplace** multi-vendedor ni una **app móvil nativa**.
- **No liquida impuestos provinciales.** Nada de percepciones ni retenciones de IIBB, ni regímenes especiales.
- **No integra otros medios de pago.** Solo Mercado Pago: nada de otros gateways, tarjetas directas ni transferencias conciliadas.
- **No está pensado para alta escala.** Está diseñado para funcionar bien con decenas de tenants, no con miles.

## Principios

1. **Multi-tenant desde el día 1.** Agregarlo después es mucho más caro que diseñarlo desde el principio.
2. **Integraciones detrás de puertos, siempre con un fake.** Se puede desarrollar y testear sin depender de que Mercado Pago, ARCA u Odoo estén disponibles.
3. **Idempotencia en todo lo que llega desde afuera.** Un webhook o un mensaje duplicado no puede cobrar, descontar ni facturar dos veces.
4. **Infraestructura a costo $0.** Solo se usan tiers gratuitos.
5. **Cada tecnología nueva se documenta.** Si entra al código, entra también a [`docs/estudio/`](estudio/README.md) y a la [bitácora](estudio/bitacora.md).
6. **Las decisiones importantes se registran como ADR**, con contexto y alternativas descartadas.

## Métricas de éxito

- Hay una **demo pública** funcionando.
- El flujo **producto → orden → pago → factura → stock descontado** se demuestra en menos de 5 minutos.
- Las decisiones clave están escritas como ADR.
- La **CI está en verde** y hay tests de dominio, de integración y de arquitectura.
- Cada tecnología usada tiene su nota en `docs/estudio/`.
