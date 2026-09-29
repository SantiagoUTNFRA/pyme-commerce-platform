# Pirámide de tests: unitarios, de integración y de arquitectura

> **Categoría:** Testing
> **Fase del roadmap:** 0 (arquitectura), 1 (integración con Testcontainers) · **Estado:** 📝 escrito (se amplía en la Fase 1)
> **Última actualización:** 2026-09-29

## Qué es (en criollo)
La **pirámide de tests** es una guía sobre **cuántos tests de cada tipo** conviene tener:

```
          /  E2E  \         pocos: lentos y frágiles (navegador, todo levantado)
        / integración \     algunos: código + base real / HTTP real
      /   unitarios     \   muchos: rápidos, sin I/O, prueban reglas de negocio
```

Al costado de la pirámide están los **tests de arquitectura**: no prueban comportamiento sino estructura, y son baratos como un unitario.

## Qué problema resuelve
- **Solo tests E2E:** tardan minutos, fallan por cosas ajenas (red, timing) y, cuando fallan, no dicen dónde está el problema.
- **Solo unitarios:** todo pasa en verde, pero la query real contra Postgres está rota.
- **Sin tests de arquitectura:** los límites entre módulos se erosionan sin que nadie lo note.

## Cómo lo usamos en este proyecto
| Tipo | Herramienta | Desde | Estado |
|---|---|---|---|
| Arquitectura | ArchUnitNET | Fase 0 | ✅ [`PymeCommerce.ArchitectureTests`](../../../tests/PymeCommerce.ArchitectureTests/) |
| Smoke (el entorno levanta) | `docker compose up` + `curl /health` en la CI | Fase 0 | ✅ [`ci.yml`](../../../.github/workflows/ci.yml) |
| Unitarios de dominio | xUnit | Fase 1 | ⏳ cuando haya reglas de negocio (stock, precios) |
| Integración con Postgres real | xUnit + Testcontainers | Fase 1 | ⏳ primer caso: aislamiento entre tenants |
| Contrato de adaptadores externos | — | Fase 4 | ⏳ |

### xUnit v3 y Microsoft.Testing.Platform
- **xUnit v3** es la versión actual del framework. Cada proyecto de tests es un **ejecutable** (`<OutputType>Exe</OutputType>`), así que se puede correr directo, además de con `dotnet test`.
- **Microsoft.Testing.Platform (MTP)** es el runner nuevo de .NET y reemplaza a VSTest. xUnit v3 4.x lo usa, y por eso [`global.json`](../../../global.json) tiene `"test": { "runner": "Microsoft.Testing.Platform" }`.
- Con MTP ya no hacen falta `Microsoft.NET.Test.Sdk` ni `xunit.runner.visualstudio`. La plantilla `dotnet new xunit` del SDK todavía genera xUnit v2, así que lo migramos a mano.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Fact / Theory** | En xUnit: test sin parámetros / test parametrizado con varios casos (`MemberData`, `InlineData`). |
| **Smoke test** | Prueba mínima de que "prende": no verifica lógica, solo que el sistema arranca y responde. |
| **Test double / fake** | Implementación de mentira de una dependencia (p. ej. `FakeAutorizadorFiscal`). |
| **Testcontainers** | Librería que levanta un contenedor real (Postgres) para cada corrida de tests. |
| **Flaky test** | Test que a veces pasa y a veces no, sin cambios en el código. |

## Alternativas y por qué no
| Alternativa | Por qué no |
|---|---|
| NUnit / MSTest | Igual de válidos. xUnit es el más usado en proyectos open source de .NET. |
| xUnit v2 | Sigue funcionando, pero la v3 es la versión mantenida. |
| Base en memoria de EF (InMemory) para integración | No se comporta como Postgres (transacciones, constraints, SQL). Testcontainers usa el Postgres real. |

## Errores comunes
- **Testear la implementación y no el comportamiento:** los tests se rompen con cada refactor.
- **Tests que dependen del orden** o de datos que dejó otro test.
- **No ver nunca un test en rojo:** no sabés si verifica algo.

## Qué aprendí / dudas abiertas
_(Espacio personal.)_

## Para profundizar
- Martin Fowler: [The Practical Test Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html)
- [xUnit v3](https://xunit.net/docs/getting-started/v3/whats-new)
- [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
