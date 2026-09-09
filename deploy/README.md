# Deploy

Everything needed to run Musify, in development and in production, from the
same set of compose files — no wrapper scripts, just `docker compose`.

| File | Role |
|---|---|
| `compose.yml` | Every service, including the one-shot bootstrap jobs. No published ports, no domains, no environment-specific values. |
| `compose.dev.yml` | Development overlay: publishes the ports to the host, adds pgAdmin. |
| `compose.prod.yml` | Production overlay: adds the nginx edge and the mounted secrets. |
| `.env` / `.env.prod` | The values compose interpolates. Not versioned; templates in `.env.example` / `.env.prod.example`. |

The four applications (`api`, `worker`, `gateway`, `web-player`) sit behind the
`apps` compose profile, so a plain `up` starts infrastructure only — the usual
development setup, with the apps running from the IDE.

### The one-shot bootstrap jobs

Three services in `compose.yml` run once and exit, so a plain `docker compose
up -d` leaves the stack fully ready with no manual steps:

| Service | Does |
|---|---|
| `keys-init` | Generates the RS256 stream-ticket key pair in `./keys`, if it is not there already. |
| `migrate` | Applies the EF Core migrations (built from `backend/Musify.Migrator.Dockerfile`). |
| `zitadel-init` | Creates the Musify project and its two OIDC apps in Zitadel (or reuses them), then writes `AUTH_CLIENT_ID` / `WEB_CLIENT_ID` / `WEB_CLIENT_SECRET` into the env file. |

`api` and `worker` wait for `migrate`; `api` and `gateway` wait for
`keys-init` (`depends_on: condition: service_completed_successfully`). They do
**not** wait for `zitadel-init` — compose interpolates `${AUTH_CLIENT_ID}` once,
when a command starts, so a container that starts in the *same* `up` as
`zitadel-init` would still get the empty value it had before provisioning ran.
That's why bringing up the apps is a second command: by the time you run it,
`zitadel-init` has already finished and the env file has real values.

---

## Development

```sh
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml up -d
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml --profile apps up -d --build
```

The first command starts the infrastructure and runs the bootstrap jobs
(keys, migrations, Zitadel project/apps). The second builds and runs `api`,
`worker`, `gateway` and `web-player` in Docker too — skip it to run the apps
from the IDE instead (the usual dev loop):

```sh
dotnet run --project backend/Hosts/Musify.Api               # :5111
dotnet run --project backend/Hosts/Musify.StreamingGateway  # :8081
dotnet run --project backend/Hosts/Musify.Worker             # background
npm --prefix web-player run dev                              # :5173
```

When running the apps from the IDE, `Musify.Api` reads `Authentication:*`
from user-secrets and `web-player` reads its own `web-player/.env` — neither
looks at `deploy/.env`. Copy the three values `zitadel-init` wrote
(`AUTH_CLIENT_ID`, `WEB_CLIENT_ID`, `WEB_CLIENT_SECRET`) into
`dotnet user-secrets set Authentication:ClientId <value> --project
backend/Hosts/Musify.Api` and into `web-player/.env` once; they stay valid
until you wipe the Zitadel volume.

Add `--profile tools` to also start pgAdmin.

### Ports

| Service | Port | Used by |
|---|---|---|
| PostgreSQL | `59000` → 5432 | API + Worker (EF Core) and Zitadel |
| Zitadel | `8080` | OIDC/OAuth2, console at `/ui/console` |
| RabbitMQ | `5672` / `15672` | MassTransit / management UI |
| SeaweedFS | `8333` / `8888` / `9333` | S3 / filer (→ gateway) / master |
| Jaeger | `16686` / `4317` / `4318` | traces UI / OTLP gRPC / OTLP HTTP |
| pgAdmin (`--profile tools`) | `5050` | Postgres UI |
| API, gateway, web player (`--profile apps`) | `5111` / `8081` / `3000` | |

Host requirements: .NET 10 SDK, Node 22, Docker Desktop. (ffmpeg and yt-dlp
are only needed on the host if you run the Worker outside Docker — inside
Docker its image already has them.)

### Why `host.docker.internal`

`PUBLIC_AUTH_URL` and `S3_PUBLIC_URL` point at `host.docker.internal`, which
resolves to the same address from the browser, from apps running on the host
and from inside the containers. That keeps the OIDC token issuer and the
presigned S3 URLs valid whichever way the apps are running. Zitadel routes by
the `Host` header, so the domain has to be identical everywhere; changing it
requires wiping the Postgres/Zitadel volumes and starting over (`docker
compose -f compose.yml -f compose.dev.yml down -v`, then `up -d` again).

### Resetting

```sh
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml down          # stop, keep data
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml down -v       # stop, wipe volumes
```

---

## Production

On a server with a domain, from the repository root:

```sh
cp deploy/.env.prod.example deploy/.env.prod
```

Fill it in:

1. Replace every `example.com` with your domain and every `CHANGE_ME` with a
   random value (`openssl rand -hex 24`; the Zitadel masterkey needs exactly
   32 characters). `chmod 600 deploy/.env.prod`.
2. Drop **`deploy/nginx/certs/origin.pem` and `origin.key`** — the origin
   certificate nginx serves. The stack assumes Cloudflare in *Full (strict)*
   mode in front of it, so a Cloudflare Origin Certificate is enough; any
   certificate valid for the five hostnames works.
3. Point `example.com`, `www`, `api`, `auth`, `stream` and `s3` at the server,
   proxied/TLS-terminated the same way.

Then, same two commands as development, with the prod env file and overlay:

```sh
docker compose --env-file deploy/.env.prod -f deploy/compose.yml -f deploy/compose.prod.yml up -d
docker compose --env-file deploy/.env.prod -f deploy/compose.yml -f deploy/compose.prod.yml --profile apps up -d --build
```

The first command starts the infrastructure, the nginx edge and the bootstrap
jobs; the second builds and starts the applications, already wired to the
OIDC client ids the first one produced. Both are idempotent — run them again
to deploy a new version (`docker compose ... --profile apps up -d --build`
alone is enough for an update once the stack already exists; it reuses the
infra containers and only rebuilds and restarts the apps).

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
├─ compose.yml                     # all services, incl. keys-init / migrate / zitadel-init
├─ compose.dev.yml                 # dev overlay: host ports, pgAdmin
├─ compose.prod.yml                # prod overlay: nginx edge, secrets
├─ .env.example / .env.prod.example
├─ keys/                           # RS256 stream-ticket key pair (generated)
├─ secrets/                        # mounted into the Worker (prod, optional)
├─ nginx/
│  ├─ templates/default.conf.template # vhosts, rendered with the domain by envsubst
│  ├─ snippets/proxy.conf             # proxy headers shared by every vhost
│  └─ certs/                          # origin.pem + origin.key (not versioned)
└─ zitadel/
   └─ .output/                    # PAT written by Zitadel on init (not versioned)
```

The application images are built from `backend/Musify.*.Dockerfile` (build
context `backend/`, so restore can copy the central props and every csproj)
and `web-player/Dockerfile`.

## Notes

- **S3 credentials** are rendered into the SeaweedFS identity file from
  `S3_ACCESS_KEY` / `S3_SECRET_KEY` when the container starts, so they cannot
  drift from the ones the apps are given.
- **Zitadel** runs `start-from-init`, which is idempotent: the same command
  works on an empty volume and on an existing one.
- **`latest` Zitadel images** require the standalone Login UI v2 container,
  which this stack does not run. `zitadel-init` turns that requirement off so
  the built-in `/ui/login` is used.
- **`AppSettings*.json` are PascalCase** in `Musify.Api` and `Musify.Worker`;
  on case-sensitive Linux the host looks for `appsettings*.json`, so their
  Dockerfiles create lowercase copies at publish time.
- **Re-running `zitadel-init` or `migrate`** is safe: the former reuses the
  existing project/apps by name (regenerating the web app's secret only if it
  is missing from the env file), the latter is a normal `dotnet ef database
  update`, a no-op once the schema is current.
