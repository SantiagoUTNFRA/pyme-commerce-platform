# Roadmap

> Estado: borrador v1 · 2026-09-29
> Relacionados: [Visión](vision.md) · [ADR-0001: Arquitectura](adr/0001-arquitectura.md) · [Estudio](estudio/README.md) · [Bitácora](estudio/bitacora.md)

Las estimaciones suponen **~10 horas por semana**. Son orientativas y no hay fechas fijas.

**Regla para todas las fases:** una fase no está terminada hasta que [`docs/estudio/`](estudio/README.md) tenga una nota por cada tecnología o concepto nuevo y la [bitácora](estudio/bitacora.md) registre qué se agregó, cambió o sacó, y por qué.

---

## A. Núcleo

El núcleo es lo que tiene que existir para que el proyecto esté "terminado". Cada fase tiene un **criterio de terminado** verificable: si no se cumple, la fase sigue abierta.

### Fase 0: Fundaciones · ~3 semanas

**Objetivo:** tener el esqueleto del proyecto funcionando, con límites entre módulos que se hacen cumplir con tests desde el primer día.

**Entregables**
- Solución .NET 10 con el esqueleto del monolito modular. Cada módulo es un proyecto con su API pública y su interior privado (ver [ADR-0001](adr/0001-arquitectura.md)).
- PostgreSQL + EF Core, con un esquema de base por módulo y migraciones.
- Tests de arquitectura que fallan si un módulo usa el interior de otro.
- Dockerfile multi-stage y `docker-compose.yml` con API + Postgres.
- CI con GitHub Actions: build, tests y publicación de la imagen en GitHub Container Registry (GHCR).
- ADR-0001 revisado.

**Tecnologías nuevas:** Docker, Docker Compose, migraciones de EF Core, ArchUnitNET, GitHub Actions, GHCR.

**Estado:** ✅ terminada el 2026-09-29. Ver la [bitácora](estudio/bitacora.md) y el [ADR-0002](adr/0002-estructura-de-modulos-y-persistencia.md).

**Criterio de terminado**
- [x] `docker compose up` levanta la API + Postgres con las migraciones aplicadas.
- [x] La CI está en verde en `main`.
- [x] Un test de arquitectura **falla** si un módulo referencia el interior de otro.

---

### Fase 1: Tenants + Catálogo + Stock + deploy mínimo · ~5 semanas

**Objetivo:** que se pueda operar el catálogo y el stock de más de una empresa, con los datos aislados entre ellas, y que ya esté accesible en una URL pública.

**Entregables**
- **Tenants e identidad:** ASP.NET Core Identity + JWT, roles por tenant (Admin, Vendedor) y resolución del tenant en cada request.
- **Aislamiento de datos:** filtros globales de EF Core por `TenantId`.
- **Catálogo:** productos, variantes, alícuotas de IVA (21 %, 10,5 %, 27 %, exento) y listas de precios.
- **Stock:** depósitos y stock modelado como un **registro de movimientos** (el saldo se calcula a partir de ellos), más reservas.
- **Backoffice básico** en Next.js: login, ABM de productos y consulta y ajuste de stock.
- **Deploy mínimo:**
  - API en **Google Cloud Run**, Postgres en **Neon** y backoffice en **Vercel**.
  - Deploy manual o semiautomático.
  - Alerta de presupuesto en $0.

**Tecnologías nuevas:** Identity/JWT, multi-tenancy, query filters de EF Core, Testcontainers, Cloud Run, Neon, Vercel.

**Criterio de terminado**
- [ ] Un test de integración con Postgres real (Testcontainers) demuestra que un tenant **no puede ver** los datos de otro.
- [ ] Desde el backoffice **en la URL pública** se da de alta un producto con stock inicial.
- [ ] La facturación de Google Cloud muestra $0.

---

### Fase 2: Clientes + Órdenes + mensajería · ~4–5 semanas

**Objetivo:** construir el primer caso de uso **asíncrono real**: confirmar una orden descuenta el stock mediante mensajería. Recién acá entra RabbitMQ, porque hay un problema concreto que lo justifica.

**Entregables**
- **Clientes:** CUIT/DNI con validación y condición frente al IVA (Responsable Inscripto, Monotributo, Consumidor Final, Exento).
- **Órdenes:** máquina de estados (borrador → confirmada → pagada → facturada / cancelada).
- **Mensajería:**
  - Al confirmar una orden se guarda un evento en el **outbox** (misma transacción).
  - Un proceso lo publica en **RabbitMQ**.
  - Un **worker** lo consume, reserva y descuenta stock.
  - **Inbox** para descartar mensajes duplicados.
  - Reintentos con backoff y **DLQ** (cola de mensajes muertos).
- RabbitMQ y el worker agregados al `docker-compose.yml`.
- Decisión (ADR) sobre la librería de mensajería: MassTransit v8, Wolverine o RabbitMQ.Client directo. Ver [riesgos](#riesgos).

**Tecnologías nuevas:** RabbitMQ, patrón outbox/inbox, workers (`BackgroundService`), DLQ, idempotencia.

**Criterio de terminado**
- [ ] Confirmar una orden descuenta el stock **de forma asíncrona**.
- [ ] Con RabbitMQ apagado, el evento **no se pierde**: queda en el outbox y se publica cuando RabbitMQ vuelve.
- [ ] Un mensaje duplicado **no** descuenta el stock dos veces.
- [ ] Un mensaje que falla N veces termina en la DLQ y se puede reprocesar.

---

### Fase 3: Deploy completo · ~2 semanas

**Objetivo:** que todo lo de la Fase 2 funcione en la nube y que se despliegue solo.

**Entregables**
- Worker desplegado en Cloud Run.
- RabbitMQ gestionado en **CloudAMQP** (plan gratuito).
- Secretos en Google Secret Manager.
- **Deploy continuo:** un push a `main` despliega API, worker y frontend. Las migraciones corren en el pipeline.
- Decisión sobre la escala a cero del worker. Ver [riesgos](#riesgos).

**Tecnologías nuevas:** CloudAMQP, Secret Manager, CD con GitHub Actions.

**Criterio de terminado**
- [ ] Un push a `main` despliega todo sin pasos manuales.
- [ ] El flujo "confirmar orden → descontar stock" funciona en la URL pública.
- [ ] La facturación de Google Cloud sigue en $0.

---

### Fase 4: Pagos con Mercado Pago · ~4 semanas

**Objetivo:** cobrar las órdenes con Mercado Pago, cada tenant con su propia cuenta.

**Entregables**
- **OAuth de Mercado Pago:** cada tenant conecta su cuenta y la plataforma guarda los tokens por tenant.
- **Link de pago (Checkout Pro)** generado desde el backoffice para una orden.
- **Webhooks:** validación de la firma `x-signature`, procesamiento idempotente y consulta del pago a la API de MP antes de confiar en la notificación.
- **Saga** "pago aprobado → orden pagada".
- Reembolsos totales.

**Tecnologías nuevas:** OAuth 2.0 con un tercero, webhooks, sagas o process managers.

**Criterio de terminado**
- [ ] Un pago en el **sandbox** de MP marca la orden como pagada.
- [ ] Recibir el mismo webhook dos veces **no** duplica el pago.
- [ ] Un webhook con firma inválida se rechaza.

---

### Fase 5: Facturación electrónica ARCA · ~4–5 semanas

**Objetivo:** que una orden pagada termine en un comprobante electrónico con CAE.

**Entregables**
- **Modelo de comprobantes:** Factura A, B y C, más notas de crédito y débito. El tipo se decide según la condición frente al IVA del emisor y del receptor.
- Puntos de venta y numeración correlativa por tipo de comprobante.
- **Adaptador fake** de ARCA, para desarrollar sin certificado.
- **Adaptador real** (cuando esté el certificado de homologación):
  - **WSAA:** login con firma CMS para obtener Token y Sign.
  - **WSFEv1:** `FECompUltimoAutorizado` y `FECAESolicitar`.
- **PDF** del comprobante con el **QR fiscal**.
- Manejo de errores y reintentos: si ARCA no responde, el comprobante queda pendiente y se reintenta.

**Tecnologías nuevas:** servicios SOAP desde .NET, firma CMS/PKCS#7 con certificados X.509, QuestPDF.

**Criterio de terminado**
- [ ] Una orden pagada genera una factura con CAE (fake, o de homologación si ya está el certificado).
- [ ] Se genera el PDF con el QR fiscal.
- [ ] Una nota de crédito anula la factura.

---

### 🏁 Hito: MVP demostrable · ~1 semana

**Objetivo:** que cualquiera pueda ver el flujo completo funcionando.

**Entregables**
- Datos de demo con 2 tenants precargados.
- README con el flujo guiado y usuarios de prueba.
- Video corto de la demo.

**Criterio de terminado**
- [ ] En la URL pública, el flujo se recorre en menos de 5 minutos: crear producto → crear orden → pagar en el sandbox de MP → factura con CAE y PDF → stock descontado.

---

### Fase 6: Integración con Odoo · ~3–4 semanas

**Objetivo:** sincronizar la plataforma con un ERP real.

**Entregables**
- Odoo Community en Docker (solo local).
- **Capa anticorrupción:** traduce entre el modelo de Odoo y el nuestro, para que el dominio no dependa de Odoo.
- Sincronización de productos, stock, clientes y facturas.
- Jobs programados (Quartz.NET o Hangfire) para la sincronización periódica.
- Detección y registro de conflictos (por ejemplo, el mismo producto modificado de ambos lados).
- Evaluación de la API externa de Odoo: XML-RPC/JSON-RPC o la nueva API JSON-2 de las versiones recientes.

**Tecnologías nuevas:** API externa de Odoo, capa anticorrupción, Quartz.NET o Hangfire.

**Criterio de terminado**
- [ ] Un producto creado en la plataforma aparece en Odoo.
- [ ] Un ajuste de stock hecho en Odoo se refleja en la plataforma.
- [ ] Los conflictos quedan registrados y visibles en el backoffice.

---

**Totales estimados:** MVP demostrable en **~5,5 meses**, núcleo completo en **~6,5 meses**.

---

## B. Extensiones opcionales

Se encaran **después del núcleo**, en cualquier orden y según interés. Son independientes entre sí.

| Extensión | Objetivo | Qué se aprende | Terminado cuando… |
|---|---|---|---|
| **.NET Aspire** | Orquestar el entorno local desde C#, con dashboard y service discovery | AppHost, integraciones de Aspire, dashboard | `dotnet run` en el AppHost levanta todo y el dashboard muestra los recursos |
| **Kubernetes** | Correr la plataforma en un clúster: primero local con k3d, después con Helm (opcional: k3s en la VM gratuita de Oracle Cloud) | Pods, Deployments, Services, Ingress, ConfigMaps/Secrets, probes, Helm | `helm install` levanta la plataforma en k3d y sobrevive a que se borre un pod |
| **Observabilidad** | Ver trazas, métricas y logs de punta a punta | OpenTelemetry, Grafana Cloud (plan free) | Una orden se puede seguir en una traza desde la API hasta el worker |
| **Keycloak** | Reemplazar Identity por un proveedor OIDC externo | OIDC, Keycloak organizations = tenants | El login pasa por Keycloak y el tenant sale del token |
| **Redis** | Caché e idempotencia distribuida | Redis, Upstash (plan free) | El catálogo se sirve desde caché y se invalida al modificarlo |
| **Tienda online** | Tienda pública por tenant | Next.js SSR, carrito, Checkout Bricks de MP | Un cliente final compra y paga en la tienda de un tenant |
| **POS** | Venta de mostrador | QR dinámico de MP, SignalR, apertura y cierre de caja | Una venta de mostrador se cobra por QR y la pantalla se actualiza sola |
| **Pulido del portfolio** | Dejar el proyecto presentable | k6 (pruebas de carga), hardening, diagramas C4 | README con diagramas C4, resultados de k6 y video actualizado |

---

## Riesgos

| Riesgo | Mitigación |
|---|---|
| **Cloud Run pide una cuenta de facturación con tarjeta**, aunque el uso quede dentro del plan gratuito. | Alertas de presupuesto en $0 y límite de instancias máximas. |
| **Cloud Run apaga los contenedores sin tráfico** (escala a cero), y un worker apagado no consume mensajes de RabbitMQ. | Se decide en la Fase 3. Opción 1: aceptar que se procesa cuando el servicio "despierta" (alcanza para una demo). Opción 2: drenar la cola con Cloud Run Jobs + Cloud Scheduler. |
| **Límites de los planes gratuitos:** Neon suspende la base tras inactividad y CloudAMQP free tiene topes de conexiones y mensajes. | Verificar los límites vigentes al llegar a cada fase y diseñar para tolerar arranques en frío. |
| **Caídas del entorno de homologación de ARCA.** | Adaptador fake como alternativa y reintentos con estado "pendiente". |
| **Cambios en las APIs de Mercado Pago u Odoo.** | Adaptadores aislados detrás de puertos, con tests de contrato. |
| **Licencias de librerías:** MassTransit v9 y MediatR pasaron a licencia comercial. | En la Fase 2, decidir entre MassTransit v8, Wolverine o RabbitMQ.Client directo, verificando el estado de las licencias en ese momento. |
| **Odoo corre solo en local**, porque no hay hosting gratuito razonable para él. | En la demo pública, la sincronización se muestra con el adaptador fake o en video. |
