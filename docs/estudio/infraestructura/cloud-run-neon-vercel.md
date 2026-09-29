# Cloud Run, Neon y Vercel

> **Categoría:** Infraestructura
> **Fase del roadmap:** 1 (deploy mínimo) y 3 (deploy completo) · **Estado:** 📝 escrito (intro)
> **Última actualización:** 2026-09-29
> **Prerrequisito:** [Docker y Docker Compose](docker-y-compose.md)

> ⚠️ Los límites de los planes gratuitos **cambian**. Los de esta nota son orientativos: hay que verificarlos en la página de precios de cada servicio antes de cada fase.

## Qué es (en criollo)
Son tres servicios **gestionados**: vos no administrás servidores, solo subís tu app o creás tu base.

| Servicio | Qué hospeda | En una línea |
|---|---|---|
| **Google Cloud Run** | API .NET y worker (contenedores) | Le das una imagen de Docker y te da una URL HTTPS. Escala solo, incluso a cero. |
| **Neon** | PostgreSQL | Postgres "serverless": se apaga cuando nadie lo usa y se prende cuando llega una consulta. |
| **Vercel** | Frontend Next.js | Lo creó la misma empresa que hace Next.js. Conectás el repo y cada push se despliega. |

## Qué problema resuelve
- **Costo $0**: los tres tienen planes gratuitos que alcanzan para una demo de portfolio.
- **Sin administrar servidores**: nada de parches, SSL ni reinicios.
- **Demo pública**: cualquier reclutador puede entrar a una URL.
- **Webhooks**: Mercado Pago (Fase 4) necesita una **URL pública HTTPS** para avisar de los pagos. Por eso el deploy va antes.

## Cómo lo usamos en este proyecto
- **Fase 1 (deploy mínimo):**
  - la imagen de la API (construida en la CI) se despliega en Cloud Run;
  - la API se conecta a Neon;
  - el backoffice va en Vercel, apuntando a la URL de la API.
- **Fase 3 (deploy completo):**
  - se suman el worker en Cloud Run y RabbitMQ en CloudAMQP;
  - los secretos pasan a Secret Manager;
  - todo se despliega solo desde GitHub Actions.
- Links al código y al pipeline: _(completar al implementar)_.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Serverless** | No manejás servidores y pagás (o no) por uso, no por máquina encendida. |
| **Escala a cero** | Sin tráfico, no queda ninguna instancia corriendo. Ahorra plata, pero la primera request tarda más. |
| **Arranque en frío (cold start)** | Demora al levantar una instancia desde cero. En .NET puede ser de varios segundos. |
| **Instancia** | Una copia del contenedor corriendo. Cloud Run crea más si hay más tráfico. |
| **Revisión (Cloud Run)** | Cada deploy crea una revisión inmutable y se puede volver a una anterior. |
| **Branch de base (Neon)** | Copia de la base creada al instante, útil para probar migraciones sin tocar la principal. |
| **Preview deployment (Vercel)** | Cada pull request tiene su propia URL de prueba. |
| **Secret Manager** | Servicio de Google para guardar secretos (connection strings, tokens de MP) fuera del código. |
| **Alerta de presupuesto** | Aviso por mail si el gasto supera un monto. Lo configuramos en $0 o $1. |

## Puntos a tener en cuenta
- **Cloud Run pide una cuenta de facturación con tarjeta**, aunque el uso quede dentro del plan gratuito. Mitigación: alerta de presupuesto y límite bajo de instancias máximas.
- **Worker con escala a cero:** un worker que escucha RabbitMQ necesita estar prendido. Si Cloud Run lo apaga por falta de tráfico HTTP, los mensajes esperan en la cola. Opciones a decidir en la Fase 3:
  - aceptar que se procesa cuando el servicio "despierta";
  - usar **Cloud Run Jobs** + **Cloud Scheduler** para drenar la cola cada X minutos.
- **Neon suspende la base** tras unos minutos sin uso, y la primera consulta paga el arranque. La API tiene que tolerarlo con reintentos de conexión.
- **Vercel Hobby es para uso no comercial**, lo cual encaja con un portfolio.

## Alternativas y por qué no
| Alternativa | Por qué no (por ahora) |
|---|---|
| Azure App Service / Container Apps | Buena opción para .NET, pero el plan gratuito es más limitado o se basa en créditos temporales. |
| Render / Railway / Fly.io | Sus planes gratuitos cambiaron mucho y algunos ya no existen o duermen agresivamente. Son una alternativa a evaluar si Cloud Run no convence. |
| Supabase (en vez de Neon) | También es Postgres gratuito, pero trae muchas cosas extra (auth, storage) que no usamos. |
| VM propia (Oracle Always Free) | Implica administrar el servidor. Queda para la extensión de Kubernetes. |

## Errores comunes
- **Olvidar la alerta de presupuesto.**
- **Poner secretos en variables de entorno visibles del frontend.** En Next.js, lo que empieza con `NEXT_PUBLIC_` **llega al navegador**.
- **CORS:** la API tiene que permitir el dominio de Vercel.
- **Migraciones al arrancar la app**, con varias instancias a la vez: pueden correr en paralelo. Mejor correrlas como paso del pipeline.
- **No usar pooling de conexiones con Neon**: hay que usar el endpoint con pooler para no agotar conexiones.

## Qué aprendí / dudas abiertas
_(Completar en las Fases 1 y 3.)_

## Para profundizar
- [Cloud Run: documentación](https://cloud.google.com/run/docs) y [precios / plan gratuito](https://cloud.google.com/run/pricing)
- [Neon: documentación](https://neon.tech/docs)
- [Vercel: Next.js](https://vercel.com/docs/frameworks/nextjs)
- [Desplegar .NET en Cloud Run](https://cloud.google.com/run/docs/quickstarts/build-and-deploy/deploy-dotnet-service)
