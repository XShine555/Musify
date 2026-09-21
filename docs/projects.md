# Proyectos

Los proyectos .NET viven en `backend/` (solución `Musify.slnx`, carpetas
`Core/`, `Hosts/` y `Tests/`); el cliente web en `web-player/`.

## Musify.Api  (`:5111`)

API REST pública (Minimal APIs, .NET 10). Es la fachada fina del sistema.

- **Endpoints**: `UserEndpoints`, `TrackEndpoints`, `PlayListEndpoints`.
- **Autenticación**: JWT Bearer contra Zitadel (`Authentication/`), con `JwtBearerEventsHandler` que sincroniza el usuario en cada token validado.
- **`CurrentUser`**: abstracción que se enlaza como parámetro de endpoint (`IBindableFromHttpContext`) y expone los claims básicos (`Id` como `long?`, `RequiredId`, nombre, etc.) sin tener que leer `ClaimsPrincipal` a mano.
- **Emite**: presigned URLs de subida (vía `IStorageService`) y **stream-tickets** (vía `IStreamTicketService`, RS256).
- **OpenAPI + Scalar** para documentación/prueba; OAuth2 (PKCE) configurado para login desde Scalar.
- **CORS** totalmente abierto **solo en Development** (para el front local).
- Traduce `ErrorOr` → HTTP en `ResultHttpExtensions`.

**Propósito/porqué**: separar la cara pública (autorización + metadatos) del procesado y del servido de bytes.

## Musify.Domain + Musify.Application + Musify.Infrastructure

El núcleo, en tres proyectos (clean architecture):

### Domain
Entidades (`Entities/`) y value objects/enums (`ValueObjects/`: `ProcessingStatus`, `LifeCycleStatus`, `UploadIntentStatus`, …). Sin dependencias de infraestructura.

### Application
Casos de uso (CQRS con Mediator):
- `Tracks/`, `PlayLists/`, `Users/` → `Commands`, `Queries`, `Handlers`, `Responses`.
- `Contracts/` → interfaces que implementa la infraestructura (`IDatabase`, `IStorageService`, `IAudioTranscoderService`, `IPictureService`, `IEventBus`, `IStreamTicketService`).
- `Configuration/` → opciones tipadas (Track, PlayList, ApplicationStorage, UploadIntent…) bindeadas y validadas al arranque.
- `Shared/` → helpers transversales (`StorageKey.Combine`, `ImageSize`).
- `Pagination/` → `PaginatedResponse<T>`.

**Porqué**: la lógica de negocio no conoce EF, S3 ni RabbitMQ; solo interfaces. Reutilizable por la API y el Worker.

### Infrastructure
Implementaciones concretas:
- `Persistence/` → `Database` (EF Core/Npgsql), migraciones.
- `Services/` → `StorageService` (AWSSDK.S3 contra SeaweedFS), `AudioTranscoderService` (ffmpeg → `.m4a`), `PictureService` (ImageSharp), `StreamTicketService` (firma RS256).
- `MassTransit/` → consumers, activities y routing slips (ver [media-processing.md](media-processing.md)).
- `Jobs/` → `UploadIntentExpirationJob`, `TemporalUploadsCleanUpJob`.

## Musify.StreamingGateway  (`:8081`)

Reverse proxy (ASP.NET + **YARP**) que sirve el audio (`.m4a`) desde SeaweedFS. Valida el stream-ticket (RS256, clave pública) y, si el objeto pedido cae bajo el prefijo autorizado, reenvía al filer.

**Porqué un proyecto aparte**: escala con el tráfico de bytes independientemente de la API, tiene superficie mínima (solo sabe validar un JWT y proxiar) y permite que SeaweedFS quede en red privada. Ver [streaming.md](streaming.md).

## Musify.Worker

Worker Service (host de fondo) que consume los eventos de MassTransit y ejecuta el procesado pesado: transcode de audio a `.m4a` (ffmpeg), generación de miniaturas (ImageSharp), transferencias de ficheros en el bucket, y los jobs de expiración/limpieza de upload intents.

**Porqué aparte**: el trabajo CPU/IO no debe ocurrir dentro de un request HTTP; se escala y reinicia por separado. (Antes se llamaba `Musify.PictureWorker`; se renombró porque hace más que imágenes.)

## web-player  (`:5173` en dev, `:3000` en contenedor)

Cliente web (SvelteKit 2 + Svelte 5 + Tailwind 4). La sesión OIDC vive en el
servidor (cookie cifrada); las llamadas al backend salen del servidor con el
access token, y las subidas se hacen contra las URLs prefirmadas de S3.

## Tests

`backend/Tests/`, carpeta `/Tests/` en el `.slnx`: cinco proyectos xUnit —
`Musify.Domain.Tests`, `Musify.Application.Tests` (SQLite en memoria, sin
Docker), `Musify.Infrastructure.Tests` y `Musify.Api.Tests` (Testcontainers:
Postgres + SeaweedFS reales) y `Musify.StreamingGateway.Tests`. Detalle en
[development.md](development.md#tests).

## Otras carpetas

- **`deploy/`** — stacks de Docker Compose (dev y prod), autocontenidos. Ver
  [../deploy/README.md](../deploy/README.md).
- **`docs/`** — esta documentación.
