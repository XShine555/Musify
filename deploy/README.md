# Deploy

Everything needed to run Musify itself, in development and in production,
from the same set of compose files, no wrapper scripts, just `docker
compose`. Postgres, Zitadel, SeaweedFS, RabbitMQ and Jaeger are **not**
started here: they're shared infrastructure that lives in the separate
`Infrastructure` repository and has to already be running (see its own
README). `../../Infrastructure` below is just an example path, assuming
that repo happens to be checked out as a sibling of this one. Adjust it to
wherever you actually cloned it.

| File | Role |
|---|---|
| `compose.yml` | Musify's own services, including the one-shot bootstrap jobs. No published ports, no domains, no environment-specific values. |
| `compose.dev.yml` | Development overlay: publishes Musify's ports to the host. |
| `compose.prod.yml` | Production overlay: adds the nginx edge and joins Infrastructure's `infra-net` network. |
| `.env` / `.env.prod` | The values compose interpolates. Not versioned; templates in `.env.example` / `.env.prod.example`. |

The four applications (`api`, `worker`, `gateway`, `web-player`) sit behind
the `apps` compose profile, so a plain `up` starts only the one-shot
bootstrap jobs, the usual development setup, with the apps running from the
IDE.

### The one-shot bootstrap jobs

Four services in `compose.yml` run once and exit, so a plain `docker compose
up -d` leaves the stack fully ready with no manual steps:

| Service | Does |
|---|---|
| `wait-for-postgres` | Waits for Infrastructure's Postgres to accept connections. Replaces an in-file health-check `depends_on`, which only works within the same compose file. |
| `keys-init` | Generates the RS256 stream-ticket key pair in `./keys`, if it is not there already. |
| `migrate` | Applies the EF Core migrations (built from `backend/Musify.Migrator.Dockerfile`). |
| `seaweedfs-init` | Creates Musify's own bucket on the shared SeaweedFS, if it doesn't exist yet. |
| `zitadel-init` | Creates the Musify project and its OIDC apps in the shared Zitadel (or reuses them), then writes `AUTH_CLIENT_ID` / `WEB_CLIENT_ID` / `WEB_CLIENT_SECRET` / `NATIVE_CLIENT_ID` into the env file. |

`api` and `worker` wait for `migrate`; `api` waits for `keys-init`
(`depends_on: condition: service_completed_successfully`). Nothing waits on
`zitadel-init`, `seaweedfs-init` or the shared infrastructure services
directly. Compose interpolates `${AUTH_CLIENT_ID}` once, when a command
starts, so a container that started in the *same* `up` as `zitadel-init`
would still get the empty value it had before provisioning ran. That's why
bringing up the apps is a second command: by the time you run it,
`zitadel-init` has already finished and the env file has real values.

---

## Development

Start the separate `Infrastructure` repository first, from wherever you
cloned it (see its own README for the exact command). Then, from this
repository's root:

```sh
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml up -d
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml --profile apps up -d --build
```

The first command starts Musify's own bootstrap jobs (Postgres wait, keys,
migrations, the bucket, the Zitadel project/apps). The second builds and runs
`api`, `worker`, `gateway` and `web-player` in Docker too. Skip it to run the
apps from the IDE instead (the usual dev loop):

```sh
dotnet run --project backend/Hosts/Musify.Api               # :5111
dotnet run --project backend/Hosts/Musify.StreamingGateway  # :8081
dotnet run --project backend/Hosts/Musify.Worker             # background
npm --prefix web-player run dev                              # :5173
```

When running the apps from the IDE, `Musify.Api` reads `Authentication:*`
from user-secrets and `web-player` reads its own `web-player/.env`, neither
looks at `deploy/.env`. Copy the four values `zitadel-init` wrote
(`AUTH_CLIENT_ID`, `WEB_CLIENT_ID`, `WEB_CLIENT_SECRET`, `NATIVE_CLIENT_ID`)
into `dotnet user-secrets set Authentication:ClientId <value> --project
backend/Hosts/Musify.Api` and into `web-player/.env` once. They stay valid
until Infrastructure's Zitadel volume is wiped.

### Ports

| Service | Port | Used by |
|---|---|---|
| Musify.Api | `5111` | `/scalar/v1`, `/openapi/v1.json` |
| Musify.StreamingGateway | `8081` | `/health`, `/media/...` |
| web-player | `5173` (`npm run dev`) or `3000` (container) | |

Postgres, Zitadel, SeaweedFS, RabbitMQ and Jaeger ports come from
Infrastructure. See its own README for that table.

Host requirements: .NET 10 SDK, Node 22, Docker Desktop. ffmpeg is only
needed on the host if you run the Worker outside Docker; inside Docker its
image already has it.

### Why `host.docker.internal`

`PUBLIC_AUTH_URL` and `S3_PUBLIC_URL` point at `host.docker.internal`, which
resolves to the same address from the browser, from apps running on the host
and from inside the containers. That keeps the OIDC token issuer and the
presigned S3 URLs valid whichever way the apps are running, and is the same
address Infrastructure's own dev ports are published on.

### Resetting

```sh
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml down          # stop, keep data
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml down -v       # stop, wipe Musify's own volumes
```

This only touches Musify's own containers and volumes. Resetting Zitadel or
SeaweedFS is done from the `Infrastructure` repo instead.

---

## Production

On a server with a domain:

1. Bring up the `Infrastructure` repo first, with its own `.env.prod` filled
   in. It creates the `infra-net` network this stack joins.
2. From this repository's root:

```sh
cp deploy/.env.prod.example deploy/.env.prod
```

Fill it in:

1. Replace every `example.com` with your domain and every `CHANGE_ME` with a
   random value (`openssl rand -hex 24`). `chmod 600 deploy/.env.prod`.
2. Drop **`deploy/nginx/certs/origin.pem` and `origin.key`**, the origin
   certificate nginx serves. The stack assumes Cloudflare in *Full (strict)*
   mode in front of it, so a Cloudflare Origin Certificate is enough; any
   certificate valid for the five hostnames works.
3. Point `example.com`, `www`, `api`, `auth`, `stream` and `s3` at the server,
   proxied and TLS-terminated the same way.

Then:

```sh
docker compose --env-file deploy/.env.prod -f deploy/compose.yml -f deploy/compose.prod.yml up -d
docker compose --env-file deploy/.env.prod -f deploy/compose.yml -f deploy/compose.prod.yml --profile apps up -d --build
```

The first command starts Musify's bootstrap jobs and the nginx edge; the
second builds and starts the applications, already wired to the OIDC client
ids the first one produced. Both are idempotent, so running them again
deploys a new version (`... --profile apps up -d --build` alone is enough for
an update once the stack already exists).

Only nginx publishes ports (80/443). Every other Musify service, and all of
Infrastructure's, is reachable only on the internal `infra-net` network.

---

## Layout

```
deploy/
├─ compose.yml                     # Musify's own services: wait-for-postgres, keys-init,
│                                   #   migrate, seaweedfs-init, zitadel-init, api/worker/gateway/web-player
├─ compose.dev.yml                 # dev overlay: host ports
├─ compose.prod.yml                # prod overlay: nginx edge, joins infra-net
├─ .env.example / .env.prod.example
├─ keys/                           # RS256 stream-ticket key pair (generated)
└─ nginx/
   ├─ templates/default.conf.template # vhosts, rendered with the domain by envsubst
   ├─ snippets/proxy.conf             # proxy headers shared by every vhost
   └─ certs/                          # origin.pem + origin.key (not versioned)
```

The application images are built from `backend/Musify.*.Dockerfile` (build
context `backend/`, so restore can copy the central props and every csproj)
and `web-player/Dockerfile`.

## Notes

- **S3 credentials, the Postgres admin user, the RabbitMQ user and the
  Zitadel masterkey** all live in Infrastructure's own `.env`/`.env.prod` now.
  This project's env files only duplicate the values that have to match (see
  the comments in `.env.example`).
- **Zitadel** runs `start-from-init`, which is idempotent: the same command
  works on an empty volume and on an existing one.
- **`AppSettings*.json` are PascalCase** in `Musify.Api` and `Musify.Worker`;
  on case-sensitive Linux the host looks for `appsettings*.json`, so their
  Dockerfiles create lowercase copies at publish time.
- **Re-running `zitadel-init`, `seaweedfs-init` or `migrate`** is safe: they
  reuse the existing project/apps/bucket by name, and `migrate` is a normal
  `dotnet ef database update`, a no-op once the schema is current.
