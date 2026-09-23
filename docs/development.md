# Development environment

How to run everything locally and test it. The full deployment details (dev
and prod) live in [deploy/README.md](../deploy/README.md).

## Startup

Bring up the separate `Infrastructure` repository first (Postgres, Zitadel,
SeaweedFS, RabbitMQ, Jaeger, see its own README for the exact command), then:

```sh
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml up -d
```

That runs Musify's own one-shot bootstrap jobs: it generates the RS256
stream-ticket keys in `deploy/keys/`, applies the EF Core migrations, creates
Musify's storage bucket, and provisions Zitadel (project + OIDC apps),
writing the client ids into `deploy/.env`. Full details, including how to run
the apps in Docker with `--profile apps`, are in
[deploy/README.md](../deploy/README.md).

## Applications

```powershell
dotnet run --project backend/Hosts/Musify.Api               # :5111
dotnet run --project backend/Hosts/Musify.StreamingGateway  # :8081
dotnet run --project backend/Hosts/Musify.Worker            # background
npm --prefix web-player run dev                       # :5173
```

## Port map

| Service | Port |
|---|---|
| Musify.Api | 5111 (`/scalar/v1`, `/openapi/v1.json`) |
| Musify.StreamingGateway | 8081 (`/health`, `/media/...`) |
| web-player | 5173 (`npm run dev`) or 3000 (container) |

Postgres, Zitadel, SeaweedFS, RabbitMQ and Jaeger are not run by this repo.
Their ports are documented in the separate `Infrastructure` repository's own
README.

DB: `musify_db`, user `postgres`/`postgres` on Infrastructure's shared
Postgres. S3 bucket: `webapi-storage` (credentials
`admin_access_key`/`admin_secret_key`, also Infrastructure's). All of these
come from `deploy/.env`.

## Migrations

The `migrate` service in `deploy/compose.yml` applies them on every `docker
compose up -d` (a no-op if the schema is already current). To run them by
hand after a schema change, against the local database:

```powershell
dotnet ef database update --project backend/Core/Musify.Infrastructure --startup-project backend/Core/Musify.Infrastructure --context Database
```

`Musify.Infrastructure` is both the migrations project and the startup
project: it has the `IDesignTimeDbContextFactory` and the `EFCore.Design`
package. The connection string is read from its user-secrets, or from the
`Database__ConnectionString` environment variable if there are no
user-secrets (which is how the `migrate` container uses it, see
`backend/Musify.Migrator.Dockerfile`).

## Stream tickets (RS256 keys)

The API signs stream tickets with the private key, and the gateway validates
them with the public one. The `keys-init` service in `deploy/compose.yml`
generates them in `deploy/keys/` if they don't exist yet; to do it by hand:

```powershell
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out deploy/keys/stream_private.pem
openssl rsa -in deploy/keys/stream_private.pem -pubout -out deploy/keys/stream_public.pem
```

- API: `StreamTicket:PrivateKeyPath`.
- Gateway: `StreamTicket:PublicKeyPath`.

## Tests

```sh
dotnet test backend/Musify.slnx
```

`backend/Tests/` (the `/Tests/` folder in the `.slnx`) has five xUnit projects:

| Project | Covers | Docker |
|---|---|---|
| `Musify.Domain.Tests` | Value object logic (`TrackAudio`/`TrackPictures`: `IsProcessed`, `IsFailed`, `IsInProgress`). Entities are mostly anemic, with no behavior of their own beyond that. | No |
| `Musify.Application.Tests` | The ~45 handlers and services in `Musify.Application` (Albums, Tracks, PlayLists, Users, Mixes). | No |
| `Musify.Infrastructure.Tests` | `Database`/migrations, `StorageService`, `StreamTicketService`, `AuditableEntityInterceptor`, `PictureService`, `SingleFlightCache`, `PlayListPresetSeeder`. | **Yes** |
| `Musify.Api.Tests` | Real HTTP endpoints (routing, auth, `ValidationFilter`, `ErrorOr`→HTTP mapping) via `WebApplicationFactory`, plus `ErrorOrHttpExtensions`/`CurrentUser`/FluentValidation validators in isolation. | **Yes** |
| `Musify.StreamingGateway.Tests` | `TicketValidator` (RS256) and `TicketValidationMiddleware` (traversal, prefix limits, ticket extraction). | No |

`Musify.Worker` has no test project: its `Program.cs` is pure DI/host wiring
(Hangfire, MassTransit, jobs). The actual logic it runs lives in
`Musify.Infrastructure`, already covered there.

### Application.Tests: why in-memory SQLite instead of mocks

`IDatabase` exposes `DbSet<T>` directly, so handlers build real LINQ
(`Where`, `Include`, `Select`...) against it. That can't be mocked with
NSubstitute, since there's no `IQueryable` behind a mock. Instead,
`TestSupport/TestDatabase.cs` is a real `DbContext` on top of in-memory
SQLite (a fresh `:memory:` connection per test, with real foreign keys,
unlike EF Core's InMemory provider, SQLite also supports transactions,
which two handlers rely on). `TestSupport/TestEntities.cs` and
`TestConfigurations.cs` build entities and configuration with sensible
defaults. `IStorageService`, `IEventBus`, etc. are mocked with NSubstitute,
since those are plain interfaces.

### Infrastructure.Tests and Api.Tests: Testcontainers

These two need Docker running. They spin up a real Postgres
(`Testcontainers.PostgreSql`) and, in `Infrastructure.Tests`, a real
SeaweedFS too (the same `chrislusf/seaweedfs:latest` image/command as
`deploy/compose.yml`, not MinIO), so `StorageService` is tested against the
same S3-compatible backend production uses, not against AWS S3. Containers
are started once per run (`ICollectionFixture`), with real EF Core
migrations applied against the ephemeral Postgres.

`Api.Tests` uses `WebApplicationFactory<Program>` against that same
Postgres, with real routing, auth, and validation, but replaces the services
that talk over the network with fakes (`IStorageService`, `IEventBus`,
`IStreamTicketService`), and swaps the real JWT auth scheme for a test
handler (see `TestSupport/FakeAuthenticationHandler.cs`) that authenticates
based on a header, with no real Zitadel needed.

Writing these tests against the real backend found and fixed two actual
bugs (not staged ones): `StorageService.RemoveFolderAsync` threw a
`NullReferenceException` against SeaweedFS (its `DeleteObjects` response
omits the `<Error>` list when there are no errors, unlike AWS S3), and
`AuditableEntityInterceptor` never updated `UpdatedAt` on a normal update
(load → modify → save) because it ran before EF had detected the pending
changes.

## Config (appsettings)

Convention: `appsettings.json` is the template, with empty or neutral
values; `appsettings.Development.json` has the real dev values; in Docker
everything is overridden with environment variables (`Section__Key`) from
`deploy/compose.yml`. To run `Musify.Api` from the IDE, the `Authentication`
values go into user-secrets (not the repo). Copy them from `deploy/.env`
once `zitadel-init` has generated them (see
[deploy/README.md](../deploy/README.md)).

Key sections: `Authentication` (Zitadel), `Database`, `MassTransit`,
`InfrastructureStorage` (S3), `ApplicationStorage` (bucket), `Track`, `PlayList`,
`UploadIntent`, `AudioTranscoder`, `Workers`, `StreamGateway` / `StreamTicket`.

## Host requirements

- .NET 10 SDK and Node 22.
- **ffmpeg** on the PATH (for the Worker's audio transcoding).
- Docker Desktop.
