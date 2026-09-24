# Musify: backend

.NET 10 solution (`Musify.slnx`): `Core/` (Domain, Application, Infrastructure), `Hosts/` (Api, Worker, StreamingGateway) and `Tests/`. Architecture and flows are in `../docs/`.

## Commands

Run from `backend/`.

```sh
dotnet build Musify.slnx            # warnings are errors, including unused usings (IDE0005)
dotnet test Musify.slnx             # Infrastructure and Api tests need Docker (Testcontainers)
dotnet format Musify.slnx --verify-no-changes
```

- Read the totals of `dotnet test` (`total:`, `succeeded:`) before trusting it; don't chain it with `&&` into a commit.
- EF migrations: `dotnet ef migrations add <Name> --project Core/Musify.Infrastructure --startup-project Core/Musify.Infrastructure` (set `Database__ConnectionString` to any value). A model change needs a migration; the entity model lives in `ModelConfiguration.Apply`, which the in-memory test database reuses.
- An HTTP contract change (routes, DTOs, responses) needs the web-player schema regenerated (`web-player/src/lib/api/schema.d.ts`, from `/openapi/v1.json`) and `npm run check` clean.
- A MassTransit message change (events, routing slip arguments, process kinds) means the queues must be drained before deploying; note it in the commit.

## Style

- File-scoped namespaces, usings outside the namespace and sorted (`.editorconfig` enforces it).
- Private fields camelCase without underscore, constants and `static readonly` fields PascalCase, explicit accessibility.
- `ArgumentNullException.ThrowIfNull(x)` without `nameof`.
- Use `PlayList` (not `Playlist`) in code; configuration POCOs end in `Configuration`; injected services are named after their type (`storageService`).
- User-facing strings are in English (the web-player owns translation).
- Trim stored names; normalized columns go through `TextNormalizer.Normalize`.
- Format ids and numbers with `CultureInfo.InvariantCulture`; put `CancellationToken` last.

## Application layer

- One file per use case, `<Feature>/<UseCase>.cs`, holding the record and its handler; responses go in `Responses/`.
- Errors come from `AppErrors` (`NotFound`, `Forbidden` = 403 for "not the owner", `Conflict`). `Error.Unauthorized` (401) only means "not signed in". The success value is `Result.Success`; the API maps it to 204.
- Handlers that cannot fail return plain types. Lists are paginated through `PageRequest` / `ToPaginatedAsync`, ordered with an `Id` tiebreak, and public reads return only `LifeCycleStatus.Active` rows.
- Owned-entity operations go through `FindOwnedAsync` (non-active entities are not found).
- No try/catch around `SaveChangesAsync` or `PublishAsync` in handlers: the outbox makes publish + save atomic. Infrastructure either handles an error or lets it propagate; never log and rethrow.
- List query records keep `int PageNumber, int PageSize`; the API binds them through `PageQuery` and validates them with `PageQueryValidator`.

## API layer

- Routes: `{id:guid}` for the resource, `{trackId:guid}` for sub-resources, `{id:long}` / `{userId:long}` for user ids.
- Every endpoint returns `IResult` (`Results.Ok` when the handler cannot fail, `ToHttpResult()` otherwise).
- Validation limits live in `Validators/Limits.cs`; the database column length wins when a limit disagrees with the web-player. Upload allow-lists are in `Validators/Uploads.cs`.
- `.WithSummary(...)` is sentence case without a trailing period.

## Infrastructure and hosts

- MassTransit routing slips are built from `RoutingSlips.Create` / `AddStep` / `TrackFaults`; activities for the three owner types share generic bases (`MarkLifeCycleActivity<T>`, `DeleteEntityActivity<T>`, `UpdatePicturesActivity<T>`).
- Recurring work is Hangfire (`RecurringJobsRegistrar`); intervals are in seconds and rounded up to whole minutes.
- The Worker calls `AddApplication` and validates its service container on build, so any handler dependency must be registered there. Shared config sections in its `appsettings.json` mirror the API's.
- The StreamingGateway references neither Application nor Infrastructure and keeps its own options.

## Tests

- xUnit v3; pass `TestContext.Current.CancellationToken` to methods that take a token (analyzer xUnit1051).
- Test names are `Method_Scenario_Expected`.
- Application tests use the in-memory SQLite `TestDatabase`; Infrastructure and Api tests use Testcontainers (Postgres, SeaweedFS). MassTransit sagas and consumers are tested on the MassTransit test harness.
- `Infrastructure` exposes its internals to `Musify.Infrastructure.Tests`.
