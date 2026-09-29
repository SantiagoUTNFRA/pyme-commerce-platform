# .NET 10 y la estructura de la solución

> **Categoría:** Backend
> **Fase del roadmap:** 0 · **Estado:** 📝 escrito
> **Última actualización:** 2026-09-29

## Qué es (en criollo)
**.NET 10** es la versión de la plataforma sobre la que corre todo el backend. Es **LTS** (*Long Term Support*): Microsoft la mantiene con parches durante 3 años, hasta noviembre de 2028.

Alrededor del código hay unos archivos de configuración que valen para toda la solución y evitan repetir lo mismo en cada proyecto:

| Archivo | Para qué sirve |
|---|---|
| [`global.json`](../../../global.json) | Fija la versión del **SDK**, para que tu Mac, la CI y Docker compilen con el mismo. También elige el runner de tests. |
| [`Directory.Build.props`](../../../Directory.Build.props) | Propiedades de MSBuild que heredan **todos** los `.csproj`: framework, nullable, warnings como errores, analizadores. |
| [`Directory.Packages.props`](../../../Directory.Packages.props) | **Central Package Management (CPM)**: las versiones de NuGet se declaran una vez. Los `.csproj` referencian paquetes **sin versión**. |
| [`PymeCommerce.slnx`](../../../PymeCommerce.slnx) | La solución en el formato nuevo (XML). Reemplaza al `.sln` clásico, que era ilegible y generaba conflictos de merge. |
| [`dotnet-tools.json`](../../../dotnet-tools.json) | Herramientas locales del repo (p. ej. `dotnet-ef`) con versión fija. Se restauran con `dotnet tool restore`. |
| [`.editorconfig`](../../../.editorconfig) | Estilo de código y severidad de los analizadores, por carpeta. |

## Qué problema resuelve
- **Soporte:** .NET 8 deja de tener soporte el 10/11/2026. Arrancar un proyecto nuevo sobre él era arrancar con fecha de vencimiento. Ver la [bitácora](../bitacora.md).
- **"En mi máquina compila":** sin `global.json`, la CI puede usar otro SDK con otras reglas de compilación.
- **Versiones desparejas:** sin CPM, con 8 proyectos es fácil terminar con dos versiones de EF Core en la misma app.
- **Calidad desde el día 1:** con `TreatWarningsAsErrors` + `AnalysisLevel=latest-recommended`, los warnings no se acumulan. Arreglar 3 warnings hoy es fácil; 300 dentro de un año, no.

## Cómo lo usamos en este proyecto
- `Directory.Build.props` define `net10.0`, `Nullable`, `ImplicitUsings`, warnings como errores y los analizadores recomendados. Por eso los `.csproj` quedan casi vacíos.
- `Directory.Packages.props` usa **transitive pinning** (`CentralPackageTransitivePinningEnabled`). Npgsql trae EF Core ≥ 10.0.4 y `EF.Design` trae 10.0.12; con el pinning, **toda** la solución usa 10.0.12.
- `global.json`:
  - `rollForward: latestFeature` acepta un SDK 10.0.x más nuevo que 10.0.300, pero nunca uno 11;
  - `test.runner: Microsoft.Testing.Platform` (ver [Pirámide de tests](../testing/piramide-de-tests.md)).
- `.editorconfig`: en `tests/` se permite `_` en los nombres de método (regla CA1707), para que los tests se lean como oraciones.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **LTS / STS** | Long Term Support (3 años, versiones pares) / Standard Term Support (2 años, versiones impares). |
| **SDK vs runtime** | El SDK compila (incluye el compilador y `dotnet build`); el runtime solo ejecuta. La imagen de Docker final lleva solo el runtime. |
| **Feature band** | El "centenar" del SDK: 10.0.**1**xx, 10.0.**3**xx… Cada banda puede traer cambios en las herramientas. |
| **MSBuild** | El motor de build de .NET. Los `.csproj` y `.props` son archivos de MSBuild. |
| **Dependencia transitiva** | Un paquete que no referenciás vos, sino uno de tus paquetes. |
| **Analizadores (CAxxxx)** | Reglas de calidad que corren al compilar (nombres, rendimiento, seguridad). |

## Alternativas y por qué no
| Alternativa | Por qué no |
|---|---|
| .NET 8 | Termina su soporte en noviembre de 2026. |
| .NET 9 (STS) | No es LTS y su soporte también termina en noviembre de 2026, igual que .NET 8. |
| `.sln` clásico | El `.slnx` es el formato por defecto del SDK 10: es más corto y legible. |
| Versiones en cada `.csproj` | Se desincronizan con facilidad. |

## Errores comunes
- **Olvidarse de `dotnet tool restore`** después de clonar: `dotnet ef` "no existe".
- **Poner una versión en un `PackageReference`** con CPM activo: da error. La versión va en `Directory.Packages.props`.
- **Silenciar un warning con `NoWarn` global** en vez de entender por qué aparece.

## Qué aprendí / dudas abiertas
_(Espacio personal.)_

## Para profundizar
- [Novedades de .NET 10](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview)
- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [global.json](https://learn.microsoft.com/dotnet/core/tools/global-json)
- [Política de soporte de .NET](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
