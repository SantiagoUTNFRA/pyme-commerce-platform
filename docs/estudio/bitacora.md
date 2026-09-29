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
