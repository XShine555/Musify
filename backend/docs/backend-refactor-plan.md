# Plan: Backend hardening, cleanup and deduplication

> Work plan for agents/AI with limited context (~128k). Based on a full read of
> **every** non-migration `.cs`, `.csproj` and `appsettings*.json` file under `backend/`
> (2026-09-24, branch `master` at `87c3f97`), plus the web-player call sites of the API.

## How to use this document

1. **Always read section A (Common context).** It's short and every part assumes it.
2. Pick **one part** (P0 through P15). Each part lists: objective, prerequisites, files to
   read, tasks, and closing criteria.
3. One part equals one commit/PR. Don't mix parts. P0 is a pure formatting commit: never
   mix formatting with behaviour changes.
4. If a part needs a shared helper from section B and it **doesn't exist yet**, create it
   yourself exactly as specified in B, so any part can create it identically.
5. When you finish a part, check its box in section D (Log) and fill in the date and notes.
6. Section E maps every audit finding to the part that fixes it. Nothing in E may be left
   without a part; if you discover a new finding, add it to E and to the right part.

Recommended order (arrows are **soft** dependencies, see point 4):

```
P0 (format + guardrails)
 ├→ P1 (security) → P2 (pipeline bugs) → P3 (query/API bugs)
 └→ P4 (dead code)
        ↓
P5 (config) → P6 (domain) → P7 (application helpers) → P8 (pictures feature)
                                                    ↘ P9 (responsibilities)
P10 (infra services) → P11 (MassTransit) → P12 (API layer) → P13 (Gateway/Worker)
                                                          → P14 (tests) → P15 (final sweep)
```

P1 must land before anything else that touches endpoints: it closes real data leaks.

---

## A. Common context (always read)

### A.1 Solution layout

| Project | Path | Role |
|---|---|---|
| Domain | `Core/Musify.Domain` | Entities (EF data-annotated), value objects, enums |
| Application | `Core/Musify.Application` | Mediator commands/queries (`<Feature>/<UseCase>.cs` = record + handler), responses, contracts (`IDatabase`, `IStorageService`, …), configuration POCOs |
| Infrastructure | `Core/Musify.Infrastructure` | EF `Database`, S3 `StorageService`, ffmpeg, ImageSharp, MassTransit (activities, routing slips, sagas, consumers), jobs |
| Api | `Hosts/Musify.Api` | Minimal API endpoints, DTOs, FluentValidation validators, JWT auth |
| StreamingGateway | `Hosts/Musify.StreamingGateway` | YARP proxy to SeaweedFS with signed stream tickets |
| Worker | `Hosts/Musify.Worker` | MassTransit consumers + Hangfire + hosted jobs |
| Tests | `Tests/*` | xUnit v3. Application tests use SQLite in-memory (`TestDatabase`), Infrastructure tests use Testcontainers (need Docker) |

Libraries: Mediator (source generator, scoped), ErrorOr, EF Core 10 + Npgsql, MassTransit 8
(RabbitMQ, EF outbox, courier routing slips, saga state machines), Hangfire, FluentValidation,
X.PagedList (to be removed in P7), YARP.

### A.2 Verifying each part

From `backend/`:

```bash
dotnet build Musify.slnx
```

```bash
dotnet test Musify.slnx
```

- The build must stay at **0 warnings** (after P0, warnings are errors).
- `Infrastructure.Tests` need Docker running. If Docker isn't available, say so in the Log;
  don't skip silently.
- A part that changes an **HTTP contract** (route, request/response shape, status code) must
  also: run the API locally, regenerate the web-player client (`npm run gen:api` in
  `web-player/`, needs the API on `http://localhost:5111`), fix the web-player call sites, and
  run `npm run check` in `web-player/`. List the contract changes in the Log.
- A part that changes the **EF model** must add a migration from `backend/`:

```bash
dotnet ef migrations add <Name> --project Core/Musify.Infrastructure --startup-project Core/Musify.Infrastructure
```

  Also update `Tests/Musify.Application.Tests/TestSupport/TestDatabase.cs`, which mirrors part
  of the model configuration by hand.
- A part that changes a **MassTransit message contract** (anything in `Application/Events` or
  `Infrastructure/MassTransit/Arguments|Logs`) must note in the Log that queues have to be
  drained before deploying (in-flight messages with the old shape will fail).

### A.3 Known gotchas (don't reintroduce them)

1. **Outbox.** The API uses the EF bus outbox (`UseBusOutbox`). `eventBus.PublishAsync` only
   writes to the outbox; the message leaves when `SaveChangesAsync` commits. So "publish, then
   save" is correct and atomic. Don't wrap the publish in its own try/catch.
2. **Worker DI.** `AddMediator()` registers **every** handler, including ones whose dependencies
   the Worker doesn't have. Today that's hidden by `ValidateOnBuild = false`. P13 fixes it; until
   then, don't add handler dependencies that the Worker can't resolve if the Worker sends that
   command.
3. **Private IDs as strings.** `long` user ids are serialized as strings (JS precision). Keep that
   when moving the converter (P9).
4. **Track pictures vs album/playlist pictures.** New tracks point at shared *preset* images until
   processed; `DeleteTrackRoutingSlipBuilder` only deletes resized files when
   `Pictures.IsProcessed`, so presets are never deleted. Keep that guard.
5. **Deploy overrides.** `deploy/compose.yml` overrides config via env vars (`Section__Key`). When
   renaming a config key, grep `deploy/` too.
6. **The web-player is the only client.** Endpoints it doesn't call (see E.2) can change freely;
   the others need the regeneration step in A.2.

### A.4 Coordination with the web-player plan

`web-player/docs/frontend-audit-plan.md` has parts marked 🔗 that need this backend. They are
scheduled here so both plans converge:

| Web-player part | Needs from the backend | Done in |
|---|---|---|
| P3.1 playlist permissions | `ownerUserId` in `PlayListApplicationResponse` | P7.9 |
| P8.1 performance | `trackCount` and `durationSeconds` in `PlayListApplicationResponse` | P7.9 |
| P8.4 add album to playlist | bulk insert: add many tracks to a playlist in one request | P9.4 |
| P7.1 shared `LIMITS` | the real validator limits (playlist description is 256 here, track title 200) | P12.10 |

When one of these lands, tick it in **both** Logs.

### A.5 Measured baseline (2026-09-24)

| Metric | Value | Target |
|---|---|---|
| Non-migration `.cs` lines (excluding tests) | 12,687 | −25–30 % |
| Build warnings | 0 (no analyzers, no style rules) | 0 with analyzers + style enforced |
| Files with block namespace / file-scoped | 249 / 50 | 0 / all |
| Files with UTF-8 BOM | 31 (2 with a BOM mid-file) | 0 |
| Files without final newline | 61 | 0 |
| Stray spaces (`} );`, `nameof(X) )]`, `{Id} )`) | 43 | 0 |
| `ThrowIfNull(x, nameof(x))` | 19 | 0 |
| `new ImageSize(` | 15 | 3 (inside `PictureSizes`) |
| Cover-ids projection copies | 9 | 1 per entity |
| `ListeningHistories.Count(l => l.IsCounted)` copies | 9 | 1 |
| `new StaticPagedList` | 7 | 0 |
| `Error.Unauthorized()` used for "not owner" | 10 | 0 |

Recompute with the script in P15 and record in D.1.

---

## B. Shared specs (single source of truth)

### B.1 Style guardrails

#### B.1.1 `backend/.editorconfig` (create in P0)

```ini
root = true

[*]
charset = utf-8
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

[*.{csproj,props,targets,slnx,xml}]
indent_size = 2

[*.json]
indent_size = 4

[**/Migrations/**.cs]
generated_code = true

[*.cs]
indent_size = 4

csharp_style_namespace_declarations = file_scoped:warning
csharp_using_directive_placement = outside_namespace:warning
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
dotnet_diagnostic.IDE0005.severity = warning

dotnet_style_require_accessibility_modifiers = always:warning
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
csharp_prefer_braces = when_multiline:warning
csharp_style_prefer_primary_constructors = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
dotnet_style_prefer_collection_expression = when_types_loosely_match:suggestion

dotnet_naming_rule.private_fields.symbols = private_fields
dotnet_naming_rule.private_fields.style = camel_case
dotnet_naming_rule.private_fields.severity = warning
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_symbols.private_fields.required_modifiers =
dotnet_naming_style.camel_case.capitalization = camel_case

dotnet_naming_rule.private_constants.symbols = private_constants
dotnet_naming_rule.private_constants.style = pascal_case
dotnet_naming_rule.private_constants.severity = warning
dotnet_naming_symbols.private_constants.applicable_kinds = field
dotnet_naming_symbols.private_constants.required_modifiers = const
dotnet_naming_style.pascal_case.capitalization = pascal_case
```

Private fields are **camelCase without underscore** (matches primary-constructor parameters,
which are the majority of the code). Constants and `static readonly` are PascalCase.

#### B.1.2 `backend/Directory.Build.props` (update in P0)

```xml
<Project>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

</Project>
```

`GenerateDocumentationFile` is only there so IDE0005 (unused usings) is reported on build.
If `latest-recommended` raises rules that would force behaviour changes in P0, set those
specific rules to `suggestion` in `.editorconfig` with a comment naming the part that will fix
them, and remove the override in that part.

### B.2 Coding conventions (apply in every part you touch)

1. **File layout.** `Application/<Feature>/<UseCase>.cs` holds the command/query record and its
   handler. Responses live in `Application/<Feature>/Responses/`. No response records declared
   inside handler files, no mapping methods on commands.
2. **Errors** (via `AppErrors`, B.3.6):
   - missing, or exists but not visible to the caller → `NotFound`
   - exists but caller isn't the owner → `Forbidden` (403). **Never** `Unauthorized` for that;
     `Unauthorized` (401) is only for "not signed in".
   - duplicate/state clash → `Conflict`; bad input → `Validation`.
   - Every error has a code (`"Album.NotFound"`) and a description.
3. **Success value:** `Result.Success`.
4. **No try/catch around `SaveChangesAsync` or `PublishAsync` in handlers.** Unexpected
   exceptions go to the global `UseExceptionHandler`. Catch only what you can turn into a
   domain error (e.g. unique-constraint violation → `Conflict`, see B.3.7).
5. **Logging.** Handlers log state changes at `Information` (created/updated/deleted). Not-found
   and forbidden aren't logged (the HTTP log already shows them). Infrastructure code **either**
   handles an exception **or** rethrows it; never log-and-rethrow.
6. **Queries never write.** If a read needs a side effect, it's a command.
7. **Return types.** A handler that can't fail returns the plain type (`ValueTask<T>`), not
   `ErrorOr<T>`. A handler that can fail returns `ErrorOr<T>`. Never return a lazy `IEnumerable`.
8. **Lists** are paginated with `PageRequest` (B.3.4) and have a deterministic order with `Id` as
   the final tiebreaker. Public reads only return `LifeCycleStatus.Active` rows.
9. **Text input:** stored names/titles are trimmed; normalized columns use
   `TextNormalizer.Normalize` (B.3.5).
10. **Entities:** no `[Required]` on value types, no `[Required]` next to `required` on
    non-nullable strings, no initializers equal to the default (`= 0`, `Id = Guid.NewGuid()` in
    object initializers). Navigations: `public User Owner { get; set; } = null!;` (no
    `#pragma warning disable CS8618`, no `required` on navigations).
11. **Naming:** `PlayList` everywhere (types, variables, route parameters: `{playListId}` →
    see P12 for the route style). `…Configuration` for config POCOs. Injected services are named
    after their type (`storageService`, not `storageHandler`).
12. **`ArgumentNullException.ThrowIfNull(x)`** without `nameof`.
13. User-facing strings (mix titles, error descriptions) are English; the web-player owns
    translation.

### B.3 Shared helpers (exact paths and APIs)

#### B.3.1 Pictures configuration (`Application/Configuration/Pictures.cs`)

Replaces `AlbumRoutes`, `PlayListRoutes`, the picture half of `TrackRoutes`, and the three
`*PicturesSizes` classes.

```csharp
namespace Musify.Application.Configuration;

public enum PictureSize { Small, Medium, Large }

public class PictureRoutes
{
    [Required] public string UploadsFolder { get; set; } = "uploads";
    [Required] public string ParentFolder { get; set; } = string.Empty;
    [Required] public string OriginalPicturesFolder { get; set; } = "OriginalPictures";
    [Required] public string SmallPicturesFolder { get; set; } = "SmallPictures";
    [Required] public string MediumPicturesFolder { get; set; } = "MediumPictures";
    [Required] public string LargePicturesFolder { get; set; } = "LargePictures";

    public string FolderPath(PictureSize size);                       // ParentFolder/<size folder>
    public string BuildPicturePath(PictureSize size, string name);    // FolderPath(size)/name
    public string BuildOriginalPicturePath(long userId, string name); // uploads/{userId}/ParentFolder/OriginalPictures/name
    public string BuildTempPath(string tempRootPrefix, long userId, string objectName);
}

public class PictureSizes
{
    [Range(1, 1024)] public int SmallWidth { get; set; } = 128;
    [Range(1, 1024)] public int SmallHeight { get; set; } = 128;
    [Range(1, 1024)] public int MediumWidth { get; set; } = 256;
    [Range(1, 1024)] public int MediumHeight { get; set; } = 256;
    [Range(1, 1024)] public int LargeWidth { get; set; } = 512;
    [Range(1, 1024)] public int LargeHeight { get; set; } = 512;

    public ImageSizes ToImageSizes(PictureRoutes routes);
}

public interface IPictureOwnerConfiguration
{
    PictureRoutes Routes { get; }
    PictureSizes PicturesSizes { get; }
}
```

- `AlbumConfiguration` and `PlayListConfiguration` become `{ PictureRoutes Routes; PictureSizes PicturesSizes; }`
  implementing `IPictureOwnerConfiguration`.
- `TrackConfiguration.Routes` becomes `TrackRoutes : PictureRoutes` adding only the preset and
  audio members (`PresetSmallPicture…`, `OriginalAudiosFolder`, `ProcessedAudiosFolder`,
  `BuildOriginalAudioPath(userId, name)`, `BuildProcessedAudioPath(folder)`).
  `BuildTempAudioPath` is deleted (it was identical to the picture one): use `BuildTempPath`.
- Config keys change: `ParentFolders` → `ParentFolder` (Album/PlayList), `ProcessedAudioFolder` →
  `ProcessedAudiosFolder`, `SmallPictureWidth` → `SmallWidth` etc. Update every `appsettings*.json`
  and grep `deploy/`.
- The un-prefixed `BuildOriginalPicturePath(string name)` overloads and the `…PicturesPath`
  properties that nobody needs after P8 are removed.

#### B.3.2 `ImageSizes` (`Application/Shared/ImageSize.cs`)

```csharp
public record ImageSize(string SavePath, int Width, int Height);

public record ImageSizes(ImageSize Small, ImageSize Medium, ImageSize Large);
```

Events and routing-slip arguments carry one `ImageSizes Sizes` instead of three `ImageSize`
parameters (message contract change, see A.2).

#### B.3.3 `EntityPictures` (`Domain/ValueObjects/EntityPictures.cs`)

Replaces `AlbumPictures` and `PlayListPictures` (identical today). Same four string properties.
Column names are mapped explicitly in `Database.cs`, so **no migration** is needed; verify with
`dotnet ef migrations add` producing an empty migration, then delete it.

Also add a factory used by create/update handlers:

```csharp
public static EntityPictures Pending(string objectName);
```

See P3 for why "pending" must not point the resized names at the original file.

#### B.3.4 Pagination (`Application/Shared/Pagination.cs`)

Replaces X.PagedList (remove `X.PagedList` and `X.PagedList.EF` packages at the end of P7).

```csharp
public record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
}

public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int PageCount,
    int TotalItemCount,
    bool HasNextPage,
    bool HasPreviousPage);

public static class PaginationExtensions
{
    public static Task<PaginatedResponse<T>> ToPaginatedAsync<T>(
        this IQueryable<T> query, PageRequest page, CancellationToken cancellationToken);
}
```

- `ToPaginatedAsync` clamps `PageNumber >= 1` and `1 <= PageSize <= MaxPageSize`, does
  `CountAsync` + `Skip/Take` on the **same** filtered query.
- The query passed in must already be ordered and projected (B.3.5).
- The JSON shape of `PaginatedResponse` stays exactly as today (same property names and order).
- API side: endpoints bind `[AsParameters] PageRequest page`; `PageRequestValidator` rejects
  `PageNumber < 1` and `PageSize` outside `1..100` with a 400.

#### B.3.5 Projections and text (`Application/<Feature>/Responses/*Projections.cs`, `Application/Shared/TextNormalizer.cs`)

```csharp
public static class TrackProjections
{
    public static IQueryable<Track> Active(this IQueryable<Track> query);
    public static IQueryable<TrackApplicationResponse> SelectResponse(this IQueryable<Track> query);
}

public static class AlbumProjections
{
    public const int CoverTrackCount = 4;
    public static IQueryable<Album> Active(this IQueryable<Album> query);
    public static IQueryable<AlbumApplicationResponse> SelectResponse(this IQueryable<Album> query);
}

public static class PlayListProjections
{
    public const int CoverTrackCount = 4;
    public static IQueryable<PlayList> Active(this IQueryable<PlayList> query);
    public static IQueryable<PlayList> VisibleTo(this IQueryable<PlayList> query, long? viewerId);
    public static IQueryable<PlayListApplicationResponse> SelectResponse(this IQueryable<PlayList> query);
}

public static class TextNormalizer
{
    public static string Normalize(string value); // value.Trim().ToUpperInvariant()
}
```

- `SelectResponse` is a single EF-translatable `Select(...)` building the response directly
  (owner name, tag list, `ListeningHistories.Count(l => l.IsCounted)`, track count, cover ids).
  No `Include`, no anonymous intermediate types, no `FromEntity` in queries.
- Keep `FromEntity` only where a handler already has a tracked entity in memory (create/update).
- `VisibleTo(viewerId)`: `Visibility == Public || OwnerUserId == viewerId`.
- Join-table queries (album tracks, playlist tracks, likes) order the join rows, then
  `Select(x => x.Track)` and call `SelectResponse()`.

#### B.3.6 Errors and ownership (`Application/Shared/AppErrors.cs`, `Application/Shared/OwnershipExtensions.cs`)

```csharp
public static class AppErrors
{
    public static Error NotFound(string entity, object id);   // code "<Entity>.NotFound"
    public static Error Forbidden(string entity, object id);  // code "<Entity>.Forbidden"
    public static Error Conflict(string code, string description);
}

public interface IOwnedEntity
{
    Guid Id { get; }
    long OwnerUserId { get; }
}

public static class OwnershipExtensions
{
    public static Task<ErrorOr<T>> FindOwnedAsync<T>(
        this DbSet<T> set, Guid id, long userId, CancellationToken cancellationToken)
        where T : class, IOwnedEntity;
}
```

`Track`, `Album` and `PlayList` implement `IOwnedEntity`. `PlayList.UserId` is renamed to
`OwnerUserId` with `[Column("UserId")]` (no migration).

#### B.3.7 Unique-violation mapping (`Application/Contracts/IDatabase.cs`)

```csharp
Task<ErrorOr<Success>> TrySaveChangesAsync(Error onUniqueViolation, CancellationToken cancellationToken);
```

Implemented in `Database` by catching `DbUpdateException` whose inner exception is
`PostgresException { SqlState: "23505" }` (and the SQLite equivalent in `TestDatabase`). Used by
like, follow, add-to-playlist/album.

#### B.3.8 Lifecycle (`Domain/Abstractions/IHasLifeCycle.cs`)

```csharp
public interface IHasLifeCycle
{
    Guid Id { get; }
    LifeCycleStatus LifeCycleStatus { get; set; }
}
```

Implemented by `Track`, `Album`, `PlayList`. Used by the generic MassTransit activities (P11).

#### B.3.9 Routing slips (`Infrastructure/MassTransit/RoutingSlip/RoutingSlips.cs`)

Replaces the preamble copied in 10 builders/consumers and `Consumers/EndpointHelper.cs`.

```csharp
internal static class RoutingSlips
{
    public static RoutingSlipBuilder Create(Guid? correlationId);               // new id + CorrelationId variable + clean-up subscription
    public static RoutingSlipBuilder AddStep(this RoutingSlipBuilder builder, string name, string executeEndpointName, object arguments);
    public static RoutingSlipBuilder TrackFaults(this RoutingSlipBuilder builder, Guid subjectId, string processKind); // SubjectId + ProcessKind + fault subscription
}
```

`RoutingSlipVariableNames` moves to namespace `Musify.Infrastructure.MassTransit.RoutingSlip`
(it lives in that folder already).

---

## C. Parts

### P0: Formatting and guardrails (pure formatting commit)

**Objective:** make style mechanical so later parts don't argue about it.

**Tasks**
1. Create `backend/.editorconfig` (B.1.1) and update `Directory.Build.props` (B.1.2).
2. Remove BOMs from all `.cs` files (including the two mid-file ones in
   `Application/Events/UpdatePlayListPictureEvent.cs` and `UpdateTrackPictureEvent.cs`).
3. From `backend/`:

```bash
dotnet format Musify.slnx
```

   then fix by hand whatever `dotnet format` can't: the 43 stray spaces (`} );`, `} ));`,
   `nameof(X) )]`, `(PID: {ProcessId} )`, `prefix: {Prefix} )`, `if (a == null|| …)`), blank
   lines right after `{` of a namespace/class, and missing `private` modifiers.
4. Build with warnings as errors. For analyzer rules that need behaviour changes, downgrade them
   in `.editorconfig` with a `# fixed in Pn` comment (see B.1.2).

**Don't:** rename anything, change any logic, touch migrations.

**Closing criteria:** `git diff --stat` shows only whitespace/namespace/using changes (review
with `git diff -w`); 0 warnings; tests green; metrics in A.5 for namespaces, BOM, final newline,
stray spaces are at target.

---

### P1: Security and privacy fixes

**Objective:** close data leaks and abuse paths. Small, surgical, test each one.

**Files to read:** `Hosts/Musify.Api/Endpoints/UserEndpoints.cs`, `PlayListEndpoints.cs`,
`Application/PlayLists/*.cs`, `Application/Services/UploadIntentValidator.cs`,
`Application/Tracks/CreateTrack.cs`, `Application/Albums/CreateAlbum.cs`,
`Application/Albums/UpdateAlbum.cs`, `Application/PlayLists/CreatePlayList.cs`,
`Application/PlayLists/UpdatePlayList.cs`, the upload validators in `Hosts/Musify.Api/Validators`,
`Hosts/Musify.StreamingGateway/Middleware/TicketValidationMiddleware.cs`.

**Tasks**
1. **Delete `POST /users`** (anonymous, lets anyone create a user with any id). Remove
   `CreateUserCommand`, `CreateUserRequest`, `CreateUserRequestValidator` and their tests. Users
   are provisioned only by `SyncUserCommand` on token validation. The web-player doesn't call it.
2. **Private playlists:**
   - `GET /playlists` (all users' playlists, not used by the web-player): delete it and
     `GetPlayListsQuery`.
   - `GET /playlists/users/{userId}`: remove the `onlyPublic` parameter; the handler gets
     `ViewerId` and applies `VisibleTo(viewerId)` (owner sees everything, others only public).
     Contract change: web-player `user/[id]/+page.server.ts` stops sending `onlyPublic`.
   - `GET /playlists/{id}/tracks`: take `CurrentUser`, return `NotFound` when the playlist isn't
     visible to the viewer (same rule as `GetPlayListById`).
   - `GET /playlists/{id}/cover`: **decision (recommended default): leave public.** The web-player
     proxies covers without the user's token (`web-player/src/lib/server/image.ts`) and ids are
     unguessable GUIDs. Write that decision in the Log. If you choose to protect it, the proxy
     must forward the session token and the cache header must become `private`.
3. **Upload intents:** `ValidateAndLoadAsync` takes the expected `UploadIntentPurpose` and returns
   `Validation` if it doesn't match. `CreateTrack` rejects `PictureIntentId == AudioIntentId`.
4. **Upload request validation** (both picture validators and the track one):
   - `FileType`: letters/digits only, 1–10 characters, from an allow-list (`jpg, jpeg, png, webp` for
     pictures; `mp3, wav, flac, ogg, m4a, aac` for audio). Put the lists in one static class in
     `Hosts/Musify.Api/Validators/Uploads.cs` so the three validators share them.
   - `ContentType`: must match the allow-list (`image/*` subset, `audio/*` subset).
   - `ExpectedSizeBytes`: `> 0` and `<= UploadIntentConfiguration.MaxUploadBytes`.
5. **Gateway range clamp:** when a ticket has `maxBytes`, an unparsable `Range` header or a
   multi-range request must be **replaced** by `bytes=0-{maxBytes-1}` (not passed through).
   Handle suffix ranges (`bytes=-N`) by rejecting them with 416 when `maxBytes` is set.
6. **Listening data privacy — decision (recommended default): require auth and self only** for
   `GET /users/{id}/listening-stats` (the web-player only reads it for the current user; check
   before changing). Leave `listening-history` and `last-listened-track` public (the profile page
   shows them) and write that in the Log.

**Tests to add:** anonymous `POST /users` → 404/405; other user's private playlist via
`/playlists/users/{id}` and `/playlists/{id}/tracks` → hidden; intent purpose mismatch;
same intent twice in `CreateTrack`; gateway middleware with multi-range and invalid range when
`maxBytes` is set.

**Closing criteria:** tests above green; web-player regenerated and `npm run check` clean.

---

### P2: Processing pipeline bugs (Worker side)

**Files to read:** `Infrastructure/MassTransit/Consumers/ProcessingSlipFaultConsumer.cs`,
`RoutingSlip/Builders/*.cs`, `Sagas/*.cs`, `Activities/Audio/*.cs`, both `appsettings.json`
(Api, Worker), `Application/Configuration/UploadIntentConfiguration.cs`.

**Tasks**
1. `ProcessingSlipFaultConsumer`: add the `AlbumPicture` case publishing
   `AlbumPictureProcessingFailed`. Today album sagas stay in `Processing` forever on failure.
2. Config keys: the appsettings use `TempRootPrefix`, `TempUploadsRetentionDays`,
   `TempCleanupJobIntervalSeconds`; the class expects `TemporalRootPrefix`,
   `TemporalUploadsRetentionDays`, `TemporalCleanUpJobIntervalSeconds`, so they're silently
   ignored. Rename the **class** properties to `TempRootPrefix`, `TempUploadsRetentionDays`,
   `TempCleanupJobIntervalSeconds` (shorter, and what ops already configured), keep the value
   `"temp"`. Check that no object with the `temporal/` prefix is left in the bucket (the clean-up
   job only lists the configured prefix); if there are, note it in the Log for a one-off clean-up.
3. The create-track, create-album and create-playlist slips (copy to final + consume intents +
   publish processing event) have no fault subscription: if they fault the entity stays
   `Pending` forever. Add `TrackFaults(...)` with new process kinds `TrackCreation`,
   `AlbumCreation`, `PlayListCreation` that publish the entity's `*ProcessingFailed` event
   directly (so the existing failed-consumers mark it as failed and remove files).
4. Duration precision: `TranscodeAudioActivity` stores `(int)Math.Round(...)`; store the `double`
   `TotalSeconds`.
5. `UpdateTrackAudioActivity`: when the folder key is empty it publishes `TrackAudioProcessed`
   without marking the audio as completed. Throw instead (it's a programming error) so the slip
   faults and the saga marks the track failed.
6. Sagas and picture **updates**: `*PictureProcessed` / `*PictureProcessingFailed` for an update
   arrive at a saga that was finalized after creation. Configure
   `OnMissingInstance(m => m.Discard())` on those events in the three state machines, and verify
   against a local RabbitMQ that updating a picture no longer puts messages in `*_error` queues.
   Record the before/after in the Log (this was "to verify" in the audit).

**Closing criteria:** manual run of the stack (`deploy/compose.dev.yml`): create album with a
corrupt image → album ends `Failed`; update a playlist picture → no `_error` messages.

---

### P3: Query and API bugs

**Files to read:** `Hosts/Musify.Api/Extensions/ErrorOrHttpExtensions.cs`, all
`Application/**/Get*.cs`, `Application/PlayLists/UpdatePlayList.cs`, `CreatePlayList.cs`,
`Application/Albums/CreateAlbum.cs`, `UpdateAlbum.cs`, `DeleteAlbum.cs`, `Application/Likes/*.cs`,
`Application/Users/FollowUser.cs`, `Application/*/AddTrackTo*.cs`,
`Infrastructure/Persistence/Database.cs`.

**Tasks**
1. **403 for "not owner":** replace the 10 `Error.Unauthorized()` with `AppErrors.Forbidden`
   (B.3.6). In `ErrorOrHttpExtensions`, map `Forbidden` to `Results.Problem(statusCode: 403)`,
   and make `NotFound`/`Conflict` return ProblemDetails too (today they return a bare string).
   Update `.Produces(401)` to `.Produces(403)` on owner-only endpoints. Contract change.
2. **Pagination bounds:** introduce `PageRequest` + validator (B.3.4) on every paginated endpoint
   (today `pageSize=0` → 500, `pageSize=1000000` → unbounded). Bound `limit` on
   `/albums/recent` to `1..50`. Remove the ad-hoc clamp in `GetTracksByUserId`.
3. `GetPlayListsByUserId`: `totalCount` is computed before the `Active` filter; move the filter
   before counting (P7 will replace this with `ToPaginatedAsync` anyway).
4. **Lifecycle filter:** public reads (`GetTracks`, `GetTracksByUserId`, `GetTrackById`,
   `GetAlbums*`, `GetAlbumById`, `GetAlbumTracks`, `GetPlayListTracks`, `GetLikedTracks`,
   `GetGenres`, `MixItemMapper`, `GetListeningHistory`, `GetRecentlyListenedAlbums`) only return
   `Active` rows. Stream only `Active` tracks.
5. **Covers before processing:** `CreateAlbum`/`UpdateAlbum`/`CreatePlayList`/`UpdatePlayList`
   set `Small/Medium/LargeName = objectName`, so `/cover?size=small` looks for
   `…/SmallPictures/{objectName}`, which doesn't exist until processing ends → S3 throws → 500.
   Fix: resized names stay `null` until the worker sets them (`EntityPictures.Pending` in B.3.3
   sets only `OriginalName`), and the cover query falls back to the **original** key while the
   resized one is missing. Also make the cover endpoints return 404 (not 500) when the object
   doesn't exist (`HeadObjectAsync` or catch `NoSuchKey` in the storage service → `null`).
6. **Races → 500:** use `TrySaveChangesAsync` (B.3.7) in `ToggleTrackLike`, `FollowUser`,
   `AddTrackToPlayList`, `AddTrackToAlbum`, mapping the unique violation to a no-op success
   (like/follow) or `Conflict` (add track). Add a unique index on
   `PlayListHasTrack (PlayListId, TrackId)` (migration; dedupe existing rows in the migration
   with `DELETE … USING` keeping the lowest position).
7. **Track/position numbering:** compute next position with
   `MaxAsync(x => (int?)x.Position) ?? -1` in one query. Decide one base for both playlist
   positions and album track numbers (recommended: playlists 0-based internal position, album
   track numbers 1-based because they're user-visible; document it in the Log) and renumber on
   removal in **both** (playlists currently leave gaps).
8. `UpdatePlayList`: trim the name; allow clearing the description (`""` → `null`; `null` means
   "unchanged"); validate `NewVisibility` with `IsInEnum()`. `CreatePlayList`: trim the name.
   Same `IsInEnum()` on `CreatePlayListRequest.Visibility`.
9. `GetMixesByUserId` N+1: one query for all cover items of all mixes.
10. `GetLastTrackListenedByUserId` counts uncounted listens while the history only shows counted
    ones: filter `IsCounted` for consistency, and return 204 (not 404) when there's none (check
    the web-player call site in `(app)/+page.server.ts` or wherever it's read).
11. `GetListeningStats` streak: count the streak ending today **or yesterday** (a streak isn't
    broken until the day is over).
12. `DeleteAlbum` removes the row directly and leaves its pictures in storage. Give it the same
    shape as playlists: mark `Removing`, publish `DeleteAlbumEvent`, and a
    `DeleteAlbumRoutingSlipBuilder` that removes the four pictures and the row. (If P11 already
    landed, build it with the generic activities.)

**Closing criteria:** each task has a test (Application tests for handlers, Api tests for status
codes); web-player regenerated; `npm run check` clean.

---

### P4: Dead code removal

**Objective:** delete what nothing uses, before refactoring it.

**Tasks**
1. `Infrastructure/Services/SingleFlightCache.cs` + its test + `Microsoft.Extensions.Caching.Memory`
   from `Infrastructure.csproj` (the API gets `IMemoryCache` from `AddMemoryCache`).
2. `IStorageService.GetUrlAsync` and `RemoveFolderAsync` (only tests use the latter) + tests.
3. `IAudioTranscoderService.IsValidAudioFileAsync` + implementation + `BuildValidateAudioArguments`.
4. `DownloadFileFromUrlActivity`, `DownloadFileFromUrlArguments`, `DownloadFileFromUrlLog`, its
   registration, `ActivityNames.DownloadThumbnail`, and `services.AddHttpClient()` in the Worker.
5. `TrackAudio.DownloadRequested`, `RetryCount`, `LastRetryAt` and their mapping (migration
   `DropTrackAudioRetryColumns`).
6. `Upload` entity, `UploadState` enum, `IDatabase.Uploads`, the `Upload` writes in
   `StorageService` (migration `DropUploadTable`). Update `docs/architecture.md` (mentions
   `Upload`).
7. `DatabaseDesignTimeFactory`: remove the unused `optionsBuilder`.
8. `Database.cs`: remove the `AppIDatabase` alias.
9. Unused locals (`var response =` in `StorageService`), `.AsNoTracking().AsQueryable()`,
   redundant `database.X.Update(entity)` on tracked entities, the always-true
   `ExpiredIntentsRetentionDays >= 0`, the duplicated `if (pictureIntent != null && …)` in
   `UpdateAlbum`/`UpdatePlayList`, `AddPictureService`'s unused `configuration` parameter,
   `AddUploadIntentConfiguration` (already done by `AddApplication`; see P13 for the Worker).
10. `Newtonsoft.Json` from `Api.csproj` and `Worker.csproj`; package references in `Api.csproj`
    and `Worker.csproj` that come transitively from Infrastructure (AWSSDK.S3, MassTransit.*,
    Npgsql, EF, OpenTelemetry.*, MimeMapping, ImageSharp, X.PagedList.EF). Keep only what the host
    uses directly. Rename the Worker `UserSecretsId` prefix (`dotnet-PictureWorker-…`) only if
    nobody relies on it locally (ask; otherwise leave and note).
11. `appsettings.json` of the API: remove `AudioTranscoder` and `Workers` (Worker-only).
    Worker: remove `AudioTranscoder.Routes.WorkingDirectory` (doesn't exist).
12. `CurrentUser.HasClaim` / `FindClaim` / `Principal` if unused after grep.

**Closing criteria:** grep proves each symbol is gone; migrations added; tests green.

---

### P5: Configuration unification

**Prerequisites:** P2 (config key rename there).

**Tasks**
1. Create `Pictures.cs` (B.3.1) and `ImageSizes` (B.3.2); rewrite `AlbumConfiguration`,
   `PlayListConfiguration`, `TrackConfiguration` on top of them. Update every call site
   (`BuildSmallPicturePath(x)` → `BuildPicturePath(PictureSize.Small, x)`).
2. Update all `appsettings*.json` with the renamed keys and grep `deploy/`.
3. **One way to register options:** `AddValidatedOptions<T>` (move it to
   `Application/Configuration/ConfigurationDependencyInjection.cs` if not there, public). Replace
   `RegisterValidatedOptions` in MassTransit DI and the inline copies in `AddDatabase`,
   `AddStorageService`, `AddObservability`, `AddAudioTranscoder`, `AddStreamTicketService`,
   `AddAuthenticationConfiguration`. Add `public interface IConfigurationOptions { static abstract string SectionName { get; } }`,
   implement it on every config class, and add the overload
   `AddValidatedOptions<T>(this IServiceCollection services, IConfiguration configuration) where T : class, IConfigurationOptions`
   so call sites stop repeating the section name.
4. Parameter name `services` everywhere (not `serviceDescriptors`).
5. Gateway: rename `StreamTicketValidationOptions` → `StreamTicketValidationConfiguration` and
   inject the POCO like the rest (no `IOptions<T>` in constructors).

**Closing criteria:** `grep -r "AddOptionsWithValidateOnStart" backend` finds only
`AddValidatedOptions`; `new ImageSize(` appears only in `PictureSizes.ToImageSizes`.

---

### P6: Domain cleanup

**Tasks**
1. `EntityPictures` (B.3.3) replaces `AlbumPictures` and `PlayListPictures`.
2. `IHasLifeCycle` (B.3.8) and `IOwnedEntity` (B.3.6); rename `PlayList.UserId` →
   `OwnerUserId` with `[Column("UserId")]`.
3. Apply B.2 rule 10 to every entity: drop redundant `[Required]`, redundant initializers,
   `#pragma warning disable CS8618` (use `= null!` on navigations), `required` on `Track.Owner`
   (then `CreateTrack` no longer needs to load the `User` tracked; it sets `OwnerUserId` only).
4. Rename `PlaylistVisibility` → `PlayListVisibility` (enum type name only; the DB stores ints,
   JSON stores names, so no migration and no contract change — verify the OpenAPI schema name
   change doesn't break web-player types; regenerate).
5. Table names: **decision (recommended default): don't rename tables** (pure churn + risky
   migration). Instead make every entity declare `[Table("…")]` explicitly with its current name
   so the inconsistency is visible and frozen. Note in the Log.
6. `UploadIntent`: move `IsExpired`'s clock read out of the entity (`IsExpiredAt(DateTime now)`)
   so it's testable.

**Closing criteria:** `dotnet ef migrations add Check` produces an empty migration (delete it);
tests green.

---

### P7: Application shared helpers

**Prerequisites:** P6.

**Tasks**
1. Create `Pagination.cs` (B.3.4), `*Projections.cs` + `TextNormalizer` (B.3.5), `AppErrors` +
   `OwnershipExtensions` (B.3.6), `TrySaveChangesAsync` (B.3.7) if P3 didn't.
2. Rewrite every list query with `…Active().VisibleTo(…).OrderBy(…).SelectResponse().ToPaginatedAsync(page, ct)`.
   Delete every `StaticPagedList`, `ToPagedListAsync`, `Include` used only for mapping, and the
   `X.PagedList*` package references.
3. Delete `TracksSearchResponse`, `TrackSearchItemResponse`, `AlbumsSearchResponse`,
   `AlbumSearchItemResponse`; `GetTracks` and `GetAlbums` return `PaginatedResponse<…>`.
   **Contract change:** web-player `explore/+page.svelte` uses `items.map((item) => item.track)`
   and the album equivalent; update to use items directly.
4. Replace the load/exists/owner/log boilerplate in every command with `FindOwnedAsync`.
5. Apply B.2 rules 2–5 and 7 in every handler: `AppErrors`, `Result.Success`, no try/catch around
   save/publish, logging policy, plain return types for handlers that can't fail (`GetUsers`,
   `GetLikedTracks`, `GetRecentlyListenedAlbums`, `GetMixesByUserId`…), `GetListeningHistory`
   returns `IReadOnlyList`.
6. Move inline responses to `Responses/`: `UserProfileResponse`, `ListeningStatsResponse`,
   `GenreResponse`, `AvailableGenreResponse`, the three `*CoverLocation` (which become one
   `CoverLocation` in P8).
7. Use `TextNormalizer.Normalize` for every normalized column and search term.
8. Deterministic ordering with `Id` tiebreaker everywhere; `GetTracksByUserId` orders by
   `CreatedAt` descending like the rest.
9. `PlayListProjections.SelectResponse` adds `OwnerUserId` (string-serialized, A.3.3),
   `TrackCount` and `DurationSeconds` (sum of active tracks) to `PlayListApplicationResponse`
   (additive contract change, needed by web-player P3.1 and P8.1; see A.4).

**Closing criteria:** A.5 metrics for cover-ids projection, listens-count projection and
`StaticPagedList` at target; line count of `Application` down ≥ 20 %.

---

### P8: Pictures feature deduplication

**Prerequisites:** P5, P6, P7.

**Objective:** Album and PlayList picture flows are copies of each other (and of the picture half
of Track). Make them one implementation.

**Tasks**
1. `Application/Pictures/RequestPictureUpload.cs`: one internal service
   `PictureUploadIssuer.IssueAsync(userId, purpose, IPictureOwnerConfiguration, fileType, contentType, expectedSize, ct)`
   returning `ErrorOr<PictureUploadResponse>`; `RequestAlbumPictureUpload` and
   `RequestPlayListPictureUpload` become thin commands calling it. `AlbumPictureUploadResponse`
   and `PlayListPictureUploadResponse` merge into `PictureUploadResponse` (same shape; OpenAPI name
   changes → regenerate web-player types). `RequestTrackUploadUrls` reuses the intent-creation
   part for both of its intents.
   Also: issue the pre-signed URL **after** the quota check (today it's issued first and wasted
   on rejection).
2. `Application/Pictures/GetCover.cs`: one `GetCoverQuery(CoverOwner Owner, Guid Id, PictureSize Size)`
   (`enum CoverOwner { Track, Album, PlayList }`), one `CoverLocation`, content type from
   `MimeUtility` for all three (Track currently hand-rolls it). Size is parsed in the API with
   `Enum.TryParse` (default `Medium`).
3. Create/Update album and playlist: extract `PictureSourceChange.PrepareAsync(intentId, userId, IPictureOwnerConfiguration, purpose)`
   returning the `EntityPictures` and the event payload; handlers shrink to entity changes +
   publish.
4. Events: `CreateAlbumResourcesEvent`/`CreatePlayListResourcesEvent` and
   `UpdateAlbumPictureSourceEvent`/`UpdatePlayListPictureSourceEvent` keep separate types (the
   sagas correlate on them) but carry `ImageSizes Sizes` (B.3.2). Message contract change (A.2).
5. `UploadIntentValidator`: stop receiving `UploadIntentConfiguration` as a method parameter;
   inject it. Quota: compute count and bytes in one aggregate query instead of loading rows, use
   the audio default for audio intents, and ignore intents whose `ExpiresAt` already passed.

**Closing criteria:** `diff` between album and playlist handlers is only names; the picture
handlers in `Albums/` and `PlayLists/` are each ≤ 40 lines.

---

### P9: Responsibilities

**Tasks**
1. **Queries don't write:** split `GetTrackStreamQuery` into `StartListeningCommand` (creates the
   `ListeningHistory`, returns stream ticket + `listenId`). `TrackStreamIssuer` only issues the
   ticket and builds the URL. Same HTTP route and response (no contract change).
2. **Serialization belongs to the API:** move `LongAsStringConverter`/`NullableLongAsStringConverter`
   to `Hosts/Musify.Api/Serialization/`, remove `[JsonConverter]` attributes from Application
   responses, and register both converters globally in `ConfigureHttpJsonOptions`. That is safe
   because the only `long` values the API returns are user ids (`ExpectedSizeBytes` is
   request-only and the converter also reads numbers; `TotalItemCount` is `int`); re-check with
   a grep before merging. Update the OpenAPI schema transformer to mark every `long` as string,
   and check that the regenerated web-player types don't change (they already see strings).
3. **One validation place:** tag rules only in `CreateTrackRequestValidator` (remove from the
   handler, keep a domain guard `GenreCompatibility.FindConflicts` call in the handler only if
   commands can be sent from somewhere other than the API — they can't today, so remove);
   `RecordListeningProgress` gets a FluentValidation validator instead of handler checks; genre
   parsing in `GetTracks` moves to binding (`Genre? genre` with a validator).
4. **Endpoint orchestration + bulk insert:** the `AddPlayListTrack` endpoint calls one command
   and stops re-querying the track. Replace it with `POST /playlists/{id}/tracks` taking
   `{ "trackIds": [...] }` (1–500 ids) → 204, backed by `AddTracksToPlayListCommand` that appends
   the tracks in order in one transaction, skipping those already present. This is the bulk
   endpoint web-player P8.4 asks for (A.4) and removes the per-track POST loop in
   `web-player/src/lib/server/playlistActions.ts`. For consistency, albums get the same shape
   (`POST /albums/{id}/tracks` with `trackIds`), replacing `POST /albums/{albumId}/tracks/{trackId}`.
   Contract change on both; update the two web-player call sites.
5. **`UserHasTrack`:** it duplicates `Track.OwnerUserId` (written only on create). Replace its reads
   (`GetTracks`, `GetTracksByUserId`, `GetGenres`, `GenerateMixesForUser`) with `Tracks` filtered
   by owner/active, then drop the table (migration). If the intent was a future "library" (saved
   tracks), write that in the Log and keep it, but then it must not be written on create.
6. **Mix texts:** move the Spanish titles out of `GenerateMixesForUser` (B.2 rule 13): store a
   `MixKind` enum (`Discovery`, `Daily`) instead of title/subtitle strings and let the web-player
   render the text. Migration + contract change. **Decision (recommended default): do it**; if
   not, at least move the strings to a single constants class.
7. `GenerateMixesForUser`: rely on cascade delete for `MixItems` (remove the manual item query).

---

### P10: Infrastructure services

**Tasks**
1. `StorageService`: after P4 it no longer depends on `IDatabase`; register it as singleton.
   Use `StorageKey.Combine` for keys; fix the `trasnsferUtility` typo and dispose it; fix
   `Path.Combine(sourceDirectory, file)` (file is already a full path); prefix normalisation and
   pagination helpers shared by `ListObjectsAsync`; no log-and-rethrow (B.2 rule 5); consistent
   log levels (`Debug` for per-object operations). `GetFileAsync` returns `null` on `NoSuchKey`
   (used by the cover endpoints, P3.5). `IStorageService`: `CancellationToken` last
   (`preventOverwrite` before it), parameter names camelCase.
2. `AudioTranscoderService.cs`: rename the class to `AudioTranscoderService` (file already has
   that name). Access modifiers, no log-and-rethrow, one `ExecuteFfmpegAsync(arguments, workingDirectory, Stream? input, …)`
   instead of two near-identical methods.
3. `PictureService`: no log-and-rethrow.
4. `MassTransitEventBus`: expression-bodied, no `nameof`.
5. **Jobs:** one mechanism. Recommended: **Hangfire for all four** (it already runs in the Worker,
   gives retries and a dashboard). Convert `UploadIntentExpirationJob` and
   `TemporalUploadsCleanUpJob` (rename file/class to `TempUploadsCleanupJob`) to Hangfire recurring
   jobs with the configured intervals, and move their logic behind commands in Application
   (`ExpireUploadIntentsCommand`, `DeleteStaleTempUploadsCommand`) like
   `DeleteStaleUncountedListensCommand`. Rename `AddDailyMixGenerationJob` to `AddJobs` and
   register all jobs there. Job recurring registration moves out of `Worker/Program.cs` into an
   extension in Infrastructure.
6. Observability: `AddObservability` also used by the Worker (P13).
7. `JwtBearerEventsHandler` (Api): dispose `HttpRequestMessage`/response; read the userinfo
   endpoint from the OIDC discovery document instead of hardcoding `/oidc/v1/userinfo`.

---

### P11: MassTransit deduplication

**Prerequisites:** P5 (`ImageSizes`), P6 (`IHasLifeCycle`).

**Tasks**
1. `RoutingSlips` helper (B.3.9); rewrite every builder and failed-consumer with it. Delete
   `EndpointHelper`.
2. Merge `AlbumPictureSourceRoutingSlipBuilder` and `PlayListPictureSourceRoutingSlipBuilder`
   into `PictureSourceRoutingSlipBuilder` (the only difference is the publish-activity
   arguments; pass a factory or a `CoverOwner`).
3. Generic lifecycle activities: `MarkLifeCycleActivity<TEntity>` (arguments: `Guid Id`,
   `LifeCycleStatus Status`) and `DeleteEntityActivity<TEntity>` for `Track`, `PlayList`, `Album`.
   Replaces `Mark{Track,PlayList,Album}As{Failed,Removing}Activity` and
   `Delete{Track,PlayList}FromDbActivity` (+ their arguments). Keep distinct endpoint names per
   entity (`mark-track-lifecycle`, …) so queues stay separate. Message contract change (A.2).
4. `Update{Track,PlayList,Album}PictureActivity`: one generic base
   `UpdatePicturesActivity<TEntity, TLog>` with an abstract "apply/restore pictures" hook; Track
   additionally sets `ProcessingStatus`. Compensation for Album/PlayList should also leave a
   consistent state (today only Track marks failed).
5. `*ProcessingFailedConsumer`: one shared method building the clean-up slip from
   `(entity, bucket, keys[])`.
6. The 9 one-line consumers (`Create*Consumer`, `Update*Consumer`, `Delete*Consumer`) stay one
   class per message (MassTransit needs it) but become expression-bodied with no blank lines.
7. The three Audio activities repeat "load track or throw, set status failed on error": extract
   `TrackAudioStatus.MarkFailedAsync(database, trackId, ct)`.
8. `GeneratePictureWorkflowPathsActivity` isn't async: return `Task.FromResult`.
9. DI: outbox configuration in one local function (used by client and consumers); saga repository
   configuration in one local function (used 3×); keep registration lists alphabetised.
10. `ThrowIfNull` without `nameof`; injected services named after their type (`storageService`,
    `pictureService`).

**Closing criteria:** `Infrastructure/MassTransit` line count down ≥ 30 %; manual end-to-end run:
create/update/delete track, album and playlist, plus a forced failure for each.

---

### P12: API layer

**Tasks**
1. `Program.cs`: `WebApplication.CreateBuilder(args)`; sorted usings; CORS constant at top.
2. Cover endpoints: one `CoverEndpoint.Stream(...)` helper used by the three `/cover` routes.
3. Routes: parameters named `{id}` for the resource itself and `{trackId}` for sub-resources;
   Guid routes use `:guid`, user ids `:long`. `PlayList` routes keep the `/playlists` path.
4. Every endpoint returns `IResult` through `ToHttpResult` (no raw return types), including the
   ones whose handlers can't fail (use `Results.Ok`).
5. Update DTOs: drop the `New…` prefix (`UpdateAlbumRequest(Title, Description, ReleaseYear, PictureIntentId)`)
   — contract change, regenerate web-player; or keep and write the reason in the Log.
   **Decision (recommended default): rename**, it's the only place with that prefix.
6. Validators: one shared upload validator (P1.4 created the allow-lists); album and playlist
   picture validators become one generic `PictureUploadRequestValidator<T>`, or merge the DTOs
   into `PictureUploadRequest` (preferred, since the responses merged in P8).
7. `OpenApiOptionsSetup` and `ScalarOptionsSetup`: share the `SecuritySchemeId` constant.
8. `.WithSummary(...)` strings: sentence case, no trailing period, consistently.
9. `JwtBearerOptionsSetup`: single-line guard without braces (B.1.1 `when_multiline`).
10. Move the validation limits into one `Hosts/Musify.Api/Validators/Limits.cs` (playlist name
    50, descriptions 256, titles 200, user name 48, release year range) used by every validator,
    and list them in the Log so web-player P7.1 copies the same values (A.4). If the two plans
    disagree on a limit (e.g. the web-player draft has playlist description 300 and track title
    100), the database column length wins.

---

### P13: Worker and Gateway hosts

**Tasks**
1. Worker `Program.cs`: call `AddApplication(configuration)` instead of registering Application
   options by hand (it currently forgets `AlbumConfiguration`); add `AddObservability`; remove
   `ValidateOnBuild = false` and make the container validate. If a handler has dependencies the
   Worker can't provide (stream tickets, gateway config), register those configs too — they're
   just POCOs — rather than disabling validation.
2. Worker `appsettings.json`: complete and consistent with the API for shared sections (after P5
   the keys changed), remove stale ones.
3. Gateway: `Program.cs` uses `AddValidatedOptions`-style registration (it doesn't reference
   Application; copy the 6-line helper locally, or reference nothing and keep
   `AddOptions…ValidateOnStart` — **decision (recommended default): keep it local and
   self-contained**, the gateway must stay dependency-free); field naming per B.1.1 (no
   underscores; rename the middleware field that shadows the `options` constructor parameter);
   `appsettings.json` default bucket in the YARP transform is overridden by compose — replace it
   with an obviously-placeholder value (`"/buckets/CHANGE_ME/{**rest}"`) so the override is
   explicit, or document it in `docs/streaming.md`.

---

### P14: Tests

**Tasks**
1. Add tests for what has none today: MassTransit activities (at least the generic lifecycle and
   picture-update activities with the SQLite `TestDatabase`), `ProcessingSlipFaultConsumer`
   (every process kind), routing slip builders (activity order and arguments), saga state
   machines with MassTransit's test harness (happy path, failure, missing instance on update).
2. Endpoint tests for visibility/ownership (P1, P3) if not already added there.
3. Keep `TestDatabase` in sync with `Database.OnModelCreating`: extract the owned-type and index
   configuration of `Database` into `ModelConfiguration.Apply(ModelBuilder)` in Infrastructure
   and call it from both, so the test model can't drift. (Requires `Application.Tests` to
   reference Infrastructure, or move the test DB to `Infrastructure.Tests`; pick the former and
   note it.)

---

### P15: Final sweep and documentation

**Tasks**
1. Recompute the A.5 metrics (script below) and fill D.1.
2. Grep for leftovers: `Error.Unauthorized(`, `StaticPagedList`, `new ImageSize(`,
   `ThrowIfNull(.*nameof`, `#pragma warning disable CS8618`, `try` blocks around
   `SaveChangesAsync` in `Application/`, `serviceDescriptors`, `storageHandler`, `PlaylistVisibility`.
3. Update `docs/architecture.md`, `docs/media-processing.md`, `docs/projects.md`,
   `docs/storage.md`, `docs/streaming.md` with the new names (jobs, entities, routes).
4. Add a short `backend/CLAUDE.md` with section B.2 (conventions) so future work follows them.
5. Keep this document as the record: don't delete section E, just make sure every row points to
   a checked part in D.

Metrics script (from `backend/`, Git Bash):

```bash
F=$(git ls-files | grep "\.cs$" | grep -v Migrations | grep -v Tests); cat $F | wc -l; grep -c "new StaticPagedList" $F | awk -F: '{s+=$2} END {print s}'; grep -c "new ImageSize(" $F | awk -F: '{s+=$2} END {print s}'
```

---

## D. Log

| Part | Done | Date | Notes (decisions, contract changes, migrations, queue drains) |
|---|---|---|---|
| P0 Formatting and guardrails | [x] | 2026-09-24 | `.editorconfig` + analyzers added. Rules downgraded to `suggestion` (need behaviour/API changes, revisit in P15): CA1848, CA1873, CA1305, CA1707, CA1859, CA1716, CA1000, CA1725, CA1816, CA1822, CA1852, CA1068, CA1711, CA1861. 350 tests green (Docker available). |
| P1 Security and privacy | [x] | 2026-09-24 | Deleted POST /users, GET /playlists; /playlists/users/{id} lost onlyPublic (owner sees all, others public); /playlists/{id}/tracks hides non-visible playlists; intent purpose checked, same intent for picture+audio rejected; upload validators use allow-lists in Validators/Uploads.cs; gateway replaces multi/invalid Range and rejects suffix ranges with 416 when maxBytes is set; listening-stats now requires auth and self (403 otherwise). Decisions: /playlists/{id}/cover stays public; listening-history and last-listened-track stay public. Contract changes: web-player schema regenerated, onlyPublic call removed. Test helper ApiTestFixture.SeedUserAsync replaces POST /users. |
| P2 Pipeline bugs | [x] | 2026-09-24 | Album picture failures now reported; TempRootPrefix/TempUploadsRetentionDays/TempCleanupJobIntervalSeconds class properties renamed to match appsettings (default value temp); create track/album/playlist slips have fault subscriptions (new process kinds TrackCreation/AlbumCreation/PlayListCreation, slips carry Bucket/PictureKey/AudioKey variables so cleanup works); update-source slips fault as TrackPicture-style (AlbumPicture/PlayListPicture kinds); duration stored as double; empty audio folder key now throws; sagas discard picture/audio events for missing instances. NOT verified: manual stack run with RabbitMQ (no local stack); objects under the old temporal/ prefix in the bucket, if any, need a one-off clean-up. Queues should be drained before deploy (new variable names/process kinds). |
| P3 Query and API bugs | [x] | 2026-09-24 | All 12 tasks done. Contract changes: 403 (ProblemDetails) instead of 401 for not-owner; NotFound/Conflict now ProblemDetails; page params validated (pageNumber>=1, pageSize 1..100, bound through Api PageQuery, default size 20) and /albums/recent limit 1..50 -> 400; /users/{id}/last-listened-track returns 204 when empty; DELETE /albums/{id} is now async (Removing + DeleteAlbumEvent + DeleteAlbumRoutingSlipBuilder). Web-player: schema regenerated, fetchAllPages helper replaces pageSize 200/500 calls. Numbering decision: playlist positions 0-based (renumbered on removal), album track numbers 1-based (already renumbered). Migration PlayListTrackUnique (dedupes rows, unique index). Covers: resized names stay null until processed (AlbumPictures/PlayListPictures.Pending, cover falls back to original, S3 NoSuchKey -> GetFileAsync returns null -> 404). PageRequest helper + AppErrors + TrySaveChangesAsync created here (rest of P7 helpers still pending). Queues: new DeleteAlbumEvent, drain not required. |
| P4 Dead code | [x] | 2026-09-24 | All 12 tasks. Merged the two migrations into one: DropUploadTableAndTrackAudioRetryColumns. Removed SingleFlightCache (+test, Caching.Memory), IStorageService.GetUrlAsync/RemoveFolderAsync, IAudioTranscoderService.IsValidAudioFileAsync, DownloadFileFromUrl activity/args/log, Upload entity/UploadState/IDatabase.Uploads and StorageService DB writes (StorageService no longer depends on IDatabase), CurrentUser.HasClaim/FindClaim/Principal. Newtonsoft.Json pin moved to Infrastructure.csproj (Hangfire pulls a vulnerable 11.0.1 transitively; removing it from Api/Worker triggered NU1903). Api/Worker lost their redundant package references. AppIDatabase alias removed (Database now names Musify.Application.Contracts.IDatabase in full because it clashes with EF Storage.IDatabase). Kept AddUploadIntentConfiguration (Worker needs it until P13). Worker UserSecretsId left as is. Removed AudioTranscoder/Workers from Api appsettings, Worker AudioTranscoder.Routes.WorkingDirectory, and the matching env vars in deploy/compose.yml. docs/architecture.md and development.md updated. |
| P5 Configuration | [x] | 2026-09-24 | Pictures.cs (PictureSize, PictureRoutes, PictureSizes, IPictureOwnerConfiguration, PictureSizeParser), ImageSizes, IConfigurationOptions (static abstract SectionName; every config POCO implements it) and AddValidatedOptions<T>(services, configuration) added; all inline/RegisterValidatedOptions copies removed. Config key renames: ParentFolders->ParentFolder, ProcessedAudioFolder->ProcessedAudiosFolder, <Size>Picture<Width|Height> -> <Size><Width|Height> (Api and Worker appsettings updated, nothing in deploy/ used them). BuildTempAudioPath/BuildTempPicturePath -> BuildTempPath; BuildSmall/Medium/LargePicturePath -> BuildPicturePath(PictureSize, name); the un-prefixed BuildOriginalPicturePath(name) overload is gone. Events still carry Small/Medium/Large ImageSize (ImageSizes in events comes in P8.4). serviceDescriptors renamed to services. Gateway: StreamTicketValidationOptions -> StreamTicketValidationConfiguration, injected as POCO. TrackRoutes keeps PresetXPicturePath props (still used). |
| P6 Domain | [x] | 2026-09-24 | EntityPictures replaces AlbumPictures/PlayListPictures (Pending factory); IHasLifeCycle and IOwnedEntity live in Domain/Abstractions (Domain cannot reference Application); PlayList.UserId -> OwnerUserId ([Column(UserId)]) and User nav -> Owner; entity noise removed (redundant Required/initializers/pragma, nav = null!, Track.Owner no longer required; CreateTrack still loads the user because the response needs the artist name); PlaylistVisibility -> PlayListVisibility (OpenAPI schema name changed, web-player regenerated, check clean); explicit Table attributes with current names (tables NOT renamed); UploadIntent.IsExpired -> IsExpiredAt(now). Verified with an empty migration (deleted); only the model snapshot changed. No schema migration. |
| P7 Application helpers | [x] | 2026-09-24 | Pagination.cs (PageRequest, PaginatedResponse, ToPaginatedAsync), TextNormalizer, AppErrors, OwnershipExtensions.FindOwnedAsync (treats non-active as NotFound; IOwnedEntity has no Id, IHasLifeCycle supplies it), Track/Album/PlayListProjections. X.PagedList removed. GetTracks/GetAlbums return PaginatedResponse directly (contract: no more track/album wrapper; web-player updated). List queries keep int PageNumber/PageSize in the query record and build PageRequest inside the handler. PlayListApplicationResponse gained ownerUserId, trackCount, durationSeconds (web-player P3.1/P8.1 unblocked). Plain return types for GetUsers, GetLikedTracks, GetAlbums, GetTracks, GetPlayListsByUserId, GetRecentlyListenedAlbums, GetMixesByUserId, GetListeningHistory (IReadOnlyList). Inline responses moved to Responses/ (cover locations merge in P8). Create/Update album+playlist, Request*Upload, CreateTrack and RequestTrackUploadUrls keep their try/catch until they are rewritten in P8/P9. |
| P8 Pictures feature | [x] | 2026-09-24 | UploadIntentIssuer (IssueAsync/IssueOneAsync/IssuePictureAsync: user check, quota in a serializable transaction, presigned URLs issued AFTER the quota check) shared by album/playlist/track upload commands; PictureUploadResponse replaces AlbumPictureUploadResponse/PlayListPictureUploadResponse (OpenAPI schema renamed, web-player regenerated). GetCoverQuery(CoverOwner, Id, PictureSize)+CoverLocation replace the three cover queries; API uses CoverEndpoint.Stream helper (done here instead of P12.2). PictureSourceChange.PrepareAsync used by create/update album+playlist; events use the intent Bucket. UploadIntentValidator now injects UploadIntentConfiguration; quota is one aggregate query, uses the audio default for audio intents and ignores intents already past ExpiresAt. Events and Publish*Arguments carry ImageSizes Sizes (message contract change: drain queues before deploying). Update/Create handlers dropped their try/catch. |
| P9 Responsibilities | [x] | 2026-09-24 | StartListeningCommand replaces GetTrackStreamQuery (creates the ListeningHistory); TrackStreamIssuer is DB-free. LongAsString converters live in Api/Serialization and are registered globally (OpenAPI transformer marks response longs as string, request longs stay integer|string; web-player schema types unchanged). Tag rules only in CreateTrackRequestValidator, RecordListeningProgressRequestValidator added, genre bound with GenreParameter (400 on unknown). POST /playlists/{id}/tracks and POST /albums/{id}/tracks take { trackIds } (1-500), skip tracks already present and answer 204 (single-track routes removed; adding a duplicate is now a silent success, no 409); web-player updated, album->playlist uses one request. UserHasTrack dropped (migration DropUserHasTrack; reads use Tracks.OwnerUserId). Mix stores MixKind instead of Spanish text (migration MixKind maps the old daily title to Daily); MixApplicationResponse now has kind, web-player renders the texts (data/mixes.ts). GenerateMixesForUser relies on cascade delete. Decision on mix texts: done. |
| P10 Infrastructure services | [x] | 2026-09-24 | StorageService is a singleton without log-and-rethrow (GetFileAsync null on NoSuchKey), IStorageService.GetUploadUrlAsync now (bucket,key,contentType,expiry,preventOverwrite,ct). AudioService -> AudioTranscoderService with a single ExecuteFfmpegAsync. PictureService and MassTransitEventBus simplified. Jobs: all four are Hangfire recurring jobs registered by RecurringJobsRegistrar (hosted service) inside AddJobs (was AddDailyMixGenerationJob); logic lives in ExpireUploadIntentsCommand / DeleteStaleTempUploadsCommand; TemporalUploadsCleanUpJob renamed TempUploadsCleanupJob. Hangfire cron has 1-minute resolution, so the configured *Seconds intervals are rounded up to whole minutes (UploadIntent ExpirationJobIntervalSeconds, TempCleanupJobIntervalSeconds). GenerateMixesForUser is now a plain ICommand. JwtBearerEventsHandler disposes request/response and reads the userinfo endpoint from the OIDC discovery document (falls back to /oidc/v1/userinfo). Observability for the Worker is wired in the Worker part. |
| P11 MassTransit | [x] | 2026-09-24 | RoutingSlips helper (Create/AddStep/TrackFaults/AddRemoveFile) replaces the slip preamble and EndpointHelper; RoutingSlipVariableNames moved to the RoutingSlip namespace. Merged builders: PictureSourceRoutingSlipBuilder (album+playlist) and DeleteRoutingSlipBuilder (track/album/playlist). Generic MarkLifeCycleActivity<T>/DeleteEntityActivity<T> and UpdatePicturesActivity<T> with one thin subclass per entity (separate queues kept; IDatabase gained Set<T>()). Album/playlist picture updates now return a compensation log. ProcessingFailedCleanup shared by the three failed consumers; TrackAudioStatus shared by the audio activities; consumers are expression-bodied; DI uses AddOutbox/ConfigureSagaRepository local helpers. MassTransit folder 3547 -> ~2530 lines (-29%). Contract change: new activity endpoints (mark-*-life-cycle, delete-*, update-*-picture args/logs) - drain queues before deploying. Not done: the remaining log-and-rethrow in the Files activities, and the manual end-to-end run (no local stack). |
| P12 API layer | [x] | 2026-09-24 | Routes: resource is `{id:guid}`, sub-resource `{trackId:guid}`, user ids `{id:long}`/`{userId:long}` (OpenApiOptionsSetup maps int64 path params to string so the web-player keeps passing string ids). Contract changes: path params renamed (`albumId`/`playlistId` -> `id`, DELETE /tracks/{id}), `New*` prefix dropped from UpdateAlbumRequest/UpdatePlayListRequest, album+playlist picture DTOs/validators merged into PictureUploadRequest(+Validator), listening-stats 403 is now ProblemDetails; malformed guid/long routes now 404. Every endpoint returns IResult. Summaries in sentence case. Program: CreateBuilder(args) and RouteHandlerOptions.ThrowOnBadRequest=false (with args the test host runs as Development, which would turn bad query params into 500s). Validators/Limits.cs (web-player P7.1 must copy): track title 200, album title 200, playlist name 50, description 256 (album and playlist), release year 1877..current year+1, user name 48 (Domain only, no API validator), max 500 tracks per add request. Web-player: schema regenerated, call sites and update bodies adapted, `npm run check` clean. 365 tests green. |
| P13 Worker and Gateway | [x] | 2026-09-24 | Worker Program uses AddApplication + AddObservability + AddStreamTicketService and the container now validates (ValidateOnBuild and ValidateScopes on); checked by running the Worker in Development, it builds and starts the MassTransit endpoints. Because every Application handler is registered, the Worker also needs `Album`, `StreamGateway`, `StreamTicket` (key never read) and `Playback` sections: Worker appsettings.json now mirrors the Api for shared sections, appsettings.Development.json got the stream settings, deploy/compose.yml gives the worker StreamGateway__PublicBaseUrl, StreamTicket__PrivateKeyPath, the keys volume and keys-init dependency. IEventBus is now registered by AddMassTransitConsumers too. AddUploadIntentConfiguration removed. Gateway: kept its own options registration, no underscore fields (TicketValidator lost its unused `options` field), CreateBuilder(args), CorsPolicy constant, YARP bucket in appsettings.json is `CHANGE_ME` (compose overrides it; appsettings.Development.json carries `webapi-storage` for local runs). 365 tests green. Docs (media-processing.md Worker sections, streaming.md bucket) to update in P15. |
| P14 Tests | [x] | 2026-09-24 | ModelConfiguration.Apply(ModelBuilder) in Infrastructure/Persistence is called by Database and by the Application.Tests TestDatabase (Application.Tests now references Infrastructure; TestDatabase therefore also creates the saga tables). New Infrastructure.Tests (Infrastructure gets InternalsVisibleTo for Infrastructure.Tests and DynamicProxyGenAssembly2 so NSubstitute can mock ExecuteContext over internal argument types): ProcessingSlipFaultConsumer for every process kind plus unknown/missing kind, saga state machines (Track/Album/PlayList happy path, failure, event for a missing instance is discarded) on the MassTransit test harness, routing slip builders (create, audio workflow, the four picture-source slips, DeleteRoutingSlipBuilder against Postgres), lifecycle activities (mark/delete, missing entity is skipped, Postgres fixture instead of SQLite because the fixture already exists there), picture-update activities run inside real routing slips on the harness (execute, missing entity/variable fault the slip, compensation restores names / marks track pictures failed), RecurringJobsRegistrar.EveryCron. Api.Tests: PlayListEndpointsTests (private/public visibility, tracks hidden, per-user list, non-owner update/delete 403, non-guid id 404, name length). Observation: EveryCron maps 60+ minute intervals to whole hours by integer division (5400 s -> every hour), left as is. 432 tests green. |
| P15 Final sweep | [x] | 2026-09-24 | Metrics recomputed (D.1). Leftover greps: `Error.Unauthorized(` only in StartListening ("sign in", intended), `new ImageSize(` only in PictureSizes, no StaticPagedList/serviceDescriptors/storageHandler/PlaylistVisibility/ThrowIfNull(nameof); the two `#pragma warning disable CS8618` in TrackAudio/TrackPictures were unnecessary and removed; no try/catch around SaveChangesAsync in Application. Log-and-rethrow removed from the Copy, Download, Transfer, Upload and Resize activities (Remove keeps its NoSuchKey catch; the bare catch blocks in the audio activities mark the track failed and are not log-and-rethrow). Analyzer overrides revisited: CA1000, CA1725 (param names), CA1816, CA1852, CA1068 (cancellation tokens moved last), CA1711 (only kept off for test collection fixtures), CA1861, CA1305 (InvariantCulture), CA1859 and CA1707 (off only under Tests/) are fixed or scoped; still `suggestion`: CA1848 and CA1873 (would need [LoggerMessage] on ~110 call sites), CA1716 (`IDatabase.Set<T>` and the `Shared` namespace, deliberate) and CA1822 (stateless DI builders). Added a `private static readonly` PascalCase naming rule. `dotnet format` applied (imports order and whitespace only) and `--verify-no-changes` is clean. docs/ (architecture, media-processing, projects, storage, streaming, development, authentication) updated to the new names, and backend/CLAUDE.md added. Temporary ZzOpenApiDumpTests deleted (to regenerate the web-player schema again, run the API in Development and export /openapi/v1.json). Every row of section E points to a part that is now checked. 431 tests green (432 with the temporary dump test); NOT verified: a manual end-to-end run of the full stack (RabbitMQ, SeaweedFS, Zitadel) after the refactor. Deploy: drain the queues first (message contracts changed in P2, P8, P11, P3) and apply the migrations `PlayListTrackUnique`, `DropUploadTableAndTrackAudioRetryColumns`, `DropUserHasTrack`, `MixKind`; the worker now needs `StreamGateway__PublicBaseUrl` and `StreamTicket__PrivateKeyPath` (+ keys volume), already added to deploy/compose.yml. |

### D.1 Recalculated metrics (after P15)

| Metric | Before | After |
|---|---|---|
| Non-migration `.cs` lines (excluding tests) | 12,687 | 10,424 (-17.9 %, below the -25 to -30 % target: the plan's dedup was done, the rest is behaviour the audit added such as bulk endpoints, ownership helpers, mix kinds and Hangfire jobs). MassTransit 3,547 -> 2,482, Application 3,811 |
| `new StaticPagedList` | 7 | 0 |
| `new ImageSize(` | 15 | 3 (inside `PictureSizes`) |
| Files with block namespace | 249 | 0 (306 file-scoped; 4 files have no namespace: AssemblyInfo and the three Program.cs) |

---

## E. Findings index (audit 2026-09-24 → part)

### E.1 Bugs

| # | Finding | Where | Part |
|---|---|---|---|
| 1 | `POST /users` is anonymous; anyone can create any user id | `UserEndpoints.cs:91` | P1.1 |
| 2 | `GET /playlists` lists every user's private playlists | `PlayListEndpoints.cs:22`, `GetPlayLists.cs` | P1.2 |
| 3 | `onlyPublic=false` default exposes other users' private playlists | `PlayListEndpoints.cs:135` | P1.2 |
| 4 | Playlist tracks endpoint ignores visibility | `GetPlayListTracks.cs` | P1.2 |
| 5 | Upload intent purpose not checked; same intent usable for picture and audio | `UploadIntentValidator.cs:45`, `CreateTrack.cs` | P1.3 |
| 6 | `FileType`/`ContentType` unrestricted; long extension overflows `varchar(64)` → 500 | upload validators | P1.4 |
| 7 | Gateway passes invalid/multi `Range` unclamped → anonymous preview bypass | `TicketValidationMiddleware.cs:84` | P1.5 |
| 8 | Album picture failures never reported; saga stuck in `Processing` | `ProcessingSlipFaultConsumer.cs:22` | P2.1 |
| 9 | `Temp*` config keys don't match `Temporal*` properties → ignored | both `appsettings.json` | P2.2 |
| 10 | Create slips without fault handling → entity `Pending` forever | `CreateTrackRoutingSlipBuilder`, picture-source builders | P2.3 |
| 11 | Duration rounded to int although column is double | `TranscodeAudioActivity.cs:58` | P2.4 |
| 12 | Empty audio folder key publishes "processed" without completing | `UpdateTrackAudioActivity.cs` | P2.5 |
| 13 | (to verify) picture-update events hit finalized sagas → `_error` queues | `Sagas/*` | P2.6 |
| 14 | "Not owner" returns 401 instead of 403 | `ErrorOrHttpExtensions.cs:32` + 10 handlers | P3.1 |
| 15 | `pageSize=0` → 500; no upper bound; `limit` unbounded | all list endpoints | P3.2 |
| 16 | `totalCount` computed before `Active` filter | `GetPlayListsByUserId.cs:41` | P3.3 |
| 17 | Removed/failed rows returned by public reads | many queries | P3.4 |
| 18 | Album/playlist covers 500 until processing finishes | `CreateAlbum.cs:63`, cover queries | P3.5 |
| 19 | Double-click like/follow/add → unique violation 500; playlist tracks can duplicate | likes, follows, `AddTrackTo*`, `Database.cs` | P3.6 |
| 20 | Position `Max+1` race; playlists 0-based without renumber vs albums 1-based with | `AddTrackTo*`, `RemoveTrackFrom*` | P3.7 |
| 21 | Playlist update can't clear description; names not trimmed; invalid enum accepted | `UpdatePlayList.cs`, `CreatePlayList.cs` | P3.8 |
| 22 | N+1 in mixes list | `GetMixesByUserId.cs` | P3.9 |
| 23 | Last-listened counts uncounted listens; 404 when none | `GetLastTrackListenedByUserId.cs` | P3.10 |
| 24 | Streak resets before the day ends | `GetListeningStats.cs` | P3.11 |
| 25 | `DeleteAlbum` leaves pictures in storage | `DeleteAlbum.cs:33` | P3.12 |
| 26 | `Path.Combine(sourceDirectory, file)` with a full path | `StorageService.cs:121` | P10.1 |
| 27 | Userinfo URL hardcoded to Zitadel path | `JwtBearerEventsHandler.cs` | P10.7 |
| 28 | Wrong log message "as processing" on delete | `DeleteTrack.cs:64` | P7.5 (log policy rewrite) |

### E.2 Endpoints not used by the web-player (free to change)

`GET /playlists`, `POST /users`, `GET /users/{id}`, `GET /users/{id}/is-following`,
`GET /albums/recent`, `GET /tracks/{id}`. `GET /users/{id}/listening-stats` has one call site;
check it before P1.6.

### E.3 Quality findings

| Finding | Part |
|---|---|
| No `.editorconfig`/analyzers; mixed namespaces, BOMs (2 mid-file), missing final newlines, stray spaces, blank lines after `{`, unsorted usings, missing `private` | P0 |
| Private field naming mixed (`_x` vs `x`), field shadowing ctor parameter in gateway middleware | P0, P13 |
| Album ↔ PlayList handler clones (upload, cover, create/update picture parts) | P8 |
| `AlbumRoutes`/`PlayListRoutes`/`TrackRoutes` + 3 `*PicturesSizes`; `ParentFolders` vs `ParentFolder`; identical `BuildTempPicturePath`/`BuildTempAudioPath` | P5 |
| `AlbumPictures` ≡ `PlayListPictures` | P6 |
| 15× `new ImageSize` | P5 |
| 9× cover-ids projection, 9× listens-count projection, 7× `StaticPagedList` | P7 |
| Search wrapper responses with swapped `HasNext/HasPrevious` order | P7 |
| Owner-check boilerplate in ~12 handlers | P7 |
| try/catch around save/publish in ~15 handlers; log-and-rethrow in infrastructure | P7, P10 |
| Inconsistent errors (`NotFound()` with/without description, one code), `Result.Success` vs `new Success()`, log levels | P7 |
| Handlers returning `ErrorOr` that can't fail; lazy `IEnumerable` result | P7 |
| Responses declared inside handler files; `CreateUserCommand.ToEntity` | P7, P1 |
| Inconsistent ordering; pagination clamp in one handler only | P3, P7 |
| Query that writes (`GetTrackStream`) | P9 |
| JSON converters in Application | P9 |
| Duplicated validation (tags, listening progress, genre parsing in endpoint) | P9 |
| Endpoint orchestration in `AddPlayListTrack`; album vs playlist add-track API mismatch | P9 |
| `UserHasTrack` duplicates ownership | P9 |
| Hardcoded Spanish mix titles in Application | P9 |
| `StorageService` writes to DB (`Upload`), typo `trasnsferUtility`, duplicated key building, `CancellationToken` not last, PascalCase parameter | P4, P10 |
| `AudioService` in `AudioTranscoderService.cs`; duplicated ffmpeg execution methods | P10 |
| Two job mechanisms (Hangfire + BackgroundService); job logic in infrastructure vs command; `CleanUp`/`Cleanup` naming; misleading `AddDailyMixGenerationJob` | P10 |
| 8 copies of validated-options registration; `serviceDescriptors` vs `services`; `…Options` vs `…Configuration` | P5 |
| 7 near-identical lifecycle activities; 3 picture-update activities; 2 picture-source builders; 3 failed consumers; routing-slip preamble ×10; outbox ×2; saga repo ×3 | P11 |
| `RoutingSlipVariableNames` namespace ≠ folder | P11 |
| Cover endpoint ×3; route parameter naming; no route constraints; raw return types; `New…` DTO prefix; duplicated upload validators; duplicated `SecuritySchemeId` | P12 |
| Worker: manual options (misses Album), no observability, `ValidateOnBuild = false`, stale appsettings | P13 |
| Entity noise: redundant `[Required]`, initializers, pragma vs `null!`, `required` navigation, mixed table naming, `PlaylistVisibility` casing | P6 |
| Dead code (see P4 list) and redundant package references | P4 |
| No tests for MassTransit activities, builders, sagas; `TestDatabase` drifts from `Database` | P14 |
