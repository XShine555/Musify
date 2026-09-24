# Handoff: backend refactor, parts P12 to P15

> For the next AI. Read this file first, then `backend/docs/backend-refactor-plan.md`
> (section A, B, and the parts P12–P15, plus the Log in section D). Both files are
> **untracked on purpose**: never commit them.

## 1. Where things stand

- Worktree: `C:\Users\XShin\OneDrive\Documentos\Musify\MusifyM\.claude\worktrees\backend-refactor-plan-9b4465`
- Branch: `claude/backend-refactor-plan-9b4465` (base `master` at `87c3f97`). Nothing is pushed.
- Done and committed: **P0 to P11** (one commit per part, see `git log --oneline`).
- Left: **P12** (API layer), **P13** (Worker and Gateway hosts), **P14** (tests), **P15** (final sweep and docs).
- State at the last commit (`405df08`): build with 0 warnings, 365 tests green
  (Infrastructure tests need Docker), `npm run check` in `web-player/` clean.
- The Log (section D of the plan) already has notes for P0–P11 and is the source of truth
  for decisions taken. Update it (checkbox, date, notes) for every part you finish.

### Deviations from the plan already taken (do not undo)

- `IOwnedEntity` lives in `Domain/Abstractions` and has no `Id`; `IHasLifeCycle` provides it.
  `FindOwnedAsync` requires both and treats non-active entities as NotFound.
- List query records keep `int PageNumber, int PageSize`; the handler builds a `PageRequest`.
  The API binds them through `Api/Models/PageQuery` (camelCase names) + `PageQueryValidator`.
- Events and `Publish*Arguments` carry `ImageSizes Sizes`.
- Cover endpoint helper `CoverEndpoint.Stream` already exists (this was P12.2).
- Mix titles were replaced by `MixKind`; the web-player renders the text (`src/lib/data/mixes.ts`).
- The two migrations planned in P4 were merged into `DropUploadTableAndTrackAudioRetryColumns`.
- Adding a duplicate track to a playlist or album is now a silent success (bulk endpoints).
- Hangfire cron has 1-minute resolution, so job intervals in seconds are rounded up to minutes.
- Pending inside P11: log-and-rethrow still present in `MassTransit/Activities/Files/*`
  (Copy, Download, Remove, Transfer, Upload). Clean it in P15 or when touching them.

## 2. Rules the user gave (follow them exactly)

1. **Language of the conversation: Spanish.** Talk to the user in Spanish.
2. **Commit messages: English, only about the code that changed.** Never mention the plan,
   part numbers (P0, P1...), the Log, or "step X". One commit per part, imperative mood,
   e.g. `Simplify infrastructure services and move all recurring jobs to Hangfire`.
   (This is also saved in the memory file `feedback_commit_messages.md`.)
3. **Never include the plan documents in commits.** `backend/docs/backend-refactor-plan.md`
   and `backend/docs/backend-refactor-handoff.md` stay untracked. Also never commit the
   temporary test `backend/Tests/Musify.Api.Tests/Endpoints/ZzOpenApiDumpTests.cs`.
   Stage with `git add backend web-player` and then
   `git reset -q backend/docs backend/Tests/Musify.Api.Tests/Endpoints/ZzOpenApiDumpTests.cs`.
4. **Do the plan in parts, one part per commit, in order.** Do not mix parts. Never mix
   formatting with behaviour.
5. **When the user says to stop at a point, stop there** (the last message was "para en esta
   parte cuando termines", so work stopped after P11). Only continue when asked.
6. Follow the plan's own rules: read section A first; create a shared helper from section B
   yourself if it does not exist yet; keep the Log and the findings index (section E) up to date;
   a contract change needs the web-player regeneration (section 4 below); an EF model change
   needs a migration and `TestDatabase` kept in sync; a MassTransit message change must be
   noted in the Log ("drain queues before deploying").
7. Pronouns: use they/them for anyone whose pronouns are not stated.
8. Outward-facing or hard-to-reverse actions (push, PR, deleting things) need explicit approval.
   Nothing has been pushed; do not push or open a PR unless asked.
9. Report faithfully: if a test fails or something was skipped or unverifiable
   (e.g. manual end-to-end runs), say so in the Log and in the final message.

## 3. Coding conventions (from the plan, section B.2, keep applying them)

- File-scoped namespaces, usings outside, sorted (`.editorconfig` enforces it; the build treats
  warnings as errors, including unused usings IDE0005).
- Private fields camelCase without underscore; constants PascalCase; explicit accessibility.
- Application: `<Feature>/<UseCase>.cs` holds record + handler; responses in `Responses/`.
- Errors via `AppErrors` (`NotFound`, `Forbidden` = 403 for "not owner", `Conflict`);
  `Unauthorized` (401) only for "not signed in". Success value is `Result.Success`.
- No try/catch around `SaveChangesAsync`/`PublishAsync` in handlers (outbox: publish then save
  is atomic). Infrastructure either handles or rethrows, never log-and-rethrow.
- Handlers that cannot fail return plain types; lists are paginated, ordered with `Id` tiebreak,
  public reads return only `LifeCycleStatus.Active`.
- Text: trim stored names; normalized columns use `TextNormalizer.Normalize`.
- `ArgumentNullException.ThrowIfNull(x)` without `nameof`.
- User-facing strings in English (the web-player owns translation).
- Naming: `PlayList` everywhere; config POCOs end in `Configuration`; injected services are
  named after their type (`storageService`).

## 4. Practical procedures (learned the hard way)

Run everything from `backend/` unless stated.

### Build, format fixes, tests

```bash
dotnet build Musify.slnx
dotnet test Musify.slnx
```

- Do **not** chain `dotnet test ... | grep && commit`: a failing run can look successful.
  Read the totals (`total:`, `succeeded:`) before committing.
- Infrastructure and Api tests need Docker Desktop running. If `docker info` fails, start
  `C:\Users\XShin\AppData\Local\Programs\DockerDesktop\Docker Desktop.exe` and wait.
- Unused-using errors (IDE0005) are common after edits. A helper script removes them:
  `python <scratchpad>/fixusings.py` (loops `dotnet build`, deletes the flagged using lines).
  If the scratchpad is gone, do it by hand or with `dotnet format`.

### Editing files from the shell (important)

- The Bash tool **fails to parse** many commands that combine several heredocs, inline
  `python - <<'EOF'` with backslashes/quotes, or big C# blocks ("unexpected EOF while looking
  for matching `'`"). When that happens nothing runs. Use the **Write tool** for files and put
  Python edit scripts in a file (in the scratchpad) and run `python that_file.py`.
- Working tree files use LF while git has autocrlf on; warnings like "LF will be replaced by
  CRLF" are noise. Redirect stderr (`2>/dev/null`) on `git add/commit`. Python edit helpers
  should read/write with `newline=''` and preserve CRLF if present.

### EF migrations

```bash
export Database__ConnectionString="Host=localhost;Database=x;Username=x;Password=x"
dotnet ef migrations add <Name> --project Core/Musify.Infrastructure --startup-project Core/Musify.Infrastructure
```

- `dotnet ef migrations remove` needs a DB connection and fails offline: delete the migration
  files by hand and `git checkout` the snapshot, then re-add.
- To verify a model refactor keeps the schema: add a migration named `Check`, confirm `Up`
  is empty, delete its two files (keep snapshot changes if they only rename types).
- Hand-edit migrations when data must be preserved (see `MixKind`, `PlayListTrackUnique`).
- `Application.Tests/TestSupport/TestDatabase.cs` mirrors part of `Database.OnModelCreating`
  (P14.3 will extract `ModelConfiguration.Apply`).

### Web-player contract regeneration (when an HTTP contract changes)

1. `web-player/node_modules` is a junction to the main checkout's `node_modules`
   (git-ignored). If missing:
   `cmd //c "mklink /J web-player\node_modules ..\..\..\web-player\node_modules"` from the worktree root.
2. The OpenAPI document is dumped by the temporary test `ZzOpenApiDumpTests` (untracked; it
   returns early unless env `OPENAPI_OUT` is set and runs the API in Development). Script used:

```bash
cd backend
export ASPNETCORE_ENVIRONMENT=Development OPENAPI_OUT="<scratchpad>/openapi.json"
dotnet test Tests/Musify.Api.Tests --filter "FullyQualifiedName~ZzOpenApiDump"
cd ../web-player
npx -y openapi-typescript@7 "<scratchpad>/openapi.json" -o src/lib/api/schema.d.ts
npm run check
```

   If the test file is gone, recreate it: it fetches `/openapi/v1.json` through
   `fixture.CreateAnonymousClient()` and writes it to `OPENAPI_OUT`.
   P12.1 changes `WebApplication.CreateBuilder(args)`; keep the dump working afterwards.
3. Fix web-player call sites, run `npx prettier --write` on touched files, then `npm run check`.
4. Delete `ZzOpenApiDumpTests.cs` at the very end (P15).

## 5. What is left, part by part

### P12: API layer (plan section C, P12)

1. `Program.cs`: `WebApplication.CreateBuilder(args)`, sorted usings, CORS constant at top.
2. Cover endpoints: **already done** with `CoverEndpoint.Stream`.
3. Routes: resource id is `{id}`, sub-resources `{trackId}`; Guid routes use `:guid`,
   user ids `:long`. Playlist routes keep `/playlists`. (Contract change: regenerate.)
4. Every endpoint returns `IResult` through `ToHttpResult` (some still return raw types,
   e.g. `GetListeningHistory`, `IsFollowing`); use `Results.Ok` where the handler cannot fail.
5. DTOs: drop the `New...` prefix (`UpdateAlbumRequest(Title, Description, ReleaseYear,
   PictureIntentId)`; same for playlists). Decision recommended in the plan: rename.
6. Validators: merge the album/playlist picture upload DTOs into one
   `PictureUploadRequest` and one validator (the responses are already merged).
7. `OpenApiOptionsSetup` and `ScalarOptionsSetup` share the `SecuritySchemeId` constant.
8. `.WithSummary(...)` strings in sentence case, no trailing period. (Current ones are Title Case.)
9. `JwtBearerOptionsSetup`: single-line guard without braces.
10. `Validators/Limits.cs` with every validation limit (playlist name 50, descriptions 256,
    titles 200, user name 48, release year range). List them in the Log for the web-player
    plan (P7.1). Database column length wins on disagreement.

Coordination table (section A.4): P3.1 and P8.1 of the web-player plan are unblocked by P7;
P8.4 (bulk add) by P9; P7.1 (shared limits) by P12.10. Tick them in the web-player plan's Log too
(`web-player/docs/frontend-audit-plan.md`, also untracked).

### P13: Worker and Gateway hosts

1. Worker `Program.cs`: call `AddApplication(configuration)` instead of registering Application
   options by hand (Album config is missing today), add `AddObservability`, remove
   `ValidateOnBuild = false` and make the container validate. Register the extra POCO configs
   the handlers need (stream tickets, gateway config) rather than disabling validation.
   Then `AddUploadIntentConfiguration` (kept only for the Worker) can go.
2. Worker `appsettings.json`: complete and consistent with the Api for shared sections
   (add `Album`; keys were renamed in P5), remove stale ones.
3. Gateway: keep option registration local and self-contained; field naming without
   underscores (`TicketValidator` uses `_options`, `_rsa`, ...; the middleware field that
   shadowed a constructor parameter is already gone); replace the default bucket in the
   YARP transform of `appsettings.json` with a placeholder like `/buckets/CHANGE_ME/{**rest}`
   (compose overrides it) or document it in `docs/streaming.md`.
   Check `deploy/` when renaming config keys.

### P14: Tests

1. Add tests for what has none: MassTransit activities (generic lifecycle and picture-update
   ones with the SQLite `TestDatabase`), `ProcessingSlipFaultConsumer` (every process kind),
   routing slip builders (activity order and arguments), saga state machines with MassTransit's
   test harness (happy path, failure, missing instance on update; sagas already use
   `OnMissingInstance(Discard)`), `RecurringJobsRegistrar.EveryCron` (internal, may need
   `InternalsVisibleTo`).
2. Endpoint tests for visibility and ownership if not already covered (P1, P3 added several).
3. Extract the owned-type and index configuration of `Database` into
   `ModelConfiguration.Apply(ModelBuilder)` in Infrastructure and call it from `Database` and
   `TestDatabase` (`Application.Tests` then references Infrastructure; note it in the Log).

### P15: Final sweep and documentation

1. Recompute the metrics of section A.5 (script in the plan) and fill D.1 (baseline was
   12,687 non-migration, non-test `.cs` lines; targets in A.5). Also record the MassTransit
   line count (3,547 to about 2,530) and the Application line count.
2. Grep for leftovers: `Error.Unauthorized(`, `StaticPagedList`, `new ImageSize(` (only in
   `PictureSizes.ToImageSizes`), `ThrowIfNull(.*nameof`, `#pragma warning disable CS8618`,
   try blocks around `SaveChangesAsync` in `Application/`, `serviceDescriptors`,
   `storageHandler`, `PlaylistVisibility`, log-and-rethrow in `MassTransit/Activities/Files`.
3. The analyzer rules downgraded to `suggestion` in `.editorconfig` in P0 (CA1848, CA1873,
   CA1305, CA1707, CA1859, CA1716, CA1000, CA1725, CA1816, CA1822, CA1852, CA1068, CA1711,
   CA1861) should be revisited: fix the cheap ones and remove their overrides.
4. Run `dotnet format Musify.slnx` and check the diff is whitespace only (some test files have a
   stray blank line before the closing brace).
5. Update `docs/architecture.md`, `docs/media-processing.md`, `docs/projects.md`,
   `docs/storage.md`, `docs/streaming.md` with the new names (jobs, entities, routes, config
   keys such as `ParentFolder`, `Temp*`, activity/queue names, `MixKind`).
6. Add a short `backend/CLAUDE.md` with the conventions of section 3 above (this one **is**
   committed).
7. Delete `ZzOpenApiDumpTests.cs`, keep the plan (section E rows should all point to a checked
   part in D), and remove the `web-player/node_modules` junction only if desired (it is ignored).

## 6. Deployment notes to keep in the Log

Queues must be drained before deploying because these message contracts changed:
processing-slip process kinds and variables (P2), `ImageSizes` in events and arguments (P8),
new lifecycle/delete/update-picture activities and arguments (P11), `DeleteAlbumEvent` (P3).
Migrations added so far: `PlayListTrackUnique`, `DropUploadTableAndTrackAudioRetryColumns`,
`DropUserHasTrack`, `MixKind`. Old bucket objects under the `temporal/` prefix (from before the
`Temp*` config fix) may need a one-off clean-up.

## 7. Suggested first steps for the next session

1. `git log --oneline -14` and `git status --short` (expect only the two untracked docs and
   `ZzOpenApiDumpTests.cs`).
2. Run build + tests to confirm the green baseline.
3. Start P12, commit, update the Log, then continue only if the user has not asked to stop.
