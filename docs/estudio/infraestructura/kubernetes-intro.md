# Kubernetes (intro)

> **Categoría:** Infraestructura
> **Fase del roadmap:** 🧩 Extensión opcional · **Estado:** 📝 escrito (intro)
> **Última actualización:** 2026-09-29
> **Prerrequisito:** [Docker y Docker Compose](docker-y-compose.md)

## Qué es (en criollo)
Docker sabe correr **un** contenedor en **una** máquina. **Kubernetes** (abreviado **K8s**) es un **orquestador**: administra **muchos contenedores en muchas máquinas**.

Vos le decís *"quiero 3 copias de la API, accesibles en tal dominio"* y Kubernetes se encarga de que eso se cumpla **siempre**:
- si un contenedor se cae, lo levanta de nuevo;
- si una máquina muere, mueve los contenedores a otra;
- si desplegás una versión nueva, reemplaza las copias de a una sin cortar el servicio.

La idea central es que **describís el estado deseado** (en archivos YAML) y Kubernetes trabaja todo el tiempo para que el estado real coincida.

## Qué problema resuelve
Con Docker Compose en un servidor, si el servidor se cae, se cae todo. Tampoco hay manera simple de escalar a varias máquinas ni de actualizar sin cortar. Kubernetes resuelve:
- **Alta disponibilidad:** varias réplicas en varias máquinas.
- **Autocuración (self-healing):** reinicia lo que falla.
- **Escalado:** más o menos réplicas según la carga.
- **Despliegues sin corte:** rolling updates y rollback.
- **Descubrimiento de servicios y balanceo de carga.**

## ¿Por qué es una extensión y no parte del núcleo?
- **Cloud Run ya hace mucho de esto por nosotros**: escala, reinicia y balancea. Es "Kubernetes gestionado" en un sentido: por debajo corre sobre Knative, que está construido sobre Kubernetes.
- Un clúster de Kubernetes gestionado (GKE, AKS, EKS) **no es gratis**.
- Kubernetes tiene **muchos conceptos nuevos**. Conviene primero entender bien los contenedores (Fase 0) y el deploy (Fases 1 y 3).
- Aun así, **es muy pedido en el mercado**. Por eso está como extensión:
  1. primero **local con k3d** (Kubernetes liviano dentro de Docker, gratis);
  2. después con **Helm**;
  3. opcionalmente, **k3s** en la VM gratuita de Oracle Cloud.

## Cómo lo usaríamos en este proyecto (extensión)
1. Levantar un clúster local con **k3d**.
2. Escribir manifiestos para la API, el worker, Postgres y RabbitMQ: Deployments, Services, Ingress, ConfigMaps y Secrets.
3. Agregar **probes** que usen los health checks de la API.
4. Empaquetar todo en un **Helm chart**.
5. (Opcional) Desplegar en **k3s** en una VM gratuita.

## Conceptos clave / glosario
| Término | Qué significa | Equivalente aproximado en Compose |
|---|---|---|
| **Cluster** | Conjunto de máquinas (nodos) administradas por Kubernetes | Tu máquina con Docker |
| **Node** | Una máquina (física o virtual) del clúster | — |
| **Pod** | Unidad mínima: uno o más contenedores que viven juntos | Un contenedor |
| **Deployment** | "Quiero N réplicas de este pod, con esta imagen". Maneja actualizaciones y rollback | Un `service` con `replicas` |
| **ReplicaSet** | Lo que usa el Deployment por debajo para mantener N pods | — |
| **Service** | Nombre y dirección estable para acceder a un grupo de pods (los pods cambian de IP) | El nombre del servicio en la red de Compose |
| **Ingress** | Regla de entrada HTTP desde afuera: dominio y ruta → Service | `ports:` publicados |
| **ConfigMap** | Configuración no sensible | `environment:` |
| **Secret** | Configuración sensible (se guarda codificada; hay que protegerla aparte) | `.env` |
| **Namespace** | Separación lógica dentro del clúster (p. ej. `dev`, `prod`) | Proyectos de Compose distintos |
| **Liveness / readiness probe** | "¿Estás vivo?" (si no, reiniciar) y "¿estás listo para recibir tráfico?" | `healthcheck:` |
| **PersistentVolume** | Almacenamiento que sobrevive a los pods | `volumes:` |
| **kubectl** | CLI para hablar con el clúster | `docker compose` |
| **Helm** | "Gestor de paquetes" de Kubernetes: plantillas de manifiestos con valores configurables | — |
| **k3s / k3d** | k3s es un Kubernetes liviano; k3d lo corre dentro de Docker para desarrollo local | — |

## Alternativas y por qué no
| Alternativa | Por qué no (para el núcleo) |
|---|---|
| GKE / AKS / EKS | Son pagos, o sus créditos gratuitos se terminan. |
| Docker Swarm | Más simple, pero casi no se usa en la industria. |
| Solo Cloud Run | **Es lo que usamos en el núcleo.** Kubernetes queda para aprender. |

## Errores comunes
- **Usar Kubernetes antes de necesitarlo.** Agrega mucha complejidad operativa.
- **Correr bases de datos en Kubernetes sin saber lo que se hace.** En la práctica suelen usarse bases gestionadas.
- **Tratar los Secrets como si fueran seguros por defecto.** Solo están codificados en base64, no cifrados.
- **No definir readiness probes**: llega tráfico a pods que todavía no están listos.
- **No poner límites de CPU y memoria**: un pod puede comerse el nodo entero.

## Qué aprendí / dudas abiertas
_(Completar si se encara la extensión.)_

## Para profundizar
- [Kubernetes: conceptos básicos](https://kubernetes.io/docs/concepts/) (documentación oficial)
- [Kubernetes Basics tutorial](https://kubernetes.io/docs/tutorials/kubernetes-basics/)
- [k3d](https://k3d.io/) y [k3s](https://k3s.io/)
- [Helm: Quickstart](https://helm.sh/docs/intro/quickstart/)
