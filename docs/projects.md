# Projects

The .NET projects live under `backend/` (solution `Musify.slnx`, folders
`Core/`, `Hosts/` and `Tests/`); the web client lives under `web-player/`.

## Musify.Api  (`:5111`)

The public REST API (Minimal APIs, .NET 10). The thin public face of the system.

- **Endpoints**: `UserEndpoints`, `TrackEndpoints`, `PlayListEndpoints`.
- **Authentication**: JWT Bearer against Zitadel (`Authentication/`), with a `JwtBearerEventsHandler` that syncs the user on every validated token.
- **`CurrentUser`**: an abstraction bound as an endpoint parameter (`IBindableFromHttpContext`) that exposes the basic claims (`Id` as `long?`, `RequiredId`, name, etc.) without reading `ClaimsPrincipal` by hand.
- **Issues**: presigned upload URLs (via `IStorageService`) and **stream tickets** (via `IStreamTicketService`, RS256).
- **OpenAPI + Scalar** for docs/testing; OAuth2 (PKCE) set up for logging in from Scalar.
- **CORS** wide open, but **only in Development** (for the local frontend).
- Translates `ErrorOr` into HTTP in `ResultHttpExtensions`.

**Purpose**: keep the public surface (authorization + metadata) separate from processing and byte-serving.

## Musify.Domain + Musify.Application + Musify.Infrastructure

The core, split into three projects (clean architecture):

### Domain
Entities (`Entities/`) and value objects/enums (`ValueObjects/`: `ProcessingStatus`, `LifeCycleStatus`, `UploadIntentStatus`, …). No infrastructure dependencies.

### Application
Use cases (CQRS with Mediator):
- `Tracks/`, `PlayLists/`, `Users/` → `Commands`, `Queries`, `Handlers`, `Responses`.
- `Contracts/` → interfaces that infrastructure implements (`IDatabase`, `IStorageService`, `IAudioTranscoderService`, `IPictureService`, `IEventBus`, `IStreamTicketService`).
- `Configuration/` → typed options (Track, PlayList, ApplicationStorage, UploadIntent…) bound and validated at startup.
- `Shared/` → cross-cutting helpers (`StorageKey.Combine`, `ImageSize`).
- `Pagination/` → `PaginatedResponse<T>`.

**Why**: business logic doesn't know about EF, S3, or RabbitMQ, only interfaces. Shared by both the API and the Worker.

### Infrastructure
Concrete implementations:
- `Persistence/` → `Database` (EF Core/Npgsql), migrations.
- `Services/` → `StorageService` (AWSSDK.S3 against SeaweedFS), `AudioTranscoderService` (ffmpeg → `.m4a`), `PictureService` (ImageSharp), `StreamTicketService` (RS256 signing).
- `MassTransit/` → consumers, activities, and routing slips (see [media-processing.md](media-processing.md)).
- `Jobs/` → `UploadIntentExpirationJob`, `TemporalUploadsCleanUpJob`.

## Musify.StreamingGateway  (`:8081`)

A reverse proxy (ASP.NET + **YARP**) that serves audio (`.m4a`) from SeaweedFS. Validates the stream ticket (RS256, public key) and, if the requested object falls under the authorized prefix, forwards it to the filer.

**Why a separate project**: it scales with byte traffic independently of the API, has a minimal surface (it only knows how to validate a JWT and proxy), and lets SeaweedFS stay on a private network. See [streaming.md](streaming.md).

## Musify.Worker

A background Worker Service that consumes MassTransit events and runs the heavy processing: audio transcoding to `.m4a` (ffmpeg), thumbnail generation (ImageSharp), file transfers within the bucket, and the upload-intent expiration/cleanup jobs.

**Why separate**: CPU/IO-heavy work shouldn't happen inside an HTTP request; it scales and restarts independently. (Previously named `Musify.PictureWorker`; renamed once it started doing more than just images.)

## web-player  (`:5173` in dev, `:3000` in a container)

The web client (SvelteKit 2 + Svelte 5 + Tailwind 4). The OIDC session lives
on the server (an encrypted cookie); backend calls go out from the server
with the access token, and uploads go straight to the presigned S3 URLs.

## Tests

`backend/Tests/` (the `/Tests/` folder in the `.slnx`) has five xUnit projects:
`Musify.Domain.Tests`, `Musify.Application.Tests` (in-memory SQLite, no
Docker), `Musify.Infrastructure.Tests` and `Musify.Api.Tests` (Testcontainers:
real Postgres + SeaweedFS), and `Musify.StreamingGateway.Tests`. Details in
[development.md](development.md#tests).

## Other folders

- **`deploy/`**: Musify's own Docker Compose stacks (dev and prod). The
  shared infrastructure (Postgres, Zitadel, SeaweedFS, RabbitMQ, Jaeger)
  lives in the separate `Infrastructure` repo instead. See
  [../deploy/README.md](../deploy/README.md).
- **`docs/`**: this documentation.
