# GitHub Actions (CI) y GHCR

> **Categoría:** Infraestructura
> **Fase del roadmap:** 0 (CI) y 3 (CD) · **Estado:** 📝 escrito
> **Última actualización:** 2026-09-29
> **Prerrequisito:** [Docker y Docker Compose](docker-y-compose.md)

## Qué es (en criollo)
**CI (integración continua)** significa que, con cada push, un servidor compila el código y corre los tests de forma automática. Si algo se rompe, te enterás en minutos y no una semana después.

**GitHub Actions** es el servicio de CI de GitHub. Describís los pasos en un YAML dentro de `.github/workflows/` y GitHub los ejecuta en máquinas virtuales descartables (*runners*).

**GHCR (GitHub Container Registry)** es el lugar donde GitHub guarda imágenes de Docker, igual que Docker Hub. La imagen queda asociada al repo.

## Qué problema resuelve
- **"En mi máquina pasa":** la CI compila en una máquina limpia, con el SDK de `global.json`.
- **Olvidarse de correr los tests** antes de pushear.
- **Criterio de la Fase 0:** "CI en verde en `main`".
- **Deploy:** Cloud Run (Fase 1) necesita una imagen publicada en un registry. La CI la construye y la publica: nunca se sube una imagen construida en una laptop.
- **Portfolio:** el check verde en GitHub es lo primero que ve alguien que entra al repo.

## Cómo lo usamos en este proyecto
[`.github/workflows/ci.yml`](../../../.github/workflows/ci.yml) tiene tres jobs:

```
push / PR ──▶ [Build y tests] ──┐
          └─▶ [docker compose up] ─┴─▶ [Publicar imagen en GHCR]   (solo push a main)
```

1. **Build y tests:** `setup-dotnet` con `global.json`, restore, build en Release y `dotnet test`. Incluye los tests de arquitectura.
2. **docker compose up:** levanta Postgres + migrador + API y hace `curl /health`. Es el criterio 1 de la Fase 0, **verificado en cada push**. Si falla, imprime los logs.
3. **Publicar imagen:** solo en `main` y solo si los otros dos pasaron.
   - Construye con Buildx.
   - Hace login en GHCR con el `GITHUB_TOKEN`.
   - Publica `ghcr.io/santiagoutnfra/pyme-commerce-api` con dos tags: `sha-<commit>` y `latest`.

**Imagen publicada:** `docker pull ghcr.io/santiagoutnfra/pyme-commerce-api:latest`

### Decisiones
- **Permisos mínimos:** por defecto el token solo puede leer (`contents: read`). Únicamente el job que publica pide `packages: write`.
- **Tag por SHA:** `latest` va cambiando; `sha-abc123` identifica exactamente qué código tiene la imagen. Cloud Run va a desplegar por SHA.
- **Los PRs no publican:** compilan y testean, pero no suben imágenes.
- **`concurrency`:** si pusheás dos veces seguidas, la corrida vieja se cancela.
- **Caché `type=gha`:** las capas de Docker se guardan entre corridas, así el restore no se repite si no cambiaron los `.csproj`.
- **Repo público:** en repos públicos, los minutos de Actions no tienen límite.

## Conceptos clave / glosario
| Término | Qué significa |
|---|---|
| **Workflow** | El archivo YAML. Se dispara con eventos (`on: push`, `pull_request`). |
| **Job** | Grupo de pasos que corre en un runner. Los jobs corren en paralelo salvo que tengan `needs`. |
| **Step** | Un comando (`run:`) o una action (`uses:`). |
| **Action** | Paso reutilizable publicado por otros (p. ej. `actions/checkout@v7`). |
| **Runner** | La VM que ejecuta el job (`ubuntu-latest`). Se descarta al terminar. |
| **`GITHUB_TOKEN`** | Token temporal que GitHub crea para cada corrida, con los permisos que declara el workflow. |
| **Buildx** | El builder moderno de Docker (BuildKit): caché remota, multiplataforma. |
| **CI vs CD** | CI: compilar y testear en cada cambio. CD: además, desplegar solo. El CD entra en la Fase 3. |

## Alternativas y por qué no
| Alternativa | Por qué no |
|---|---|
| GitLab CI / Azure Pipelines / CircleCI | Igual de capaces, pero el código está en GitHub y Actions viene integrado. |
| Docker Hub | Limita los pulls anónimos y es otra cuenta más. GHCR usa el mismo login que GitHub. |
| Google Artifact Registry | Lo podemos sumar en la Fase 1 si Cloud Run lo requiere. Para la Fase 0 alcanza con GHCR, gratis. |

## Errores comunes
- **Mayúsculas en el nombre de la imagen:** GHCR solo acepta minúsculas, y el usuario es `SantiagoUTNFRA`. `docker/metadata-action` las convierte a minúsculas.
- **Olvidarse `permissions: packages: write`:** el push a GHCR falla con `denied`.
- **Secretos en el YAML:** van en *Settings → Secrets* y se leen con `${{ secrets.X }}`.
- **Actions sin versión fija** (`@main`): pueden cambiar sin aviso. Usamos la versión mayor (`@v7`); lo más estricto sería fijar el SHA del commit.
- **Paquete privado por defecto:** en cuentas personales, una imagen nueva en GHCR puede nacer privada. Se cambia en *Package settings → Change visibility*. En nuestro caso heredó la visibilidad pública del repo (verificado con un pull sin credenciales).

## Qué aprendí / dudas abiertas
_(Espacio personal.)_

## Para profundizar
- [GitHub Actions: documentación](https://docs.github.com/actions)
- [Publicar imágenes de Docker](https://docs.github.com/actions/publishing-packages/publishing-docker-images)
- [Working with the Container registry](https://docs.github.com/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
- [docker/build-push-action](https://github.com/docker/build-push-action)
