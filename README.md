# PyME Commerce Platform

[![CI](https://github.com/SantiagoUTNFRA/pyme-commerce-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/SantiagoUTNFRA/pyme-commerce-platform/actions/workflows/ci.yml)

Plataforma SaaS multi-tenant de gestión de ventas para PyMEs argentinas: catálogo, stock, órdenes, cobros con **Mercado Pago**, factura electrónica **ARCA** e integración con **Odoo**.

Es un **proyecto de portfolio y aprendizaje**: cada decisión técnica está justificada en un ADR y cada tecnología tiene su nota de estudio.

## Estado

**Fase 0 (Fundaciones) terminada.** Esqueleto del monolito modular, Postgres con un esquema por módulo, tests de arquitectura, Docker y CI. Ver el [roadmap](docs/roadmap.md).

## Stack

- **Backend:** .NET 10 · ASP.NET Core Minimal APIs · EF Core + PostgreSQL 18
- **Arquitectura:** monolito modular con límites verificados por tests (ArchUnitNET) · hexagonal en las integraciones · RabbitMQ desde la Fase 2
- **Infraestructura:** Docker + Compose · GitHub Actions · GHCR · (Fase 1) Cloud Run + Neon + Vercel

## Correrlo en local

Requiere Docker.

```bash
docker compose up --build
curl http://localhost:8080/health           # Healthy
curl http://localhost:8080/api/catalogo/productos
```

Para desarrollar sin contenedor para la API (Postgres sí en Docker):

```bash
docker compose up -d postgres
dotnet tool restore
dotnet run --project src/Api/PymeCommerce.Api -- --migrate   # aplica las migraciones
dotnet run --project src/Api/PymeCommerce.Api                # http://localhost:5050
dotnet test
```

## Estructura

```
src/Api/PymeCommerce.Api/           host: registra los módulos (y con --migrate, migra)
src/Modules/<Modulo>/
  PymeCommerce.<Modulo>.Contracts/  API pública del módulo
  PymeCommerce.<Modulo>/            interior (internal): dominio, EF, endpoints
tests/PymeCommerce.ArchitectureTests/
docs/                               visión, roadmap, ADRs y material de estudio
```

## Documentación

- [Visión](docs/vision.md) · [Roadmap](docs/roadmap.md)
- ADRs: [0001 Arquitectura](docs/adr/0001-arquitectura.md) · [0002 Estructura de módulos y persistencia](docs/adr/0002-estructura-de-modulos-y-persistencia.md)
- [Estudio](docs/estudio/README.md): una nota por tecnología · [Bitácora](docs/estudio/bitacora.md)
