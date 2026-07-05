# Development deploy

Everything the backend needs for development, in one place. A single
`docker-compose.yml` brings up **PostgreSQL, Zitadel, RabbitMQ, SeaweedFS and
Jaeger**; the .NET apps run from the IDE (or, optionally, in Docker too).

## Quick start

```powershell
# From the repo root (Musify/)
./deploy/up.ps1
```

`up.ps1` does the full bootstrap:

1. Creates `deploy/.env` from `.env.example` if missing.
2. Starts the stack (`docker compose up -d`) and waits for Postgres to be healthy.
3. Creates the `webapi-storage` bucket in SeaweedFS (`seaweedfs-init` service).
4. Generates the RS256 stream-ticket keys in `D:\weed` if missing.
5. Creates the Worker temp directory (`D:\tempsFilesDev`).
6. Applies the EF Core migrations to `musify_db`.
7. **Provisions Zitadel automatically** (project + OIDC app with JWT token) and
   wires the ClientId into `Musify.Api` user-secrets and `.env`.

When it finishes it prints a summary with URLs and credentials. **No manual steps.**

### Without the script (infra only)

```powershell
docker compose -f deploy/docker-compose.yml up -d
```

The compose file still creates the bucket on its own; run migrations with
`dotnet ef database update` (see [../docs/development.md](../docs/development.md)).

## Services and ports

| Service | Port(s) | Used for |
|---|---|---|
| PostgreSQL | `59000` → 5432 | API + Worker (EF Core) and the Zitadel database |
| Zitadel | `8080` | OIDC/OAuth2 (console at `/ui/console`) |
| RabbitMQ | `5672` / `15672` | MassTransit / management UI |
| SeaweedFS S3 | `8333` | S3 (presigned PUT/GET) |
| SeaweedFS filer | `8888` | serves files → StreamingGateway |
| SeaweedFS master | `9333` | admin/UI |
| Jaeger | `16686` / `4317` / `4318` | OTLP traces (UI / gRPC / HTTP) |

Ports are published to the host, so apps run locally connect without changing
`AppSettings.Development.json` (Postgres, RabbitMQ, S3, OTLP).

## Authentication (Zitadel) — automatic

There are **no manual steps**. `up.ps1` runs `zitadel/provision.ps1`, which
idempotently:

1. Uses a **service account** with a **PAT** that Zitadel writes on init
   (`zitadel/.output/admin-sa.pat`).
2. Creates (or reuses) the **project** `Musify` and an **OIDC app** (PKCE,
   `devMode`, **access token = JWT** — required for `JwtBearer` to validate).
3. Captures the generated **ClientId** and writes it to:
   - **`Musify.Api` user-secrets** (for running the apps locally), and
   - `deploy/.env` as `AUTH_CLIENT_ID` (for the full-docker path).

The values stay stable while the Postgres volume persists. After `up.ps1 -Destroy`
the next `up.ps1` re-provisions (new ClientId, re-wired).

- Console: `http://host.docker.internal:8080/ui/console`
- Admin login: `admin@zitadel.host.docker.internal` / `Password1!` (see `.env`).
- Test login: open Scalar (`http://localhost:5111/scalar/v1`) and authorize.

### Why `host.docker.internal`

`ZITADEL_EXTERNAL_DOMAIN=host.docker.internal` forms the token *issuer* and
resolves the **same** from the browser, local apps and containers, so token
validation works both ways (local and full-docker). Zitadel routes by the `Host`
header, so the domain must be identical everywhere. If you only run the apps
locally you can use `localhost`. Changing the domain requires a re-init:
`up.ps1 -Destroy; up.ps1`.

## Running the apps

### Recommended: from the IDE (best for debugging)

```powershell
dotnet run --project backend/Musify.Api               # :5111
dotnet run --project backend/Musify.StreamingGateway  # :8081
dotnet run --project backend/Musify.Worker            # background
```

Host requirements: **.NET 10 SDK**, **ffmpeg** on PATH (Worker audio transcode).

### Optional: everything in Docker

```powershell
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.apps.yml up -d --build
```

Builds and runs `api`, `worker` and `gateway` as containers (the Worker already
ships ffmpeg). The stream-ticket keys are mounted from `D:\weed` (configurable via
`STREAM_KEYS_DIR` in `.env`). Auth works in this mode too (the API downloads the
Zitadel JWKS via `host.docker.internal`).

> Note: on Linux the apps look for `appsettings*.json` in lowercase. The `Api`/
> `Worker` projects use `AppSettings*.json` (PascalCase), so their Dockerfiles
> create lowercase copies at publish time. On Windows it did not matter
> (case-insensitive).

## Useful commands

```powershell
./deploy/up.ps1 -SkipMigrations   # bring up without touching the DB
./deploy/up.ps1 -Down             # stop and remove containers (keep data)
./deploy/up.ps1 -Destroy          # stop and remove containers + volumes (wipe data)

docker compose -f deploy/docker-compose.yml logs -f zitadel
docker compose -f deploy/docker-compose.yml ps
```

## Project layout — what each file does

```
deploy/
├─ docker-compose.yml         # infrastructure stack (single source of truth)
├─ docker-compose.apps.yml    # optional overlay: the 3 .NET apps in Docker
├─ .env                       # local config (secrets/ports); not versioned
├─ .env.example               # config template (versioned)
├─ up.ps1                     # orchestrator: up / down / destroy
├─ README.md                  # this file
├─ scripts/
│  ├─ common.ps1              # shared helpers (Read-DotEnv, Write-Step)
│  ├─ stream-keys.ps1         # generates the RS256 stream-ticket key pair
│  └─ migrate.ps1             # applies EF Core migrations
├─ seaweedfs/
│  └─ s3.conf                 # SeaweedFS S3 identities/credentials
└─ zitadel/
   ├─ provision.ps1           # creates project + OIDC app, wires the ClientId
   └─ .output/                # PAT + init secrets written by Zitadel (not versioned)

backend/
├─ Musify.Api.Dockerfile              # API image
├─ Musify.Worker.Dockerfile           # Worker image (includes ffmpeg)
├─ Musify.StreamingGateway.Dockerfile # Gateway image
└─ .dockerignore
```

### Compose

- **`docker-compose.yml`** — the infra stack (`postgres`, `zitadel`, `rabbitmq`,
  `seaweedfs`, the one-shot `seaweedfs-init` bucket creator, and `jaeger`). This is
  the single source of truth for services, ports and volumes. Values come from `.env`.
- **`docker-compose.apps.yml`** — an optional overlay that adds the `api`, `worker`
  and `gateway` services. It merges with the infra file (same `musify-dev` project
  and network) and shares the backend env via YAML anchors. Used to run the whole
  stack in Docker; for active development prefer running the apps from the IDE.

### Config

- **`.env`** — the actual values used by compose (Postgres/RabbitMQ/Zitadel/S3
  credentials, ports, host paths, `AUTH_CLIENT_ID`). Ignored by git (`*.env`).
- **`.env.example`** — the versioned template. Copy it to `.env` (or let `up.ps1`
  do it) and adjust.

### Scripts (PowerShell)

- **`up.ps1`** — the orchestrator. Parses `.env`, starts the stack, waits for
  Postgres, then calls the sub-scripts in order (keys → migrations → provisioning)
  and prints the summary. Also handles `-Down` / `-Destroy` / `-SkipMigrations`.
- **`scripts/common.ps1`** — shared helpers dot-sourced by the others: `Read-DotEnv`
  (parse `.env` into a hashtable) and `Write-Step` (step logging).
- **`scripts/stream-keys.ps1`** — generates the RS256 key pair that the API uses to
  sign stream tickets and the Gateway uses to verify them. No-op if the keys exist.
- **`scripts/migrate.ps1`** — sets the connection string in user-secrets and runs
  `dotnet ef database update` (startup project = `Musify.Infrastructure`).
- **`zitadel/provision.ps1`** — reads the service-account PAT, creates/reuses the
  Zitadel project and OIDC app (JWT token), and wires the ClientId into
  `Musify.Api` user-secrets and `.env`. Idempotent; can be run standalone.

### Service configs

- **`seaweedfs/s3.conf`** — the S3 identities (access/secret keys) SeaweedFS loads.
  Must match `InfrastructureStorage:*` in the app settings and `S3_*` in `.env`.
- **`zitadel/.output/`** — created at runtime; holds the PAT Zitadel writes on init
  (`admin-sa.pat`). Not versioned; wiped by `up.ps1 -Destroy`.

### Dockerfiles (in `backend/`)

- **`Musify.Api.Dockerfile`** / **`Musify.Worker.Dockerfile`** /
  **`Musify.StreamingGateway.Dockerfile`** — multi-stage builds for each host. The
  Worker image also installs ffmpeg. Built by the apps overlay; build context is
  `backend/` so restore can copy the central props and all csproj files.
- **`.dockerignore`** — keeps `bin/`, `obj/`, IDE folders and `DesignSettings.json`
  out of the build context.
