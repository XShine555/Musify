# Arquitectura

## Principios

- **Clean architecture / capas** en `backend/`: el dominio y la aplicación no dependen de infraestructura; la infraestructura implementa las interfaces (`Application/Contracts`).
- **CQRS**: cada operación es un `Command` o `Query` con su handler (Mediator). Los handlers devuelven `Ardalis.Result` (Ok/NotFound/Unauthorized/Conflict/Invalid…), que la API traduce a códigos HTTP.
- **Los bytes pesados no pasan por la API**: subidas con URLs prefirmadas (cliente → S3) y reproducción vía un reverse proxy dedicado (gateway → S3). La API solo mueve metadatos y emite permisos.
- **Procesado asíncrono**: crear una pista no transcodifica en caliente; publica un evento y un worker hace el trabajo pesado con compensación (sagas/routing slips de MassTransit).

## Componentes

| Componente | Responsabilidad | Por qué separado |
|---|---|---|
| **Musify.Api** | API REST (usuarios, pistas, playlists), autoriza, emite presigned URLs y stream-tickets | Punto de entrada fino; no procesa medios ni sirve bytes |
| **Domain/Application/Infrastructure** | Dominio + casos de uso + infraestructura compartida (EF, S3, MassTransit, ffmpeg) | Lógica reutilizada por la API y el Worker |
| **StreamingGateway** | Reverse proxy (YARP) que valida el ticket y sirve el audio (`.m4a`) desde SeaweedFS | Escala con el tráfico de bytes, independiente de la API; SeaweedFS queda privado detrás |
| **Worker** | Consume eventos de MassTransit: transcodifica audio a `.m4a` y genera miniaturas | Trabajo CPU/IO intensivo fuera del request HTTP |
| **PostgreSQL** | Metadatos (usuarios, pistas, playlists, upload intents) | — |
| **RabbitMQ** | Cola de eventos + outbox transaccional | Desacopla creación de procesado |
| **SeaweedFS** | Almacenamiento de objetos (S3 + filer) | Guarda originales y derivados (audio `.m4a`, miniaturas) |
| **Zitadel** | Identidad (OIDC/OAuth2) | Login y emisión de JWT |

## Flujos principales

### 1. Subida de una pista
1. Cliente pide `POST /tracks/upload-urls` → la API crea **UploadIntents** (reservas) y devuelve **URLs prefirmadas PUT** (imagen + audio).
2. Cliente sube los ficheros **directo a SeaweedFS** con esas URLs (no pasan por la API).
3. Cliente llama `POST /tracks` con los `intentId`s → la API valida los intents, crea el `Track` (+ `UserHasTrack`) y publica `CreateTrackResourcesEvent`.
4. El **Worker** consume el evento: mueve los originales a su sitio, genera miniaturas y transcodifica el audio a `.m4a`, y actualiza el estado del track.

Ver detalle en [media-processing.md](media-processing.md) y [storage.md](storage.md).

### 2. Reproducción (streaming)
1. Cliente pide `GET /tracks/{id}/stream` (autenticado) → la API autoriza y devuelve `{ manifestUrl, ticket }` (URL al `.m4a` + ticket RS256 acotado a la carpeta de esa pista).
2. El reproductor abre la URL del audio añadiendo `?t=<ticket>` (o cabecera `X-Stream-Ticket`) y descarga por HTTP range.
3. El **StreamingGateway** valida el ticket y reenvía al filer de SeaweedFS; los bytes van storage → cliente.

Ver [streaming.md](streaming.md).

### 3. Autenticación
1. Cliente hace login OIDC contra **Zitadel** y obtiene un access token (JWT).
2. La Musify.Api valida la firma/iss/aud del JWT.
3. En cada token validado, se **sincroniza** el usuario local (crea/actualiza nombre y foto) — provisioning just-in-time.

Ver [authentication.md](authentication.md).

## Modelo de datos (entidades)

- **User** — `Id` es el `sub` numérico de Zitadel (`long`), más nombre y `ProfilePictureUrl`.
- **Track** — pista: nombres de originales/derivados, `AudioFolderName` (carpeta del `.m4a` transcodificado), estados de procesado (`ProcessingStatus`), `LifeCycleStatus`.
- **PlayList** — lista del usuario (`UserId`).
- **PlayListHasTrack** — pistas dentro de una playlist (con `Position`).
- **UserHasTrack** — propiedad: qué usuario posee qué pista.
- **UploadIntent** — reserva de subida (clave, bucket, tamaño esperado, estado, expiración) para validar y aplicar cuota.
- **Upload** — registro de objetos subidos/transferidos al storage.
