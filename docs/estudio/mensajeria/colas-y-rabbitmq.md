# Colas y RabbitMQ (intro)

> **Categoría:** Mensajería
> **Fase del roadmap:** 2 · **Estado:** 📝 escrito (intro; se amplía en la Fase 2)
> **Última actualización:** 2026-09-29
> **Relacionado:** [Estilos de arquitectura](../arquitectura/estilos-de-arquitectura.md) · [ADR-0001](../../adr/0001-arquitectura.md)

## Qué es (en criollo)
Una **cola de mensajes** funciona como un buzón. Una parte del sistema (el **productor**) deja un mensaje, por ejemplo *"se confirmó la orden 123"*, y sigue con lo suyo. Otra parte (el **consumidor**, en nuestro caso un **worker**) lo retira y lo procesa **cuando puede**.

**RabbitMQ** es un **broker de mensajes**: el "correo" que recibe, guarda y reparte esos mensajes. Es open source, muy usado y habla el protocolo AMQP.

## Qué problema resuelve
Sin cola, cuando Órdenes llama a Stock directamente:
- **Si Stock está caído, la confirmación de la orden falla.**
- **Si Stock tarda, el usuario espera.**
- **Órdenes tiene que conocer a todos** los que necesitan enterarse (Stock, Facturación, Notificaciones…).

Con una cola:
- Órdenes **publica** y responde enseguida.
- Si el worker está caído, el mensaje **espera** en la cola.
- Se pueden sumar consumidores nuevos **sin tocar Órdenes**.
- Si hay mucha carga, se suman workers y se reparten los mensajes.

## Síncrono vs asíncrono
| | Síncrono (llamada directa) | Asíncrono (mensaje) |
|---|---|---|
| El que llama… | Espera la respuesta | Sigue de largo |
| Si el otro está caído | Falla | El mensaje espera |
| Consistencia | Inmediata | **Eventual** (en un rato) |
| Complejidad | Baja | Más alta: duplicados, orden, reintentos |
| Cuándo usarlo | Necesito la respuesta **ya** | Necesito que **algo pase como consecuencia** |

## Cómo lo usamos en este proyecto (Fase 2)
Primer caso de uso: **confirmar una orden → reservar y descontar stock**.

```
[API: Órdenes]                                  [Worker: Stock]
     │ 1. confirma la orden                          ▲
     │ 2. en la MISMA transacción guarda             │ 5. consume, verifica en el
     │    el evento en la tabla outbox               │    inbox que no sea duplicado,
     ▼                                               │    descuenta stock y hace ack
 [Postgres] ── 3. publicador lee el outbox ──▶ [RabbitMQ] ── 4. entrega ──┘
                                                     │
                                          falla N veces ──▶ [DLQ]
```

### ¿Por qué el outbox?
El problema es que **guardar en la base** y **publicar en RabbitMQ** son dos sistemas distintos, y no hay una transacción que abarque a los dos:
- Si guardo y **después** publico, y se cae el proceso en el medio, **se pierde el evento**.
- Si publico y **después** guardo, y falla el guardado, **se publica algo que no pasó**.

**Solución: outbox.** El evento se guarda en una tabla `outbox`, **en la misma transacción** que la orden. Un proceso aparte lee esa tabla y publica en RabbitMQ. Si RabbitMQ está caído, reintenta más tarde. El evento nunca se pierde.

### ¿Por qué el inbox (idempotencia)?
RabbitMQ garantiza entrega **al menos una vez**: un mensaje puede llegar **duplicado** (por ejemplo, si el worker procesó pero se cayó antes de confirmar). El **inbox** registra los IDs de mensaje ya procesados. Si llega uno repetido, se ignora, y así no se descuenta el stock dos veces.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Broker** | Servidor intermediario que recibe y reparte mensajes (RabbitMQ). |
| **Productor / publisher** | Quien envía el mensaje. |
| **Consumidor / consumer** | Quien lo recibe y procesa. |
| **Worker** | Proceso en segundo plano que consume mensajes. En .NET, un `BackgroundService`. |
| **Queue (cola)** | Donde esperan los mensajes hasta que alguien los consume. |
| **Exchange** | En RabbitMQ, el productor publica en un **exchange** (no en una cola directamente) y el exchange decide a qué colas va el mensaje. |
| **Binding** | Regla que conecta un exchange con una cola. |
| **Routing key** | Etiqueta del mensaje que el exchange usa para rutear (p. ej. `ordenes.confirmada`). |
| **Tipos de exchange** | `direct`: routing key exacta. `topic`: patrones como `ordenes.*`. `fanout`: a todas las colas conectadas. `headers`: según encabezados. |
| **Ack / Nack** | El consumidor confirma "lo procesé" (ack) o "falló" (nack). Sin ack, RabbitMQ lo vuelve a entregar. |
| **Prefetch** | Cuántos mensajes le entrega RabbitMQ a un consumidor antes de recibir acks. |
| **DLQ (dead letter queue)** | Cola adonde van los mensajes que fallaron demasiadas veces, para revisarlos a mano. |
| **Reintento con backoff** | Reintentar esperando cada vez más (1 s, 2 s, 4 s…). |
| **Al menos una vez** | El mensaje llega seguro, pero puede llegar repetido. Por eso hace falta idempotencia. |
| **Idempotencia** | Procesar lo mismo dos veces tiene el mismo efecto que procesarlo una vez. |
| **Outbox / inbox** | Tablas que garantizan, respectivamente, no perder eventos al publicarlos y no procesar duplicados. |
| **Evento vs comando** | Evento: *"pasó X"* (`OrdenConfirmada`), puede tener muchos oyentes. Comando: *"hacé X"* (`EmitirFactura`), tiene un destinatario. |

## Alternativas y por qué no
| Alternativa | Por qué no (en este proyecto) |
|---|---|
| Eventos solo en memoria | Se pierden si se cae el proceso, no hay reintentos reales y no se aprende a usar un broker. |
| Kafka | Es un **log de eventos** distribuido pensado para volúmenes enormes y *replay*. Es excesivo acá y más difícil de hostear gratis. |
| Azure Service Bus / Google Pub/Sub | Son gestionados y buenos, pero atan a un proveedor de nube y pueden tener costo. RabbitMQ corre igual en local (Docker) y en la nube (CloudAMQP). |
| Redis Streams | Sirve, pero RabbitMQ es el estándar más pedido para colas de trabajo. |

### Librería en .NET (se decide en la Fase 2)
| Opción | Nota |
|---|---|
| **RabbitMQ.Client** directo | Máximo aprendizaje de cómo funciona por dentro, pero hay que implementar a mano reintentos, outbox, etc. |
| **MassTransit** | Muy popular. Resuelve outbox, reintentos y sagas. La **v9 es comercial**; la v8 es open source. Verificar el estado de la licencia al decidir. |
| **Wolverine** | Open source (MIT), con outbox integrado y buena integración con EF Core. |

Una opción razonable es **arrancar con RabbitMQ.Client** para entender los conceptos y después evaluar si conviene una librería. Queda para el ADR de la Fase 2.

## Errores comunes
- **Publicar directo a RabbitMQ dentro de la transacción de la base**: se pierden eventos o se publican eventos fantasma. Por eso existe el outbox.
- **Suponer que los mensajes llegan una sola vez**: siempre hay que diseñar consumidores idempotentes.
- **Suponer que llegan en orden**: con varios consumidores o reintentos, el orden no está garantizado.
- **Reintentar para siempre**: un mensaje "venenoso" bloquea la cola. Después de N intentos va a la DLQ.
- **Mensajes gigantes**: mejor mandar IDs y datos mínimos, no el objeto entero.
- **No monitorear la DLQ**: los mensajes se acumulan sin que nadie se entere.

## Qué aprendí / dudas abiertas
_(Completar en la Fase 2.)_

## Para profundizar
- [Tutoriales oficiales de RabbitMQ (incluye .NET)](https://www.rabbitmq.com/tutorials)
- [RabbitMQ: Reliability Guide](https://www.rabbitmq.com/docs/reliability)
- Chris Richardson: [Transactional outbox](https://microservices.io/patterns/data/transactional-outbox.html)
- [CloudAMQP: plan gratuito](https://www.cloudamqp.com/plans.html) (lo usamos en la Fase 3)
