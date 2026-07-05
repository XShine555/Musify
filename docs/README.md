# Musify — Documentación

Musify es un backend de streaming de música: subir pistas, procesarlas (transcodificar el audio a `.m4a` y generar miniaturas), organizarlas en playlists y reproducirlas en streaming sin que los bytes pasen por la API.

Esta carpeta explica **qué hace cada pieza, su propósito y por qué está diseñada así**.

## Índice

- [architecture.md](architecture.md) — Visión global: componentes, cómo encajan y los flujos principales.
- [projects.md](projects.md) — Qué es y para qué sirve cada proyecto/solución.
- [authentication.md](authentication.md) — Zitadel (OIDC), validación de JWT, provisioning/sync de usuarios.
- [storage.md](storage.md) — SeaweedFS (S3), estructura de claves, URLs prefirmadas, qué es público y qué privado.
- [streaming.md](streaming.md) — Streaming del audio (`.m4a` por HTTP range) con stream-ticket (RS256) y el reverse proxy (StreamingGateway).
- [media-processing.md](media-processing.md) — El worker y los workflows de MassTransit (transcode de audio, miniaturas).
- [development.md](development.md) — Cómo levantar todo en local: contenedores, puertos y comandos.

## Mapa de componentes (alto nivel)

```
                 ┌─────────────────────────────────────────────┐
   Cliente  ───► │ WebApi (:5111)  API REST + emite tickets     │
                 └─────────────────────────────────────────────┘
                        │ EF Core          │ presigned (subidas)   │ MassTransit (eventos)
                        ▼                  ▼                       ▼
                 PostgreSQL          SeaweedFS S3 (:8333)     RabbitMQ ──► Worker (procesa)
                 (metadatos)         (objetos)                            audio→.m4a, imágenes
                                          ▲
   Cliente  ───► StreamingGateway (:8081) ┘   sirve audio .m4a (valida ticket, proxy al filer)

   Login: Cliente ◄──► Zitadel (:8080, OIDC)   →  WebApi valida el JWT
```

## Stack

- **.NET 10**, C#. Minimal APIs (WebApi), Worker Service (Worker), ASP.NET + YARP (StreamingGateway).
- **CQRS** con [Mediator] (source-generator) y resultados con **Ardalis.Result**.
- **EF Core 10** + **PostgreSQL** (metadatos).
- **MassTransit 8** + **RabbitMQ** (procesado asíncrono con routing slips/sagas).
- **SeaweedFS** (almacenamiento de objetos compatible con S3, vía AWSSDK.S3).
- **ffmpeg** (transcode de audio a AAC/`.m4a`), **SixLabors.ImageSharp** (miniaturas).
- **Zitadel** (OIDC/OAuth2) para identidad.

## Repositorios git

Cada proyecto de nivel superior es su propio repo git: `MusifyBackend`, `Musify.WebApi`, `Musify.StreamingGateway`, `Musify.Worker`. La raíz del workspace (`docs/`, `deploy/`, `player.html`) no está versionada.
