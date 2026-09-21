# Musify documentation

Musify is a music streaming backend: upload tracks, process them (transcode
the audio to `.m4a`, generate thumbnails), organize them into playlists, and
stream them without the bytes ever passing through the API.

This folder explains **what each piece does, its purpose, and why it's built
this way**.

## Index

- [architecture.md](architecture.md): overview, components, how they fit together, and the main flows.
- [projects.md](projects.md): what each project/solution is and what it's for.
- [authentication.md](authentication.md): Zitadel (OIDC), JWT validation, user provisioning/sync.
- [storage.md](storage.md): SeaweedFS (S3), key layout, presigned URLs, what's public vs. private.
- [streaming.md](streaming.md): audio streaming (`.m4a` over HTTP range) with stream tickets (RS256) and the reverse proxy (StreamingGateway).
- [media-processing.md](media-processing.md): the worker and its MassTransit workflows (audio transcoding, thumbnails).
- [development.md](development.md): running everything locally, containers, ports, commands.
- [../deploy/README.md](../deploy/README.md): deployment in detail, compose, scripts, and production.

## Component map (high level)

```
                 ┌─────────────────────────────────────────────┐
   Client   ───► │ Musify.Api (:5111)  REST API + issues tickets│
                 └─────────────────────────────────────────────┘
                        │ EF Core          │ presigned (uploads)   │ MassTransit (events)
                        ▼                  ▼                       ▼
                 PostgreSQL          SeaweedFS S3 (:8333)     RabbitMQ ──► Worker (processes)
                 (metadata)          (objects)                            audio→.m4a, images
                                          ▲
   Client   ───► StreamingGateway (:8081) ┘   serves .m4a audio (validates ticket, proxies to the filer)

   Login: Client ◄──► Zitadel (:8080, OIDC)   →  Musify.Api validates the JWT
```

PostgreSQL, Zitadel, SeaweedFS and RabbitMQ are not run by this repository.
They're shared infrastructure, provided by the separate `Infrastructure`
repository, that this stack connects to the same way any other project
does.

## Stack

- **.NET 10**, C#. Minimal APIs (`Musify.Api`), Worker Service (`Musify.Worker`), ASP.NET + YARP (`Musify.StreamingGateway`).
- **CQRS** with [Mediator] (source generator) and results via **ErrorOr**.
- **EF Core 10** + **PostgreSQL** (metadata).
- **MassTransit 8** + **RabbitMQ** (async processing with routing slips/sagas).
- **SeaweedFS** (S3-compatible object storage, via AWSSDK.S3).
- **ffmpeg** (audio transcoding to AAC/`.m4a`), **SixLabors.ImageSharp** (thumbnails).
- **Zitadel** (OIDC/OAuth2) for identity.

## Repository layout

A single repo: `backend/` (the six .NET projects), `web-player/` (the
SvelteKit web client), `deploy/` (Docker Compose stacks and scripts), and
`docs/`. The shared infrastructure (Postgres, Zitadel, SeaweedFS, RabbitMQ,
Jaeger) lives in the separate `Infrastructure` repository, a sibling of this
one, and is not part of this layout.
