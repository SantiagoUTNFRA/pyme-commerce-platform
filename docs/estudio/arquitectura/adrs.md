# ADRs: cómo y por qué registrar decisiones

> **Categoría:** Arquitectura
> **Fase del roadmap:** 0 · **Estado:** 📝 escrito
> **Última actualización:** 2026-09-29

## Qué es (en criollo)
Un **ADR** (*Architecture Decision Record*) es un documento corto, de una o dos páginas, que registra **una** decisión de arquitectura importante:
- qué se decidió;
- en qué contexto;
- qué alternativas se descartaron y por qué;
- qué consecuencias trae, buenas y malas.

Se numeran (`0001`, `0002`…), viven en el repo junto al código y **no se reescriben**: si una decisión cambia, se escribe un ADR nuevo que reemplaza al anterior.

## Qué problema resuelve
- **"¿Por qué está hecho así?"** Seis meses después nadie se acuerda, y el código muestra *qué* se hizo, no *por qué*.
- **Volver a discutir lo mismo.** Si las alternativas descartadas están escritas con su razón, no hace falta repetir la discusión.
- **Cambiar sin romper la lógica original.** Si cambia el contexto (p. ej. aparece un segundo equipo), el ADR dice qué supuesto dejó de valer.
- **Portfolio:** en una entrevista, "acá está por qué *no* usé microservicios" vale más que el código.

## Cómo lo usamos en este proyecto
- Viven en [`docs/adr/`](../../adr/), con el formato `000N-titulo.md`.
- [ADR-0001](../../adr/0001-arquitectura.md): monolito modular, hexagonal selectivo y RabbitMQ. Revisado al cerrar la Fase 0.
- [ADR-0002](../../adr/0002-estructura-de-modulos-y-persistencia.md): cómo se implementa en .NET (proyectos `Contracts`, esquema por módulo, migrador, ArchUnitNET).
- Próximo ADR previsto: la librería de mensajería (Fase 2).
- **Estructura que usamos:** Estado · Fecha · Contexto · Decisión · Alternativas consideradas · Consecuencias (positivas y negativas).

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Estado** | Propuesto → Aceptado → (Reemplazado por ADR-000N / Deprecado). |
| **Contexto** | Las fuerzas que empujan la decisión: equipo, costo, plazos, restricciones técnicas. |
| **Consecuencias** | Qué se gana **y qué se paga**. Un ADR sin consecuencias negativas probablemente no pensó bien la decisión. |
| **Reemplazo (supersede)** | Un ADR nuevo invalida uno viejo. El viejo queda, marcado como reemplazado, para no perder la historia. |

## Alternativas y por qué no
| Alternativa | Por qué no |
|---|---|
| Wiki o Notion | Se desincroniza del código y no queda en el historial de git. |
| Comentarios en el código | Explican una línea, no una decisión que atraviesa todo el sistema. |
| Un documento de arquitectura gigante | Nadie lo mantiene actualizado. Los ADRs son chicos e inmutables. |

## Errores comunes
- **Escribir ADRs para todo**, como elegir el nombre de una variable. Van solo las decisiones **caras de revertir**.
- **Editar un ADR aceptado para cambiar la decisión.** Se escribe uno nuevo que lo reemplaza. Agregar una sección de revisión, como hicimos en el ADR-0001, sí está bien.
- **Omitir las alternativas.** Son la parte más valiosa.

## Qué aprendí / dudas abiertas
_(Espacio personal.)_

## Para profundizar
- Michael Nygard: [Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [adr.github.io](https://adr.github.io/): plantillas y herramientas.
