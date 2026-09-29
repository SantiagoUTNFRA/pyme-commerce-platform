# ADR-0001: Monolito modular, hexagonal selectivo y mensajería con RabbitMQ

- **Estado:** Aceptado
- **Fecha:** 2026-09-29 · **Revisado:** 2026-09-29 (cierre de la Fase 0)
- **Detalle de implementación:** [ADR-0002: Estructura de módulos y persistencia](0002-estructura-de-modulos-y-persistencia.md)
- **Material de estudio:** [Estilos de arquitectura](../estudio/arquitectura/estilos-de-arquitectura.md) · [Colas y RabbitMQ](../estudio/mensajeria/colas-y-rabbitmq.md)

## Contexto

Hay que elegir la arquitectura de una plataforma de ventas para PyMEs ([visión](../vision.md)) con estas características:

- **Equipo:** una sola persona, con ~10 h/semana.
- **Dominio:** catálogo, stock, órdenes, pagos, facturación e integración con un ERP. Varios pasos tienen que ser **consistentes entre sí**: una orden pagada no puede quedar sin stock reservado ni sin factura.
- **Integraciones externas pesadas:** Mercado Pago (webhooks), ARCA (SOAP con certificados, entorno de homologación inestable) y Odoo. Estas integraciones son **asíncronas por naturaleza**.
- **Límites entre módulos todavía inciertos.** Por ejemplo, no está claro si Precios forma parte de Catálogo o es un módulo propio.
- **Infraestructura con costo $0.**
- **Objetivo de aprendizaje:** practicar mensajería real (colas, workers) y patrones de integración.

"Arquitectura" en realidad involucra **tres decisiones independientes**, que se combinan entre sí:

1. **Despliegue:** ¿un proceso con una base, o N procesos con N bases?
2. **Organización interna del código:** ¿hacia dónde apuntan las dependencias?
3. **Comunicación entre partes:** ¿llamadas directas o eventos?

## Decisión

### 1. Despliegue: **monolito modular**

- Una sola aplicación desplegable (API) más un proceso worker, que comparte el código.
- **Una base de datos PostgreSQL con un esquema por módulo.** Un módulo no lee las tablas de otro.
- Cada módulo expone una **API pública** (contratos: interfaces, DTOs, eventos). Todo lo demás es interno.
- Los límites se **hacen cumplir con tests de arquitectura** que corren en la CI.

### 2. Organización interna: **hexagonal selectivo**

- Los módulos con integraciones externas (**Pagos, Facturación, Integración ERP**) usan **puertos y adaptadores**:
  - el dominio define la interfaz (p. ej. `IAutorizadorFiscal`);
  - la infraestructura la implementa (`ArcaWsfeAdapter`, `FakeAutorizadorFiscal`).
- Los módulos más simples (p. ej. las partes CRUD de Catálogo) usan **capas simples**, sin ceremonia de más.

### 3. Comunicación: **llamadas directas dentro de una transacción + eventos con RabbitMQ entre módulos**

- **Sincrónico:** cuando un módulo necesita una respuesta inmediata de otro, lo llama en proceso a través de su API pública.
- **Asincrónico:** cuando un módulo tiene que *reaccionar* a lo que pasó en otro (p. ej. `OrdenConfirmada` → descontar stock), se usa un evento:
  - se guarda en una tabla **outbox**, en la misma transacción que el cambio;
  - un publicador lo envía a **RabbitMQ**;
  - un **worker** lo consume;
  - una tabla **inbox** garantiza que un mensaje duplicado se procese una sola vez.
- **Desde la Fase 2.** La mensajería entra en la Fase 2 del [roadmap](../roadmap.md), con el primer caso de uso que la justifica. En las Fases 0 y 1 no hay eventos entre módulos.

## Alternativas consideradas

### Microservicios (descartada por ahora)
- **A favor:** despliegue y escalado independientes por servicio, y aislamiento de fallas.
- **En contra, para este contexto:**
  - Resuelven problemas de **organizaciones con varios equipos**, y acá hay una sola persona.
  - La consistencia entre pagos, stock y facturas pasaría a necesitar **sagas distribuidas** con compensaciones en todos los casos.
  - Multiplican la infraestructura: N bases, N pipelines, trazas distribuidas y versionado de contratos, todo en tiers gratuitos limitados.
  - Con límites todavía inciertos, **mover un límite** implica migrar datos entre bases, en vez de hacer un refactor.

### Monolito "clásico" en capas, sin módulos (descartada)
- Es simple al principio, pero con el tiempo cualquier parte termina dependiendo de cualquier otra ("big ball of mud").
- No deja preparada la extracción futura de un módulo.
- No aprovecha el objetivo de aprender a diseñar límites.

### Hexagonal / Clean / Onion en **todos** los módulos (descartada)
- Las tres son variantes de la misma idea: el dominio no depende de la infraestructura.
- Aplicarla a un CRUD de categorías agrega interfaces y mapeos sin ningún beneficio.
- Se usa donde hay integraciones externas, que es donde paga su costo.

### Eventos solo en memoria, sin broker (descartada)
- Es más simple, pero:
  - se pierden eventos si el proceso se cae;
  - no hay reintentos ni DLQ reales;
  - no cumple el objetivo de aprender mensajería con un broker.
- Con el outbox + RabbitMQ, el diseño aguanta caídas y queda listo para mover un consumidor a otro proceso.

## Criterios para extraer un módulo a microservicio

Extraer un módulo a un servicio propio se reconsidera si aparece **alguno** de estos casos:

- Un módulo necesita **escalar distinto** que el resto. Ejemplo: la tienda online recibe 100 veces más tráfico que el backoffice.
- Un módulo necesita **desplegarse con otro ritmo** o por otro equipo.
- Un módulo tiene **requisitos de disponibilidad o de seguridad distintos**. Ejemplo: aislar las credenciales de ARCA.
- Un módulo usa una **tecnología incompatible** con el resto.

Como los módulos ya se comunican por contratos y eventos, la extracción consistiría en:

1. mover el módulo a su propio proyecto desplegable;
2. mover su esquema a su propia base;
3. reemplazar las llamadas en proceso por HTTP o mensajes.

## Consecuencias

**Positivas**
- Un solo deploy (más el worker): infraestructura mínima y compatible con el plan gratuito.
- Transacciones ACID dentro de un mismo módulo, y consistencia eventual solo donde tiene sentido.
- Las integraciones se testean con fakes, sin depender de MP, ARCA u Odoo.
- Los límites están definidos y verificados, así que la extracción futura es posible.

**Negativas / costos**
- **Disciplina:** hay que respetar los límites aunque "sería más fácil" hacer un join entre esquemas. Los tests de arquitectura la hacen cumplir solo en parte.
- **Complejidad del outbox/inbox:** tablas, publicador, limpieza y monitoreo.
- **Un bug en un módulo puede tirar el proceso entero**, porque no hay aislamiento de fallas entre módulos.
- **Consistencia eventual:** entre confirmar una orden y ver el stock descontado pasan milisegundos o segundos, y la UI lo tiene que contemplar.

## Revisión: cierre de la Fase 0 (2026-09-29)

Con el esqueleto implementado, la decisión **se mantiene sin cambios**. Lo que se aprendió al implementarla:

- **"API pública" se concretó** como un proyecto `Contracts` por módulo, con el interior `internal`. Detalle en el [ADR-0002](0002-estructura-de-modulos-y-persistencia.md).
- **Los tests de arquitectura funcionan como red de seguridad.** Una violación a propósito (Stock usando `CatalogoModule`) hizo fallar el test con un mensaje que dice qué tipo depende de cuál.
- **El compilador cubre la mayor parte del límite, pero no todo.** El interior de un módulo necesita al menos una clase pública para que el host lo registre, y esa clase se podría usar desde otro módulo. Esa es exactamente la brecha que cubre el test.
- **Sigue abierto:** el worker, el outbox y RabbitMQ entran en la Fase 2, como estaba previsto.
