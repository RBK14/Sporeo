# Sporeo Backend

Modular .NET 10 backend for Sporeo fixtures: catalog administration, fixture queries, external provider sync, and Aspire orchestration.

## Structure

- `src/BuildingBlocks` — shared domain/application/infrastructure primitives (Result, MediatR behaviors, outbox)
- `src/Modules/Fixtures` — Fixtures bounded context (Api, Application, Domain, Contracts, Infrastructure, Worker)
- `src/Platform` — Aspire AppHost and ServiceDefaults
- `tests` — unit and integration tests mirroring the source layout

## Prerequisites

- .NET 10 SDK
- Docker (optional, for Aspire + SQL Server/Redis)

## Local development

```bash
cd backend
dotnet restore Sporeo.slnx
dotnet build Sporeo.slnx
dotnet test Sporeo.slnx --filter "FullyQualifiedName!~Infrastructure.Persistence.Tests"
```

Run the full stack with Aspire:

```bash
cd backend/src/Platform/Sporeo.AppHost
dotnet run
```

The AppHost starts SQL Server, Redis, a one-shot migration task (`--migrate`), then the Fixtures API and Worker.

## Configuration

Secrets and connection strings belong in user secrets or environment variables (not committed). Typical keys:

- `ConnectionStrings:fixtures-db`
- `ConnectionStrings:quartz-db`
- `ConnectionStrings:redis`
- `ExternalProviders:TheSportsDb:ApiKey`

## Health endpoints

- `GET /alive` — liveness (self check)
- `GET /health` — readiness (includes TheSportsDB and Nominatim checks when Integration is registered)

## Docker

See Dockerfiles under `src/Modules/Fixtures/Sporeo.Fixtures.Api` and `Sporeo.Fixtures.Worker`.
