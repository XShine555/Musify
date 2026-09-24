# Architecture

## Principles

- **Clean architecture / layers** in `backend/`: the domain and application layers don't depend on infrastructure; infrastructure implements their interfaces (`Application/Contracts`).
- **CQRS**: every operation is a `Command` or `Query` with its own handler (Mediator). Handlers that can fail return `ErrorOr<T>` (built with `AppErrors`: NotFound, Forbidden, Conflict, Validation; `Unauthorized` only for "not signed in"), which the API translates into HTTP status codes and ProblemDetails with `ToHttpResult()`.
- **Heavy bytes never touch the API**: uploads use presigned URLs (client → S3) and playback goes through a dedicated reverse proxy (gateway → S3). The API only moves metadata and issues permissions.
- **Async processing**: creating a track doesn't transcode it on the spot. It publishes an event, and a worker does the heavy lifting, with compensation (MassTransit sagas/routing slips).

## Components

| Component | Responsibility | Why it's separate |
|---|---|---|
| **Musify.Api** | REST API (users, tracks, albums, playlists, likes, mixes, genres), authorizes requests, issues presigned URLs and stream tickets | A thin entry point, it doesn't process media or serve bytes |
| **Domain/Application/Infrastructure** | Domain + use cases + shared infrastructure (EF, S3, MassTransit, ffmpeg) | Logic shared between the API and the Worker |
| **StreamingGateway** | Reverse proxy (YARP) that validates the ticket and serves the audio (`.m4a`) from SeaweedFS | Scales with byte traffic independently of the API, and keeps SeaweedFS private behind it |
| **Worker** | Consumes MassTransit events (transcodes audio to `.m4a`, generates thumbnails, deletes entities) and runs the recurring Hangfire jobs | Keeps CPU/IO-heavy work off the HTTP request path |
| **PostgreSQL** *(shared, runs in the separate `Infrastructure` repo)* | Metadata (users, tracks, albums, playlists, upload intents, listening history) and the Hangfire job store | n/a |
| **RabbitMQ** *(shared, runs in Infrastructure)* | Event queue + transactional outbox | Decouples creation from processing |
| **SeaweedFS** *(shared, runs in Infrastructure)* | Object storage (S3 + filer) | Stores originals and derivatives (`.m4a` audio, thumbnails) |
| **Zitadel** *(shared, runs in Infrastructure)* | Identity (OIDC/OAuth2) | Login and JWT issuance |

## Main flows

### 1. Uploading a track
1. The client requests `POST /tracks/upload-urls`. The API creates **UploadIntents** (reservations) and returns **presigned PUT URLs** (image + audio).
2. The client uploads the files **directly to SeaweedFS** using those URLs (never through the API).
3. The client calls `POST /tracks` with the `intentId`s. The API validates the intents, creates the `Track` (owned by the user through `OwnerUserId`), and publishes `CreateTrackResourcesEvent`. Albums and playlists follow the same pattern with `POST /albums/upload-picture` / `POST /playlists/upload-picture` and `POST /albums` / `POST /playlists`.
4. The **Worker** consumes the event: moves the originals into place, generates thumbnails, transcodes the audio to `.m4a`, and updates the track's status.

See [media-processing.md](media-processing.md) and [storage.md](storage.md) for details.

### 2. Playback (streaming)
1. The client requests `GET /tracks/{id}/stream` (authenticated, or anonymous when `Playback:AllowAnonymousListening` is on). The API records the listen (`StartListeningCommand`) and issues the ticket. The API authorizes it and returns `{ manifestUrl, ticket }` (a URL to the `.m4a` plus an RS256 ticket scoped to that track's folder).
2. The player opens the audio URL with `?t=<ticket>` appended (or an `X-Stream-Ticket` header) and downloads it via HTTP range requests.
3. The **StreamingGateway** validates the ticket and forwards the request to the SeaweedFS filer. The bytes flow storage → client.

See [streaming.md](streaming.md).

### 3. Authentication
1. The client logs in via OIDC against **Zitadel** and gets an access token (JWT).
2. Musify.Api validates the JWT's signature, issuer and audience.
3. On every validated token, the local user is **synced** (name and picture created or updated). This is just-in-time provisioning.

See [authentication.md](authentication.md).

## Data model (entities)

- **User**: `Id` is Zitadel's numeric `sub` (`long`), plus a name and `ProfilePictureUrl`. **UserFollow** links follower and followed.
- **Track**: owned by `OwnerUserId`; `Pictures` (`TrackPictures`) and `Audio` (`TrackAudio`, with `FolderName` = the transcoded `.m4a`'s folder) are owned types with their own processing status; `LifeCycleStatus` (Active, Removing, Failed). **TrackTag** holds its genres, **TrackLike** the likes, **ListeningHistory** the listens.
- **Album** / **AlbumHasTrack**: an album with 1-based track numbers. **PlayList** / **PlayListHasTrack**: a playlist with 0-based positions and a `Visibility` (Private or Public). Both use `EntityPictures` (resized names stay null until processed).
- **Mix** / **MixItem**: server-generated daily mixes, identified by `MixKind` (the web-player renders the title).
- **UploadIntent**: an upload reservation (key, bucket, purpose, expected size, status, expiration) used for validation and quota.

Every entity that can be listed or removed implements `IHasLifeCycle`; public reads return only `Active` rows, and the ownership checks (`FindOwnedAsync`) treat non-active entities as not found.
