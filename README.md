# Musify

Music streaming platform: upload your own tracks (or import them from YouTube),
process them asynchronously (audio transcoded to `.m4a`, thumbnails generated)
and stream them without the bytes ever going through the API.

```
backend/      .NET 10 — Domain / Application / Infrastructure / Api / Worker / StreamingGateway
web-player/   SvelteKit 2 + Svelte 5 + Tailwind 4 web client
deploy/       Docker Compose stacks and the scripts that bootstrap them
docs/         What each piece does and why (in Spanish)
```

## Run it

Development (Windows, Docker Desktop):

```powershell
./deploy/up.ps1        # infrastructure; the apps run from the IDE
./deploy/up.ps1 -Apps  # everything in Docker
```

Production, on a server with a domain:

```sh
./deploy/up.sh         # fill in deploy/.env.prod and the TLS certs when it asks
```

Both are idempotent and print what they did. See
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
