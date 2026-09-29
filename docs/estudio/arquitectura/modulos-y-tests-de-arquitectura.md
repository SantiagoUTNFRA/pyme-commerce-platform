# Módulos, límites y tests de arquitectura (ArchUnitNET)

> **Categoría:** Arquitectura · Testing
> **Fase del roadmap:** 0 · **Estado:** 📝 escrito
> **Última actualización:** 2026-09-29
> **Decisiones asociadas:** [ADR-0001](../../adr/0001-arquitectura.md) · [ADR-0002](../../adr/0002-estructura-de-modulos-y-persistencia.md)

## Qué es (en criollo)
Un **módulo** es una parte del sistema con una responsabilidad propia (Catálogo, Stock…). Tiene dos caras:
- una **API pública**: lo que otros módulos pueden usar;
- un **interior**: todo lo demás, que nadie de afuera toca.

Un **test de arquitectura** es un test automático que no prueba *qué hace* el código, sino **cómo está armado**. Por ejemplo: "nada de Stock depende del interior de Catálogo". Si alguien rompe la regla, la CI se pone en rojo.

## Qué problema resuelve
En un monolito, todo el código está "a mano": un `using` y ya podés usar cualquier clase de cualquier parte. Los límites se erosionan de a poco, con un atajo acá y otro allá, hasta llegar al *big ball of mud*.

Hay tres niveles de defensa:

| Nivel | Qué impide | Qué no impide |
|---|---|---|
| **Convención** ("no lo hagas") | Nada, en realidad | Todo |
| **Compilador** (proyectos separados + `internal`) | Usar tipos internos de otro módulo | Agregar un `<ProjectReference>` al interior de otro módulo y usar sus tipos públicos (p. ej. `CatalogoModule`) |
| **Test de arquitectura** | Cualquier dependencia de tipos entre interiores | Una dependencia que no pase por tipos (p. ej. leer las tablas de otro módulo con SQL crudo) |

Usamos los dos últimos juntos.

## Cómo lo usamos en este proyecto
- **Estructura** ([ADR-0002](../../adr/0002-estructura-de-modulos-y-persistencia.md)):
  - `PymeCommerce.Catalogo.Contracts`: [`ICatalogoApi`](../../../src/Modules/Catalogo/PymeCommerce.Catalogo.Contracts/ICatalogoApi.cs) y DTOs;
  - `PymeCommerce.Catalogo`: el interior, todo `internal` salvo [`CatalogoModule`](../../../src/Modules/Catalogo/PymeCommerce.Catalogo/CatalogoModule.cs).
- **Tests:** [`LimitesEntreModulosTests`](../../../tests/PymeCommerce.ArchitectureTests/LimitesEntreModulosTests.cs):
  1. el interior de cada módulo no depende del interior de ningún otro (un caso por cada par de módulos);
  2. los `Contracts` no dependen de ningún interior;
  3. se descubrieron al menos 2 módulos. Sin este control, un descubrimiento roto haría pasar todo sin verificar nada.
- **Descubrimiento automático** ([`Modulo.cs`](../../../tests/PymeCommerce.ArchitectureTests/Modulo.cs)): busca los `PymeCommerce.*.Contracts.dll` en la carpeta de los tests. Como el proyecto de tests referencia al host, y el host a todos los módulos, un módulo nuevo queda cubierto solo.

### La prueba en rojo
Se agregó a propósito en Stock una referencia al interior de Catálogo y una clase que usaba `CatalogoModule`. Resultado:

```
failed ...El_interior_de_un_modulo_no_depende_del_interior_de_otro(origen: "Stock", destino: "Catalogo")
  Types that reside in assembly "PymeCommerce.Stock" should not depend on any Types that reside in
  assembly "PymeCommerce.Catalogo" because Stock solo puede usar Catalogo a través de
  PymeCommerce.Catalogo.Contracts" failed:
    PymeCommerce.Stock.Violacion does depend on "PymeCommerce.Catalogo.CatalogoModule"
```

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Contracts** | Proyecto con la API pública de un módulo: interfaces, DTOs y (desde la Fase 2) eventos. |
| **`internal`** | Visibilidad de C#: el tipo solo se ve dentro de su propio ensamblado (proyecto). |
| **Ensamblado (assembly)** | El `.dll` que produce cada proyecto. ArchUnitNET analiza los ensamblados ya compilados. |
| **Dependencia de tipos** | El tipo A usa al tipo B: lo instancia, lo recibe como parámetro, hereda de él, lo llama… |
| **Regla fluida** | `Types().That()...Should()...Because(...)`: la regla se lee casi como una oración. |

## Alternativas y por qué no
| Alternativa | Por qué no |
|---|---|
| **NetArchTest.Rules** | La propone el roadmap, pero su última versión es de **mayo de 2021** (verificado en NuGet). |
| **Solo proyectos + `internal`, sin test** | No detecta el uso de la clase pública de registro de otro módulo (fue justo la violación que probamos). |
| **Analizadores de Roslyn propios** | Dan errores en el IDE mientras escribís, pero escribirlos es mucho más trabajo. |
| **Microservicios** (el límite es la red) | Ver [ADR-0001](../../adr/0001-arquitectura.md). |

## Errores comunes
- **Un test que pasa porque no verifica nada.** Si la lista de módulos queda vacía, "ningún módulo viola la regla" es cierto. Por eso el test `Se_descubren_los_modulos`.
- **No ver nunca el test en rojo.** Un test de arquitectura que nunca viste fallar no sabés si funciona.
- **Poner lógica en `Contracts`.** Tiene que ser liviano: interfaces y datos, no implementaciones.
- **Compartir entidades entre módulos.** Otro módulo recibe un DTO (`ProductoResumen`), nunca la entidad (`Producto`).

## Qué aprendí / dudas abiertas
- El compilador hace la mayor parte del trabajo, y el test cubre la brecha de la clase pública de registro.
- Duda abierta: el test **no** detecta un módulo que lea tablas de otro con SQL crudo. Se podría cubrir en la Fase 1 con usuarios de Postgres con permisos por esquema.

## Para profundizar
- [ArchUnitNET: documentación](https://archunitnet.readthedocs.io/)
- Kamil Grzybek: repo [modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd), que incluye tests de arquitectura en .NET.
