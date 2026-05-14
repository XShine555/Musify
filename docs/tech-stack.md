# Tech stack y arquitectura (MusifyBackend)

Este documento describe **qué tecnologías usa este repo** y **cómo encajan** (capas, workers, mensajería, storage, observabilidad). Está pensado para alguien que llega de cero o para una IA que necesita contexto.

## Foto rápida
- Runtime: **.NET 10** (C#)
- Estilo: **Clean Architecture** (capas `Domain` / `Application` / `Infrastructure`)
- Casos de uso: **CQRS** con **DispatchR** (similar a MediatR)
- Persistencia: **Entity Framework Core** + **PostgreSQL** (provider `Npgsql`)
- Workers / mensajería: **MassTransit** + **RabbitMQ**
- Orquestación de workflows: **MassTransit Courier / RoutingSlip** (Activities)
- Storage de objetos: **SeaweedFS** expuesto como **S3-compatible** (vía AWS SDK `IAmazonS3`)
- Observabilidad: **OpenTelemetry** (traces + metrics, export OTLP)
- Procesado multimedia:
  - Imágenes: **SixLabors.ImageSharp** (resize, WebP)
  - Audio: **ffmpeg** (transcode a DASH)

## Estructura del repo (capas)

### `Domain/` (núcleo de negocio)
- Entidades puras (sin dependencias de infraestructura).
- Ejemplos: `Track`, `PlayList`, `Upload`, etc.
- Estados de negocio relevantes:
  - `ProcessingStatus` (procesado en curso/finalizado, etc.).
  - `LifeCycleStatus` (p.ej. `Active`, `Removing`) para deletes asíncronos.

### `Application/` (casos de uso + contratos)
- Contiene comandos/queries/handlers por feature.
- CQRS:
  - `Commands/` y `Queries/` por feature (`PlayLists`, `Tracks`, `Users`, ...).
  - `Handlers/` ejecutan la lógica de aplicación (coordinar DB + eventos), pero **no implementan** detalles de infraestructura.
- Contratos con infraestructura: `Application/Abstractions/Infrastructure/*`
  - `IDatabase` (abstracto de EF DbContext)
  - `IEventBus` (publicación de eventos)
  - `IStorageService`, `IPictureService`, `IAudioTranscoderService`, etc.
- Eventos de integración: `Application/Events/*`
  - Se publican desde `Application` y se consumen en `Infrastructure`.

### `Infrastructure/` (implementaciones + integraciones)
- Implementa las abstracciones de `Application`.
- Contiene:
  - EF Core DbContext y migraciones
  - MassTransit (consumers, activities, routing slips, outbox)
  - Integración S3 (SeaweedFS) mediante AWS SDK
  - Observabilidad OpenTelemetry
  - Servicios de procesado (ImageSharp/ffmpeg)

## CQRS con DispatchR
- Los “use cases” se modelan como:
  - **Commands**: mutaciones (crear/actualizar/borrar).
  - **Queries**: lecturas.
- Los handlers viven en `Application/*/Handlers/` y dependen de:
  - `IDatabase` para persistencia (sin acoplarse al DbContext concreto).
  - `IEventBus` para emitir eventos cuando hay trabajo asíncrono.

## Persistencia (EF Core + PostgreSQL)
- DbContext: `Infrastructure/Persistence/Database.cs`
  - Usa `UseNpgsql(connectionString)`.
  - Incluye tablas de MassTransit Outbox/Inbox (ver sección de mensajería).
- DI: `Infrastructure/Persistence/DatabaseDependencyInjection.cs`
- Config: `Infrastructure/Configuration/DatabaseConfiguration.cs` (sección `Database`)
  - `ConnectionString`

## Mensajería y workers (MassTransit + RabbitMQ)

### Qué se usa
- Broker: RabbitMQ (`MassTransit.RabbitMQ`).
- MassTransit está configurado en dos modos:
  1) **Cliente** (publica eventos): `Infrastructure/MassTransit/DependencyInjection/MassTransitDependencyInjection.cs` → `AddMassTransitClient()`
  2) **Workers** (consumen y ejecutan activities): `AddMassTransitConsumers()`

### Outbox / Inbox (robustez)
- Se usa **EntityFramework Outbox** (`AddEntityFrameworkOutbox<Database>()`) con:
  - `UsePostgres()`
  - `UseBusOutbox()`
  - `QueryDelay = 1s`
  - `DuplicateDetectionWindow = 30m`
- En el DbContext se registran entidades de MassTransit:
  - `AddInboxStateEntity()`
  - `AddOutboxMessageEntity()`
  - `AddOutboxStateEntity()`

Idea clave:
- La outbox asegura que, tras confirmar la transacción de DB, los mensajes/eventos pendientes se publican de forma fiable.

### Courier / RoutingSlip (workflows asíncronos)
Para operaciones multi-paso (p.ej. imagen/audio y deletes), se usa **MassTransit Courier**:
- Un “RoutingSlip” es una lista de pasos.
- Cada paso es una **Activity** (con args + log opcional).
- Existen dos tipos que aparecen en este repo:
  - `ExecuteActivity` (endpoint `<name>_execute`), típicamente acciones atómicas.
  - `Activity` (con compensación/log), típico para pasos de workflow.

Dónde mirar:
- Builders: `Infrastructure/MassTransit/RoutingSlip/Builders/*`
- Activities: `Infrastructure/MassTransit/Activities/*`
- Consumers (punto de entrada del workflow): `Infrastructure/MassTransit/Consumers/*`
- Registro de endpoints: `Infrastructure/MassTransit/DependencyInjection/*`

Ejemplos de workflows presentes:
- Procesado de imagen (resize + upload + update DB)
- Procesado de audio (transcode DASH + upload + update DB)
- Deletes asíncronos (marcar “removing”, borrar objetos, borrar filas)

### Config de MassTransit
- `Infrastructure/Configuration/MassTransitConfiguration.cs` (sección `MassTransit`)
  - `Address`
  - `Username`
  - `Password`

## Storage de objetos (SeaweedFS como S3-compatible)

### Qué se usa
- SDK: `Amazon.S3` (`IAmazonS3`).
- Implementación: `Infrastructure/Services/StorageService.cs` (usa `IAmazonS3`).
- DI: `Infrastructure/Services/ServicesDependencyInjection.cs` → `AddStorageService()`

### Config
- `Infrastructure/Configuration/InfrastructureStorageConfiguration.cs` (sección `InfrastructureStorage`)
  - `Address` (ServiceURL)
  - `AccessKey` / `SecretAccessKey`
  - `ForcePathStyle` (útil en S3-compatibles)
  - `UseHttp`

Notas prácticas (S3-compatible):
- Algunas features avanzadas de AWS S3 pueden no comportarse igual (depende de SeaweedFS). Diseñar workflows para que sean correctos con el subconjunto básico: PUT/GET/HEAD/LIST/DELETE y paths bien definidos.

## Procesado multimedia

### Imágenes
- Librería: `SixLabors.ImageSharp`
- Servicio: `Infrastructure/Services/PictureService.cs`
- Contrato: `Application/Abstractions/Infrastructure/IPictureService.cs`
- Patrón: workers descargan/transforman/suben; la app coordina por eventos.

### Audio
- Herramienta: `ffmpeg` (binario externo)
- Config: `Infrastructure/Configuration/AudioTranscoderConfiguration.cs` (sección `AudioTranscoder`)
  - Timeout, codec/bitrate/channels/sample-rate, segmentación DASH, etc.
- Servicio: `Infrastructure/Services/AudioTranscoderService.cs`
- Contrato: `Application/Abstractions/Infrastructure/IAudioTranscoderService.cs`

### Directorio temporal (workers)
- Config: `Infrastructure/Configuration/WorkerConfiguration.cs` (sección `Workers`)
  - `Routes.TemporaryFilesDirectory`

## Observabilidad (OpenTelemetry)
- DI: `Infrastructure/Observability/OpenTelemetryDependencyInjection.cs`
- Config: `Infrastructure/Configuration/OpenTelemetryConfiguration.cs` (sección `OpenTelemetry`)
  - `OtlpEndpoint`

Qué instrumenta:
- Tracing:
  - `MassTransit`
  - ASP.NET Core
  - HttpClient
  - EF Core
  - Exporter OTLP
- Métricas:
  - `MassTransit`
  - ASP.NET Core
  - HttpClient
  - Runtime + Process
  - Exporter OTLP

## Identidad (Keycloak)
- El repo asume integración con **Keycloak** (ver `AGENTS.md` y nombres como `GetUserByKeycloakQuery`).
- La capa `Application` trata el “Keycloak Id” como identificador de usuario en algunos casos de uso.
- La integración concreta (tokens, validación, etc.) puede vivir fuera de estas 3 capas o en otro proyecto/host.

## Glosario rápido
- **Command / Query**: operación de escritura / lectura.
- **Handler**: clase que ejecuta el caso de uso (CQRS).
- **EventBus**: abstracción para publicar eventos (MassTransit por debajo).
- **Consumer**: “listener” de mensajes en una cola.
- **RoutingSlip**: workflow compuesto por Activities.
- **Activity**: paso del workflow (puede tener log/compensación).
- **Outbox**: patrón para publicar eventos de forma fiable tras commit de DB.

## “Dónde empiezo a leer” (ruta recomendada)
1) Arquitectura general: `AGENTS.md`
2) Casos de uso: `Application/*/Commands`, `Application/*/Queries` y sus `Handlers/`
3) Persistencia: `Infrastructure/Persistence/Database.cs` + migraciones
4) Mensajería/workflows: `Infrastructure/MassTransit/*`
5) Integraciones: `Infrastructure/Services/*` (S3, imagen, audio)
6) Observabilidad: `Infrastructure/Observability/OpenTelemetryDependencyInjection.cs`
