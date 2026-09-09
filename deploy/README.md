# Deploy

Everything needed to run Musify, in development and in production, from the
same set of compose files.

| File | Role |
|---|---|
| `compose.yml` | Every service. No published ports, no domains, no environment-specific values. |
| `compose.dev.yml` | Development overlay: publishes the ports to the host, adds pgAdmin. |
| `compose.prod.yml` | Production overlay: adds the nginx edge and the mounted secrets. |
| `.env` / `.env.prod` | The values compose interpolates. Not versioned; templates in `.env.example` / `.env.prod.example`. |
| `up.ps1` | Development orchestrator (Windows). |
| `up.sh` | Production orchestrator (the server). |

The four applications (`api`, `worker`, `gateway`, `web-player`) sit behind the
`apps` compose profile, so a plain `up` starts infrastructure only — the usual
development setup, with the apps running from the IDE.

---

## Development

```powershell
./deploy/up.ps1            # infrastructure only, apps from the IDE
./deploy/up.ps1 -Apps      # build and run the apps in Docker too
```

`up.ps1` does the whole bootstrap, with no manual steps afterwards:

1. Creates `deploy/.env` from `.env.example` if it is missing.
2. Generates the RS256 stream-ticket keys in `deploy/keys/` and the Worker
   scratch directory `deploy/.tmp/`.
3. Starts the stack and waits for Postgres.
4. Applies the EF Core migrations.
5. Provisions Zitadel: project, API OIDC app and web player OIDC app, then
   writes the client ids into `deploy/.env`, `Musify.Api` user-secrets and
   `web-player/.env`.
6. Prints a summary with every URL and credential.

Other switches: `-Tools` (pgAdmin), `-SkipMigrations`, `-Down` (stop, keep
data), `-Destroy` (stop and wipe the volumes).

### Ports

| Service | Port | Used by |
|---|---|---|
| PostgreSQL | `59000` → 5432 | API + Worker (EF Core) and Zitadel |
| Zitadel | `8080` | OIDC/OAuth2, console at `/ui/console` |
| RabbitMQ | `5672` / `15672` | MassTransit / management UI |
| SeaweedFS | `8333` / `8888` / `9333` | S3 / filer (→ gateway) / master |
| Jaeger | `16686` / `4317` / `4318` | traces UI / OTLP gRPC / OTLP HTTP |
| pgAdmin (`-Tools`) | `5050` | Postgres UI |
| API, gateway, web player (`-Apps`) | `5111` / `8081` / `3000` | |

### Running the apps from the IDE

```powershell
dotnet run --project backend/Hosts/Musify.Api               # :5111
dotnet run --project backend/Hosts/Musify.StreamingGateway  # :8081
dotnet run --project backend/Hosts/Musify.Worker            # background
npm --prefix web-player run dev                       # :5173
```

Host requirements: .NET 10 SDK, Node 22, ffmpeg and yt-dlp on PATH (the Worker
transcodes and downloads audio), Docker Desktop.

### Why `host.docker.internal`

`PUBLIC_AUTH_URL` and `S3_PUBLIC_URL` point at `host.docker.internal`, which
resolves to the same address from the browser, from apps running on the host
and from inside the containers. That keeps the OIDC token issuer and the
presigned S3 URLs valid whichever way the apps are running. Zitadel routes by
the `Host` header, so the domain has to be identical everywhere; changing it
requires a re-init (`./up.ps1 -Destroy; ./up.ps1`).

---

## Production

One command on the server, from the repository root:

```sh
./deploy/up.sh
```

It refuses to start until the two things it cannot generate are in place:

1. **`deploy/.env.prod`** — copied from `.env.prod.example` on the first run.
   Replace every `example.com` with your domain and every `CHANGE_ME` with a
   random value (`openssl rand -hex 24`; the Zitadel masterkey needs exactly 32
   characters). `chmod 600` it.
2. **`deploy/nginx/certs/origin.pem` and `origin.key`** — the origin
   certificate nginx serves. The stack assumes Cloudflare in *Full (strict)*
   mode in front of it, so a Cloudflare Origin Certificate is enough; any
   certificate valid for the five hostnames works.

DNS: point `example.com`, `www`, `api`, `auth`, `stream` and `s3` at the server.

Then `up.sh` generates the stream keys, starts the infrastructure and the edge,
provisions Zitadel (it needs `curl` and `jq` on the server), applies the
migrations and finally builds and starts the applications. It is idempotent —
run it again to deploy a new version, or `./deploy/up.sh --no-build` to restart
without rebuilding.

Only nginx publishes ports (80/443); everything else is reachable only on the
internal compose network.

### Optional secrets

`deploy/secrets/` is mounted read-only at `/secrets` in the Worker. Drop a
`youtube_cookies.txt` there if you want yt-dlp to use a cookie jar — it is
referenced from `YTDLP_ADDITIONAL_ARGUMENTS` in `.env.prod`, which you can
empty if you do not have one.

---

## Layout

```
deploy/
├─ compose.yml                     # all services, environment-independent
├─ compose.dev.yml                 # dev overlay: host ports, pgAdmin
├─ compose.prod.yml                # prod overlay: nginx edge, secrets
├─ .env.example / .env.prod.example
├─ up.ps1                          # dev orchestrator
├─ up.sh                           # prod orchestrator
├─ keys/                           # RS256 stream-ticket key pair (generated)
├─ secrets/                        # mounted into the Worker (prod, optional)
├─ nginx/
│  ├─ templates/default.conf.template # vhosts, rendered with the domain by envsubst
│  ├─ snippets/proxy.conf             # proxy headers shared by every vhost
│  └─ certs/                          # origin.pem + origin.key (not versioned)
├─ scripts/
│  ├─ common.ps1 / lib.sh          # shared helpers
│  ├─ stream-keys.ps1 / .sh        # RS256 key pair generation
│  └─ migrate.ps1 / .sh            # EF Core migrations
└─ zitadel/
   ├─ provision.ps1 / .sh          # project + OIDC apps, wires the client ids
   └─ .output/                     # PAT written by Zitadel on init (not versioned)
```

The application images are built from `backend/Musify.*.Dockerfile` (build
context `backend/`, so restore can copy the central props and every csproj) and
`web-player/Dockerfile`.

## Notes

- **S3 credentials** are rendered into the SeaweedFS identity file from
  `S3_ACCESS_KEY` / `S3_SECRET_KEY` when the container starts, so they cannot
  drift from the ones the apps are given.
- **Zitadel** runs `start-from-init`, which is idempotent: the same command
  works on an empty volume and on an existing one.
- **`latest` Zitadel images** require the standalone Login UI v2 container,
  which this stack does not run. The provisioning scripts turn that requirement
  off so the built-in `/ui/login` is used.
- **`AppSettings*.json` are PascalCase** in `Musify.Api` and `Musify.Worker`;
  on case-sensitive Linux the host looks for `appsettings*.json`, so their
  Dockerfiles create lowercase copies at publish time.
