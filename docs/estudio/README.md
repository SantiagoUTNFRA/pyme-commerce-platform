# 📚 Estudio

Esta carpeta existe para que el repo no sea "solo código que no sé por qué está ahí". Cada tecnología, patrón o concepto que usamos tiene su nota: **qué es, qué problema resuelve y cómo lo usamos acá**.

- **[Bitácora](bitacora.md):** registro cronológico de qué agregamos, cambiamos o sacamos, y por qué.
- **[Plantilla](_plantilla.md):** formato que sigue cada nota nueva.

**Regla:** si algo entra al código, entra a esta carpeta. Ninguna fase del [roadmap](../roadmap.md) está terminada sin actualizar el estudio y la bitácora.

## Estados

| Ícono | Significado |
|---|---|
| 📝 | Nota escrita (puede ser introductoria y ampliarse después) |
| ⏳ | Pendiente: se escribe cuando arranca la fase que lo usa |
| 🧩 | Extensión opcional: se escribe solo si la encaramos |
| 🗑️ | Descartado: la nota explica por qué lo sacamos |

## Índice

### Arquitectura
| Tema | Estado | Fase |
|---|---|---|
| [Estilos de arquitectura: monolito, modular, microservicios, hexagonal, eventos](arquitectura/estilos-de-arquitectura.md) | 📝 | 0 |
| ADRs: cómo y por qué registrar decisiones | ⏳ | 0 |
| Módulos, límites y tests de arquitectura (NetArchTest) | ⏳ | 0 |
| Multi-tenancy: estrategias de aislamiento de datos | ⏳ | 1 |
| Máquinas de estado (ciclo de vida de la orden) | ⏳ | 2 |
| Sagas / process managers | ⏳ | 4 |
| Capa anticorrupción | ⏳ | 6 |

### Mensajería
| Tema | Estado | Fase |
|---|---|---|
| [Colas y RabbitMQ (intro)](mensajeria/colas-y-rabbitmq.md) | 📝 | 2 |
| Patrón outbox / inbox en detalle | ⏳ | 2 |
| Idempotencia | ⏳ | 2 |
| Reintentos, backoff y DLQ | ⏳ | 2 |
| Librería de mensajería elegida (MassTransit / Wolverine / RabbitMQ.Client) | ⏳ | 2 |

### Infraestructura y DevOps
| Tema | Estado | Fase |
|---|---|---|
| [Docker y Docker Compose](infraestructura/docker-y-compose.md) | 📝 | 0 |
| GitHub Actions (CI) y GHCR | ⏳ | 0 |
| [Cloud Run, Neon y Vercel](infraestructura/cloud-run-neon-vercel.md) | 📝 | 1 |
| CloudAMQP y Secret Manager | ⏳ | 3 |
| Deploy continuo (CD) | ⏳ | 3 |
| [Kubernetes (intro)](infraestructura/kubernetes-intro.md) | 📝 | 🧩 |
| Helm y k3s | 🧩 | 🧩 |
| .NET Aspire | 🧩 | 🧩 |
| OpenTelemetry y Grafana | 🧩 | 🧩 |

### Backend .NET
| Tema | Estado | Fase |
|---|---|---|
| .NET 10: qué trae y por qué no .NET 8 | ⏳ | 0 |
| EF Core y migraciones | ⏳ | 0 |
| ASP.NET Core Identity + JWT | ⏳ | 1 |
| Query filters de EF Core | ⏳ | 1 |
| Testcontainers | ⏳ | 1 |
| Workers (`BackgroundService`) | ⏳ | 2 |
| SOAP desde .NET y firma CMS con certificados | ⏳ | 5 |
| QuestPDF | ⏳ | 5 |
| Quartz.NET / Hangfire | ⏳ | 6 |
| Keycloak | 🧩 | 🧩 |
| Redis | 🧩 | 🧩 |
| SignalR | 🧩 | 🧩 |

### Integraciones argentinas
| Tema | Estado | Fase |
|---|---|---|
| IVA, condición frente al IVA y tipos de comprobante (A/B/C) | ⏳ | 2 |
| Mercado Pago: OAuth, Checkout Pro, webhooks | ⏳ | 4 |
| ARCA: WSAA, WSFEv1, CAE, QR fiscal | ⏳ | 5 |
| Odoo: modelo de datos y API externa | ⏳ | 6 |

### Frontend
| Tema | Estado | Fase |
|---|---|---|
| Next.js (App Router) para el backoffice | ⏳ | 1 |
| SSR y tienda online | 🧩 | 🧩 |

### Testing y calidad
| Tema | Estado | Fase |
|---|---|---|
| Pirámide de tests: unitarios, de integración y de arquitectura | ⏳ | 0 |
| Tests de contrato para adaptadores externos | ⏳ | 4 |
| k6: pruebas de carga | 🧩 | 🧩 |
