# Docker y Docker Compose

> **Categoría:** Infraestructura
> **Fase del roadmap:** 0 · **Estado:** 📝 escrito (intro; se amplía al implementarlo)
> **Última actualización:** 2026-09-29

## Qué es (en criollo)
**Docker** empaqueta una aplicación junto con **todo lo que necesita para correr** (runtime de .NET, librerías, configuración) en una **imagen**. Esa imagen corre igual en tu Mac, en la CI y en la nube. Se terminó el "en mi máquina anda".

**Docker Compose** es un archivo (`docker-compose.yml`) que describe **varios contenedores que trabajan juntos**: la API, Postgres y, más adelante, RabbitMQ y el worker. Con `docker compose up` se levantan todos, conectados entre sí.

## Qué problema resuelve
- **Entornos distintos:** sin Docker, cada máquina necesita instalar la versión correcta de .NET, Postgres, RabbitMQ…
- **"En mi máquina anda":** la imagen que probás local es la misma que se despliega.
- **Onboarding:** cualquier persona clona el repo, corre `docker compose up` y tiene todo andando.
- **Deploy:** Cloud Run (nuestro hosting) **ejecuta imágenes de contenedor**. Sin imagen, no hay deploy.

## Contenedor vs máquina virtual
| | Máquina virtual (VM) | Contenedor |
|---|---|---|
| Qué virtualiza | Hardware completo, con su propio sistema operativo | Solo el proceso; comparte el kernel del host |
| Tamaño | GBs | MBs |
| Arranque | Minutos | Segundos |
| Aislamiento | Muy fuerte | Fuerte, pero comparte kernel |

En Mac, Docker Desktop corre una VM Linux liviana por debajo, porque los contenedores necesitan un kernel Linux.

## Cómo lo usamos en este proyecto
- **Fase 0:**
  - un `Dockerfile` multi-stage para la API;
  - `docker-compose.yml` con API + Postgres;
  - la CI construye la imagen y la publica en **GHCR** (GitHub Container Registry, gratis).
- **Fase 1:** esa misma imagen se despliega en **Cloud Run**.
- **Fase 2:** se suman RabbitMQ y el worker al compose.
- Links al código: _(completar al implementar)_.

### Dockerfile multi-stage (idea)
Se usan **dos etapas**:
1. **build:** imagen grande con el SDK de .NET, que compila y publica.
2. **runtime:** imagen chica que solo tiene el runtime de ASP.NET y copia lo publicado.

El resultado es una imagen final mucho más liviana y sin compiladores, así que también tiene menos superficie de ataque.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Imagen** | "Molde" inmutable: sistema de archivos + app + configuración. Se construye con `docker build`. |
| **Contenedor** | Una imagen **ejecutándose**. De una imagen se pueden levantar muchos contenedores. |
| **Dockerfile** | Receta para construir una imagen (`FROM`, `COPY`, `RUN`, `ENTRYPOINT`…). |
| **Capa (layer)** | Cada instrucción del Dockerfile genera una capa cacheada. El orden importa para aprovechar la caché. |
| **Registry** | "Repositorio" de imágenes: Docker Hub, GHCR, Google Artifact Registry. |
| **Tag** | Versión de una imagen (`api:1.0.3`, `api:sha-abc123`). |
| **Volumen** | Almacenamiento que sobrevive a que se borre el contenedor (p. ej. los datos de Postgres). |
| **Red (network)** | Compose crea una red donde los contenedores se encuentran **por nombre de servicio**: la API se conecta a `postgres:5432`, no a `localhost`. |
| **Puerto publicado** | `5000:8080` significa que el puerto 5000 de tu Mac apunta al 8080 del contenedor. |
| **Variables de entorno** | La forma estándar de configurar un contenedor (connection strings, claves). |
| **Healthcheck** | Comando que Docker ejecuta para saber si el contenedor está sano. |

## Alternativas y por qué no
| Alternativa | Por qué no (por ahora) |
|---|---|
| Instalar todo en la máquina | No es reproducible y no sirve para Cloud Run. |
| Podman | Compatible con Docker, pero Docker tiene más documentación y ejemplos. Se puede probar después. |
| .NET Aspire en vez de Compose | Es una herramienta muy buena para desarrollo local, pero agrega conceptos encima. Queda como [extensión](../../roadmap.md#b-extensiones-opcionales). |
| Kubernetes | Orquesta contenedores en producción. No lo necesitamos para Cloud Run. Ver [Kubernetes (intro)](kubernetes-intro.md). |

## Errores comunes
- **Usar `localhost` dentro de un contenedor** para hablar con otro. Hay que usar el nombre del servicio del compose.
- **Guardar datos sin volumen**: se pierden al recrear el contenedor.
- **Copiar todo antes de `dotnet restore`**: rompe la caché de capas y cada build tarda más. Primero se copian los `.csproj`, se hace restore y después se copia el resto.
- **Meter secretos en la imagen.** Van como variables de entorno o en un gestor de secretos, nunca en el Dockerfile.
- **Olvidar el `.dockerignore`**: se copian `bin/`, `obj/` y `node_modules/` a la imagen.
- **Suponer que `depends_on` espera a que Postgres esté listo.** Solo espera a que arranque el contenedor. Hace falta un healthcheck + `condition: service_healthy`.

## Qué aprendí / dudas abiertas
_(Completar al implementar la Fase 0.)_

## Para profundizar
- [Docker: Get started](https://docs.docker.com/get-started/)
- [Multi-stage builds](https://docs.docker.com/build/building/multi-stage/)
- [Compose file reference](https://docs.docker.com/reference/compose-file/)
- [Imágenes de contenedor de .NET](https://learn.microsoft.com/dotnet/core/docker/introduction)
