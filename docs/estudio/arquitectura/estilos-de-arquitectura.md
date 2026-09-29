# Estilos de arquitectura: monolito, modular, microservicios, hexagonal y eventos

> **Categoría:** Arquitectura
> **Fase del roadmap:** 0 · **Estado:** 📝 escrito
> **Última actualización:** 2026-09-29
> **Decisión asociada:** [ADR-0001](../../adr/0001-arquitectura.md)

## Qué es (en criollo)
"Arquitectura" suena a una sola elección, pero en realidad son **tres preguntas distintas**. Monolito, microservicios, hexagonal, onion y "por eventos" **no compiten entre sí**: cada uno responde una de esas preguntas y se pueden combinar.

| Pregunta | Opciones | Qué define |
|---|---|---|
| **1. ¿Cuántas cosas despliego?** | Monolito · Monolito modular · Microservicios | Si todo corre en **un proceso con una base** o en **N procesos con N bases** que se hablan por red. |
| **2. ¿Cómo organizo el código por dentro?** | Capas clásicas · Hexagonal · Onion · Clean | Hacia dónde apuntan las dependencias. En hexagonal, onion y clean, el **dominio no depende** de la base de datos ni de las APIs externas. |
| **3. ¿Cómo se comunican las partes?** | Llamadas directas (síncrono) · Eventos (asíncrono) | Si "Órdenes" le **pide** a "Stock" que descuente, o **avisa** `OrdenConfirmada` y Stock reacciona por su cuenta. |

Por eso "monolito modular + hexagonal + eventos" es una combinación válida. Es la que usamos.

## Qué problema resuelve
Elegir una arquitectura es elegir **qué problemas vas a tener**. Todas pueden resolver este proyecto; lo que cambia es el costo y el riesgo. Por eso se elige según los **drivers** (las condiciones reales del proyecto) y no según cuál está de moda:

- ¿Cuántas personas trabajan?
- ¿Qué necesita ser consistente al mismo tiempo?
- ¿Qué tan claros están los límites del dominio?
- ¿Cuánto cuesta la infraestructura?
- ¿Qué parte tiene que escalar distinto?

## Pregunta 1: despliegue

### Monolito (clásico)
Una aplicación, una base, sin límites internos claros.
- ✅ Simple de arrancar, de desplegar y de debuggear.
- ❌ Con el tiempo, todo termina dependiendo de todo ("big ball of mud"). Cambiar algo rompe cosas lejanas.

### Monolito modular
Una aplicación, pero dividida en **módulos con límites estrictos**. Cada módulo tiene sus tablas y una API pública, y nadie toca el interior de otro módulo.
- ✅ Tiene la simplicidad operativa del monolito: un deploy y transacciones ACID.
- ✅ Tiene el orden de los microservicios: límites claros y módulos extraíbles.
- ❌ Requiere **disciplina**: los límites se pueden violar fácil si no hay tests que lo impidan.

### Microservicios
Cada módulo es una aplicación separada, con su propia base, y se comunican por red (HTTP o mensajes).
- ✅ Despliegue y escalado independientes, y cada equipo es dueño de su servicio.
- ❌ **Todo lo que era fácil se vuelve difícil:**
  - las transacciones entre servicios necesitan sagas;
  - las fallas de red necesitan reintentos;
  - hacen falta trazas distribuidas;
  - hay N pipelines y N bases que mantener.
- 💡 Resuelven problemas de **organizaciones** (muchos equipos que se pisan) más que problemas técnicos.

### ¿Por qué monolito modular acá?
| Driver | Qué implica |
|---|---|
| Equipo de 1 persona | No existe el problema que resuelven los microservicios, pero sí pagaríamos su costo. |
| Consistencia pago + stock + factura | En un proceso con una base es una transacción. En microservicios necesitaríamos sagas con compensaciones del tipo "¿qué pasa si se cobró pero el stock falló?". |
| Límites inciertos | En un monolito, mover un límite es un refactor. En microservicios es una migración de datos entre bases. |
| Costo $0 | Una app + un worker entra en el plan gratuito. Seis servicios con seis bases, no. |
| Portfolio | Muestra criterio: saber **no** usar microservicios cuando no corresponde, pero dejarlos preparados para cuando sí. |

## Pregunta 2: organización interna

### Capas clásicas
`Controller → Service → Repository → DB`. Es simple, pero el negocio termina dependiendo de la base de datos.

### Hexagonal (puertos y adaptadores), Onion y Clean
Son **variantes de la misma idea**: el dominio (las reglas del negocio) está en el centro y **no conoce** la infraestructura.
- El dominio define un **puerto**: una interfaz como `IAutorizadorFiscal`.
- La infraestructura implementa **adaptadores**: `ArcaWsfeAdapter` (el real) y `FakeAutorizadorFiscal` (para tests).
- Las dependencias apuntan **hacia adentro**: la infraestructura depende del dominio, nunca al revés.

Las diferencias entre las tres son sobre todo de nomenclatura y de cuántos "anillos" dibujan.

### ¿Por qué hexagonal *selectivo* acá?
- **Pagos, Facturación e Integración ERP** hablan con sistemas externos (Mercado Pago, ARCA, Odoo) que se caen, cambian o no están disponibles en desarrollo. Con puertos, testeamos con fakes y cambiamos de proveedor sin tocar el negocio.
- En un **CRUD simple** (p. ej. categorías de productos), los puertos solo agregan interfaces y mapeos sin ningún beneficio. Ahí usamos algo más directo.

## Pregunta 3: comunicación

### Síncrona (llamadas directas)
Órdenes llama a `stock.Reservar(...)` y espera la respuesta.
- ✅ Simple y con resultado inmediato.
- ❌ Si Stock falla o tarda, Órdenes falla o tarda (acoplamiento temporal).

### Asíncrona (eventos)
Órdenes publica `OrdenConfirmada` y sigue. Stock lo recibe y reacciona cuando puede.
- ✅ Desacopla: Órdenes no sabe quién escucha, y si Stock está caído el evento espera en la cola.
- ❌ **Consistencia eventual**: durante un rato, la orden está confirmada pero el stock todavía no se descontó. Además hay que manejar duplicados y reintentos.

### ¿Por qué eventos con RabbitMQ acá?
Buena parte del dominio **ya es asíncrono**:
- Mercado Pago avisa del pago por webhook, no en la misma request.
- ARCA puede tardar o estar caído.
- La sincronización con Odoo es en segundo plano.

Detalle de cómo funciona en [Colas y RabbitMQ](../mensajeria/colas-y-rabbitmq.md). Entra en la **Fase 2**, cuando aparece el primer caso que lo necesita.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Driver arquitectónico** | Condición real del proyecto que empuja hacia una decisión (tamaño del equipo, costo, consistencia). |
| **Módulo** | Parte del sistema con responsabilidad propia, datos propios y una API pública. |
| **Límite (boundary)** | La "frontera" de un módulo: lo que expone hacia afuera versus lo que es interno. |
| **Acoplamiento** | Cuánto se rompe un módulo cuando cambia otro. Se busca que sea bajo. |
| **Cohesión** | Cuánto "van juntas" las cosas dentro de un módulo. Se busca que sea alta. |
| **Puerto** | Interfaz definida por el dominio para algo que necesita del mundo exterior. |
| **Adaptador** | Implementación concreta de un puerto (base de datos, API externa, fake). |
| **Consistencia eventual** | Los datos quedan consistentes "en un rato", no al instante. |
| **Transacción ACID** | Operación de base de datos que se hace completa o no se hace. |
| **Saga** | Secuencia de pasos entre servicios con acciones de compensación si algo falla. |
| **ADR** | Architecture Decision Record: documento corto que registra una decisión, su contexto y las alternativas descartadas. |

## Alternativas y por qué no
Ver la sección "Alternativas consideradas" del [ADR-0001](../../adr/0001-arquitectura.md).

## Errores comunes
- **Elegir microservicios "porque es lo que se usa".** Sin el problema organizacional que resuelven, solo suman costo.
- **Monolito modular sin tests de arquitectura.** Los límites se erosionan en semanas.
- **Compartir tablas entre módulos.** Es la forma más rápida de volver al "big ball of mud".
- **Hexagonal en todo.** Termina en tres interfaces y cuatro mapeos para guardar una categoría.
- **Eventos para todo.** Si necesitás la respuesta ya, una llamada directa es más simple y más clara.

## Qué aprendí / dudas abiertas
_(Completar a medida que avanza el proyecto.)_

## Para profundizar
- Martin Fowler: [MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html) y [Microservice Prerequisites](https://martinfowler.com/bliki/MicroservicePrerequisites.html).
- Alistair Cockburn: [Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/).
- Kamil Grzybek: [Modular Monolith Primer](https://www.kamilgrzybek.com/blog/posts/modular-monolith-primer) y el repo [modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) (en .NET).
- Michael Nygard: [Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) (el origen de los ADR).
