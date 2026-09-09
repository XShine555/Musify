# Musify

Music streaming platform: upload your own tracks (or import them from YouTube),
process them asynchronously (audio transcoded to `.m4a`, thumbnails generated)
and stream them without the bytes ever going through the API.

```
backend/      .NET 10 — Domain / Application / Infrastructure / Api / Worker / StreamingGateway
web-player/   SvelteKit 2 + Svelte 5 + Tailwind 4 web client
deploy/       Docker Compose stacks — self-contained, no wrapper scripts
docs/         What each piece does and why (in Spanish)
```

## Run it

Development (Docker Desktop):

```sh
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml up -d
```

Infrastructure plus stream-ticket keys, database migrations and Zitadel OIDC
provisioning — all as one-shot jobs baked into the compose file. Apps run from
the IDE by default; add `--profile apps up -d --build` as a second command to
run them in Docker instead.

Production, on a server with a domain:

```sh
cp deploy/.env.prod.example deploy/.env.prod   # fill in the domain and secrets
docker compose --env-file deploy/.env.prod -f deploy/compose.yml -f deploy/compose.prod.yml up -d
docker compose --env-file deploy/.env.prod -f deploy/compose.yml -f deploy/compose.prod.yml --profile apps up -d --build
```

Idempotent — run the same commands again to deploy an update. See
[deploy/README.md](deploy/README.md) for the details, the port map and the
production prerequisites.

## Components

| Service | Port (dev) | Role |
|---|---|---|
| `Musify.Api` | 5111 | REST API, issues presigned upload URLs and stream tickets |
| `Musify.StreamingGateway` | 8081 | YARP proxy that validates the stream ticket and serves the audio |
| `Musify.Worker` | — | MassTransit consumers: transcoding, thumbnails, YouTube downloads |
| `web-player` | 5173 / 3000 | Web client (SvelteKit, server-side OIDC session) |
| Zitadel | 8080 | Identity (OIDC/OAuth2) |
| PostgreSQL / RabbitMQ / SeaweedFS / Jaeger | 59000 / 5672 / 8333 / 16686 | Metadata, messaging, object storage, traces |

More in [docs/](docs/README.md): [architecture](docs/architecture.md),
[authentication](docs/authentication.md), [storage](docs/storage.md),
[streaming](docs/streaming.md), [media processing](docs/media-processing.md),
[development](docs/development.md).
