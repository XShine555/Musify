# Projects

The .NET projects live under `backend/` (solution `Musify.slnx`, folders
`Core/`, `Hosts/` and `Tests/`); the web client lives under `web-player/`.

## Musify.Api  (`:5111`)

The public REST API (Minimal APIs, .NET 10). The thin public face of the system.

- **Endpoints**: `AlbumEndpoints`, `ConfigEndpoints`, `GenreEndpoints`, `LikeEndpoints`, `MixEndpoints`, `PlayListEndpoints`, `TrackEndpoints`, `UserEndpoints`. Every endpoint returns `IResult`; routes use `{id:guid}` (sub-resources `{trackId:guid}`) and `{id:long}` / `{userId:long}` for user ids; list endpoints bind `PageQuery` (`pageNumber`, `pageSize` 1..100). The three `/cover` routes share `CoverEndpoint.Stream`.
- **Validation**: FluentValidation validators in `Validators/`; every limit lives in `Validators/Limits.cs` (the web-player copies the same values) and the upload allow-lists in `Validators/Uploads.cs`.
- **Authentication**: JWT Bearer against Zitadel (`Authentication/`), with a `JwtBearerEventsHandler` that syncs the user on every validated token.
- **`CurrentUser`**: an abstraction bound as an endpoint parameter (`IBindableFromHttpContext`) that exposes the basic claims (`Id` as `long?`, `RequiredId`, name, etc.) without reading `ClaimsPrincipal` by hand.
- **Issues**: presigned upload URLs (via `IStorageService`) and **stream tickets** (via `IStreamTicketService`, RS256).
- **OpenAPI + Scalar** for docs/testing; OAuth2 (PKCE) set up for logging in from Scalar.
- **CORS** wide open, but **only in Development** (for the local frontend).
- Translates `ErrorOr` into HTTP (ProblemDetails) in `ErrorOrHttpExtensions`.

**Purpose**: keep the public surface (authorization + metadata) separate from processing and byte-serving.

## Musify.Domain + Musify.Application + Musify.Infrastructure

The core, split into three projects (clean architecture):

### Domain
Entities (`Entities/`) and value objects/enums (`ValueObjects/`: `ProcessingStatus`, `LifeCycleStatus`, `UploadIntentStatus`, …). No infrastructure dependencies.

### Application
Use cases (CQRS with Mediator):
- One folder per feature (`Albums/`, `Genres/`, `Likes/`, `Mixes/`, `Pictures/`, `PlayLists/`, `Tracks/`, `Users/`, …): each `<UseCase>.cs` holds the command/query record and its handler; DTOs live in `Responses/`.
- `Contracts/` → interfaces that infrastructure implements (`IDatabase`, `IStorageService`, `IAudioTranscoderService`, `IPictureService`, `IEventBus`, `IStreamTicketService`).
- `Configuration/` → typed options (Track, PlayList, Album, Mix, ApplicationStorage, UploadIntent, Playback, StreamGateway…), each ending in `Configuration`, bound and validated at startup with `AddValidatedOptions`; `Pictures.cs` holds the shared picture routes and sizes.
- `Shared/` → cross-cutting helpers: `StorageKey.Combine`, `ImageSize(s)`, `AppErrors`, `PageRequest` / `PaginatedResponse<T>`, `OwnershipExtensions.FindOwnedAsync`, `TextNormalizer`.
- `Services/` → `UploadIntentIssuer` / `UploadIntentValidator` (upload intents) and `TrackStreamIssuer` (stream tickets).
- `AddApplication(configuration)` registers all of the above; the API and the Worker both call it.

**Why**: business logic doesn't know about EF, S3, or RabbitMQ, only interfaces. Shared by both the API and the Worker.

### Infrastructure
Concrete implementations:
- `Persistence/` → `Database` (EF Core/Npgsql), migrations, and `ModelConfiguration.Apply` (the entity model, shared with the in-memory test database).
- `Services/` → `StorageService` (AWSSDK.S3 against SeaweedFS), `AudioTranscoderService` (ffmpeg → `.m4a`), `PictureService` (ImageSharp), `StreamTicketService` (RS256 signing).
- `MassTransit/` → consumers, activities, routing slips and sagas (see [media-processing.md](media-processing.md)).
- `Jobs/` → the recurring Hangfire jobs (`DailyMixGenerationJob`, `ListeningHistoryCleanupJob`, `UploadIntentExpirationJob`, `TempUploadsCleanupJob`) and `RecurringJobsRegistrar`.

## Musify.StreamingGateway  (`:8081`)

A reverse proxy (ASP.NET + **YARP**) that serves audio (`.m4a`) from SeaweedFS. Validates the stream ticket (RS256, public key) and, if the requested object falls under the authorized prefix, forwards it to the filer. It references neither Application nor Infrastructure and registers its own options. The bucket in the default YARP transform is the placeholder `CHANGE_ME` (compose overrides it; `appsettings.Development.json` sets `webapi-storage`).

**Why a separate project**: it scales with byte traffic independently of the API, has a minimal surface (it only knows how to validate a JWT and proxy), and lets SeaweedFS stay on a private network. See [streaming.md](streaming.md).

## Musify.Worker

A background Worker Service that consumes MassTransit events and runs the heavy processing: audio transcoding to `.m4a` (ffmpeg), thumbnail generation (ImageSharp), file transfers within the bucket, and the recurring Hangfire jobs (daily mixes, listening-history cleanup, upload-intent expiration, temporary upload cleanup).

**Why separate**: CPU/IO-heavy work shouldn't happen inside an HTTP request; it scales and restarts independently. (Previously named `Musify.PictureWorker`; renamed once it started doing more than just images.)

## web-player  (`:5173` in dev, `:3000` in a container)

The web client (SvelteKit 2 + Svelte 5 + Tailwind 4). The OIDC session lives
on the server (an encrypted cookie); backend calls go out from the server
with the access token, and uploads go straight to the presigned S3 URLs.

## Tests

`backend/Tests/` (the `/Tests/` folder in the `.slnx`) has five xUnit projects:
`Musify.Domain.Tests`, `Musify.Application.Tests` (in-memory SQLite, no
Docker), `Musify.Infrastructure.Tests` (Testcontainers: real Postgres +
SeaweedFS; also covers the MassTransit activities, routing slips, sagas and
jobs), `Musify.Api.Tests` (Testcontainers) and `Musify.StreamingGateway.Tests`. Details in
[development.md](development.md#tests).

## Other folders

- **`deploy/`**: Musify's own Docker Compose stacks (dev and prod). The
  shared infrastructure (Postgres, Zitadel, SeaweedFS, RabbitMQ, Jaeger)
  lives in the separate `Infrastructure` repo instead. See
  [../deploy/README.md](../deploy/README.md).
- **`docs/`**: this documentation.
