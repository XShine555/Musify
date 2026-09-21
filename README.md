# Musify

A music streaming app. Upload your own tracks, organize them into playlists
and albums, and stream them back with a web player built around your
listening habits: auto-generated mixes, a home feed, search, likes, and an
Explore page for finding something new.

## What it does

- **Upload and organize your own music**: tracks, albums, cover art, artist credits.
- **Playlists**, likes, and a "recently played" history.
- **Mixes**, playlists generated automatically from what you actually listen to.
- **Explore**, genre browsing and search across tracks, albums, playlists and people.
- **Real streaming**, not just file downloads: seek, queue, shuffle, repeat.
- **Accounts via OIDC login**, with an optional anonymous listening mode.
- Light and dark themes, and a layout that works on both desktop and mobile.

## How it's built

**Backend**: .NET 10, split into a REST API, a background worker, and a
dedicated streaming gateway.
- Clean architecture (Domain / Application / Infrastructure) with CQRS (Mediator) and `ErrorOr` results.
- PostgreSQL via EF Core for metadata. RabbitMQ and MassTransit handle async processing (audio transcoding, thumbnails) with saga-based rollback.
- SeaweedFS (S3-compatible) for file storage. Uploads and downloads go straight between the client and storage, never through the API.
- ffmpeg for audio transcoding, ImageSharp for thumbnails.
- A YARP reverse proxy that streams the audio and checks a signed, short-lived ticket on every request.
- Zitadel for login (OIDC/OAuth2).

**Web client**: SvelteKit 2 on Svelte 5 (runes), TypeScript, Tailwind 4.
The session lives on the server, so the backend access token never reaches the browser.

**Infrastructure**: Musify's own apps run from Docker Compose (dev and
production overlays). Postgres, RabbitMQ, SeaweedFS, Zitadel and Jaeger are
shared infrastructure that lives in a separate `Infrastructure` git
repository, used by Musify and by other projects that need the same OIDC
provider and object storage.

## Project layout

```
backend/      .NET solution: Api, Worker, StreamingGateway, and the shared Domain/Application/Infrastructure
web-player/   the SvelteKit web client
deploy/       Docker Compose stacks for dev and production
docs/         architecture notes, one file per area
```

Postgres, Zitadel, SeaweedFS, RabbitMQ and Jaeger are not part of this
layout. They live in the separate `Infrastructure` repository instead.

## Digging deeper

[docs/](docs/README.md) covers how each piece works and why: architecture,
authentication, storage, streaming, media processing, and the local dev
setup. [deploy/README.md](deploy/README.md) has the full instructions for
running the stack, in development or on a production server.
