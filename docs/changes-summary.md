# MusifyBackend — Resumen de cambios (2026-05-14)

Este documento resume **todos los cambios aplicados hasta ahora** en la rama `master` durante esta iteración.

> Nota: El proyecto compila en `Release` al cierre de estos cambios.

---

## 1) Mensajería robusta: MassTransit + EF Outbox

**Objetivo**: hacer la publicación de eventos confiable y transaccional (DB + mensajes) y tolerar redelivery en consumidores.

### Cambios principales
- Se integró **MassTransit EntityFramework Outbox** con PostgreSQL (`UsePostgres` + `UseBusOutbox`).
- Se añadieron entidades/tablas de **Inbox/Outbox** en el `DbContext`.
- Se generó y añadió una **migración** para las tablas de outbox/inbox.
- Se ajustó la configuración de endpoints para aplicar:
  - **Retry** (exponential backoff)
  - **Idempotencia** en consumo usando `UseEntityFrameworkOutbox<Database>()`
- Se ajustó el orden de operaciones para el patrón outbox: **publicar evento y luego `SaveChanges`**.

### Archivos involucrados (principalmente)
- `Infrastructure/MassTransit/DependencyInjection/MassTransitDependencyInjection.cs`
- `Infrastructure/MassTransit/DependencyInjection/MassTransitDependencyInjection.Endpoints.cs`
- `Infrastructure/Persistence/Database.cs`
- `Infrastructure/Persistence/Migrations/20260514095113_AddMassTransitOutbox.cs`
- `Application/Abstractions/Infrastructure/IEventBus.cs`
- `Infrastructure/MassTransit/EventBus/MassTransitEventBus.cs`

---

## 2) Workflows: Courier RoutingSlip para imágenes y audio

**Objetivo**: mantener el pipeline asíncrono (descarga, resize/transcode, upload, update de entidades) con reintentos e idempotencia.

### Fix importante en workflow de imágenes
- Se corrigió el nombre de actividad del paso de **Upload del tamaño Medium** (antes duplicaba el nombre de `ResizeMedium`).

Archivos:
- `Infrastructure/MassTransit/RoutingSlip/Builders/PictureWorkflowRoutingSlipBuilder.cs`
- `Infrastructure/MassTransit/RoutingSlip/ActivityNames.cs`

### Fix importante en compensación de uploads
- `UploadFileToBucketActivity` ahora registra correctamente el **object key completo** (ruta + filename) para que la compensación borre el objeto correcto.

Archivo:
- `Infrastructure/MassTransit/Activities/Files/UploadFileToBucketActivity.cs`

---

## 3) Uploads (Opción B): Presigned PUT URL + Create/Update con nombres

**Objetivo**: separar la subida de binarios (S3) de la operación de dominio (Create/Update), evitando rollbacks complejos y haciendo el flujo más robusto.

### Endpoint/Commands añadidos (request presigned URL)
- Playlist picture:
  - `Application/PlayLists/Commands/RequestPlayListPictureUploadCommand.cs`
  - `Application/PlayLists/Handlers/RequestPlayListPictureUploadCommandHandler.cs`
  - `Application/PlayLists/Responses/PlayListPictureUploadResponse.cs`
- Track picture + audio:
  - `Application/Tracks/Commands/RequestTrackUploadUrlsCommand.cs`
  - `Application/Tracks/Handler/RequestTrackUploadUrlsCommandHandler.cs`
  - `Application/Tracks/Responses/TrackUploadUrlsResponse.cs`

### Storage: presigned PUT
- Se añadió soporte para generar **presigned PUT URLs**:
  - `Application/Abstractions/Infrastructure/IStorageService.cs`
  - `Infrastructure/Services/StorageService.cs` (`GetUploadUrlAsync`)

---

## 4) Limpieza: eliminar V1 y normalizar nombres (V2 → “normal”)

**Objetivo**: eliminar comandos/handlers “V1” (subida directa por stream/multipart) y dejar un único set de comandos “normales” con el contrato final.

### Resultado
- `CreatePlayListCommand`, `UpdatePlayListCommand` y `CreateTrackCommand` quedaron con el contrato nuevo (nombres/metadata, no streams).
- Se eliminaron archivos `*V2*` ya innecesarios.

Archivos clave:
- `Application/PlayLists/Commands/CreatePlayListCommand.cs`
- `Application/PlayLists/Handlers/CreatePlayListCommandHandler.cs`
- `Application/PlayLists/Commands/UpdatePlayListCommand.cs`
- `Application/PlayLists/Handlers/UpdatePlayListCommandHandler.cs`
- `Application/Tracks/Commands/CreateTrackCommand.cs`
- `Application/Tracks/Handler/CreateTrackCommandHandler.cs`

---

## 5) Prefijo de seguridad: `uploads/{userId}/...` para originales

**Objetivo**: reducir superficie (y colisiones) haciendo que los objetos originales queden naturalmente “namespaced” por usuario.

### Cambios
- Las **keys de originales** (playlist original picture, track original picture, track original audio) ahora se generan bajo:
  - `uploads/{userId}/...`
- Se añadieron overloads de rutas para construir keys con `userId`.
- Los handlers de request presigned URL ahora generan keys usando el prefijo con `userId`.
- Los eventos de workflows (sourceKey) se publican con la key prefijada.
- El borrado de playlist y track usan keys prefijadas dentro de sus workflows de delete.

Archivos:
- `Application/Configuration/PlayListConfiguration.cs`
- `Application/Configuration/TrackConfiguration.cs`
- `Application/PlayLists/Handlers/RequestPlayListPictureUploadCommandHandler.cs`
- `Application/Tracks/Handler/RequestTrackUploadUrlsCommandHandler.cs`
- `Application/PlayLists/Handlers/CreatePlayListCommandHandler.cs`
- `Application/PlayLists/Handlers/UpdatePlayListCommandHandler.cs`
- `Application/Tracks/Handler/CreateTrackCommandHandler.cs`
- `Application/PlayLists/Handlers/DeletePlayListCommandHandler.cs`


---

## 6) Normalización de separadores en keys S3 (evitar `\\` en Windows)

**Objetivo**: evitar que `Path.Combine` introduzca separadores `\\` en keys S3 (lo cual es incorrecto/inesperado en S3, y rompe interoperabilidad).

### Cambios
- Se reemplazaron combinaciones de keys que usaban `Path.Combine` por joins con `/` en puntos críticos.

Archivos:
- `Application/Configuration/PlayListConfiguration.cs`
- `Application/Configuration/TrackConfiguration.cs`
- `Infrastructure/MassTransit/Activities/Files/UploadFileToBucketActivity.cs`
- `Infrastructure/Services/StorageService.cs`

---

## 7) Validaciones en Application eliminadas (delegadas a API)

**Objetivo**: evitar duplicar validaciones de formato (extensiones/strings) si la capa API ya se encarga.

### Cambios
- Se eliminó el helper de validación creado inicialmente y los checks en handlers.

Archivos:
- Eliminado: `Application/Validation/UploadObjectNameValidator.cs`
- Ajustados:
  - `Application/PlayLists/Handlers/CreatePlayListCommandHandler.cs`
  - `Application/PlayLists/Handlers/UpdatePlayListCommandHandler.cs`
  - `Application/Tracks/Handler/CreateTrackCommandHandler.cs`

---

## 8) Documento de mitigaciones (presigned uploads)

Se añadió documentación de hardening/mitigaciones contra abuso/spam en endpoints presigned.

Archivo:
- `docs/presigned-upload-hardening.md`

---

## 9) Notas de compatibilidad / impacto

- Este cambio **rompe compatibilidad** con la API anterior si existían endpoints que aceptaban multipart/streams para Create/Update (ahora se espera el flujo presigned y luego Create/Update con nombres).
- Los objetos originales ahora viven bajo `uploads/{userId}/...`. Objetos antiguos fuera de ese prefijo pueden quedar huérfanos si ya existían de antes.

---

## 10) Estado final (iteración anterior)

- `dotnet build MusifyBackend.slnx -c Release` ✅

---

## 11) Deletes unificados con Workflows (RoutingSlip)

**Objetivo**: homogeneizar deletes de Track/PlayList usando Courier RoutingSlip con actividades granulares e idempotentes.

### Cambios
- Se añadió `LifeCycleStatus` (`Active`/`Removing`) a `Track` y `PlayList` y se generó migración.
- Track delete:
  - Se eliminó la activity monolítica y el slip ahora encadena: marcar como removing → borrar N ficheros (condicional) → borrar en DB.
- PlayList delete:
  - Se añadió `DeletePlayListEvent` + consumer + routing slip builder + activities para marcar como removing, borrar pictures (si no son preset) y borrar en DB.
- Se endureció la idempotencia de borrado en S3: si una key no existe, se loguea y el paso se considera completado.
- Se eliminaron `RemoveFileEvent`/`RemoveFileConsumer` por quedar sin usos.

Archivos clave:
- `Domain/Entities/LifeCycleStatus.cs`
- `Domain/Entities/Track.cs`
- `Domain/Entities/PlayList.cs`
- `Application/Events/DeletePlayListEvent.cs`
- `Application/PlayLists/Handlers/DeletePlayListCommandHandler.cs`
- `Application/Tracks/Handler/DeleteTrackCommandHandler.cs`
- `Infrastructure/MassTransit/Consumers/DeleteTrackConsumer.cs`
- `Infrastructure/MassTransit/Consumers/DeletePlayListConsumer.cs`
- `Infrastructure/MassTransit/RoutingSlip/Builders/DeleteTrackRoutingSlipBuilder.cs`
- `Infrastructure/MassTransit/RoutingSlip/Builders/DeletePlayListRoutingSlipBuilder.cs`
- `Infrastructure/MassTransit/Activities/Files/RemoveFileFromBucketActivity.cs`
- `Infrastructure/MassTransit/DependencyInjection/MassTransitDependencyInjection.Registration.cs`
- `Infrastructure/Persistence/Migrations/20260514161518_AddLifeCycleStatus.cs`

---

## 12) Hardening de pre-signed uploads (Fases 1 y 2)

**Objetivo**: evitar abuso de coste/almacenamiento cuando usuarios hacen spam de endpoints presigned sin crear la entidad final (objetos huérfanos en S3).

Referencia: `docs/presigned-upload-hardening.md`

---

### Fase 1 — Upload Intents en DB

#### Nuevas entidades (Domain)
- `Domain/Entities/UploadIntent.cs` — tabla `UploadIntents` con campos: `Id`, `UserId`, `Bucket`, `Key`, `ObjectName`, `ContentType`, `ExpectedSizeBytes`, `Purpose`, `Status`, `ExpiresAt`, `CreatedAt`.
- `Domain/Entities/UploadIntentStatus.cs` — enum: `Issued`, `Consumed`, `Expired`.
- `Domain/Entities/UploadIntentPurpose.cs` — enum: `PlayListPicture`, `TrackPicture`, `TrackAudio`.

#### Nueva configuración (Application)
- `Application/Configuration/UploadIntentConfiguration.cs` — sección `UploadIntent`:
  - `UploadUrlExpiresInSeconds` (default: 120)
  - `MaxActiveUploadIntentsPerUser` (default: 5)
  - `MaxActiveUploadBytesPerUser` (default: 200 MB)
  - `MaxUploadBytes` (default: 100 MB, para validación HEAD)
  - `DefaultExpectedPictureSizeBytes` / `DefaultExpectedAudioSizeBytes` (fallback cuando el caller no envía tamaño)
  - `ExpirationJobIntervalSeconds` / `ExpiredIntentsRetentionDays`
  - `TempRootPrefix` / `TempUploadsRetentionDays` / `TempCleanupJobIntervalSeconds` (Fase 2)

#### Interfaces actualizadas
- `Application/Abstractions/Infrastructure/IDatabase.cs` — añadido `DbSet<UploadIntent> UploadIntents`.
- `Application/Abstractions/Infrastructure/IStorageService.cs` — añadidos:
  - `HeadObjectAsync(bucket, key)` → `ObjectMetadata?` (para validación antes de consumir).
  - `ListObjectsAsync(bucket, prefix)` → `IAsyncEnumerable<(string Key, DateTime LastModifiedUtc)>` (para el cleanup job).
- `Application/Abstractions/Infrastructure/ObjectMetadata.cs` — record `(ContentType, ContentLength)`.

#### Infrastructure actualizada
- `Infrastructure/Persistence/Database.cs` — añadido `DbSet<UploadIntent>`.
- `Infrastructure/Services/StorageService.cs` — implementaciones de `HeadObjectAsync` y `ListObjectsAsync`.

#### Commands actualizados (cambio de contrato)
- `RequestPlayListPictureUploadCommand` — nuevo campo opcional `ExpectedSizeBytes?`.
- `RequestTrackUploadUrlsCommand` — nuevos campos opcionales `ExpectedPictureSizeBytes?`, `ExpectedAudioSizeBytes?`.
- `CreatePlayListCommand` — `OriginalPictureName?` → `PictureIntentId?` (Guid).
- `CreateTrackCommand` — `OriginalPictureName` + `OriginalAudioName` → `PictureIntentId` + `AudioIntentId` (Guid).
- `UpdatePlayListCommand` — `NewOriginalPictureName?` → `NewPictureIntentId?` (Guid).

#### Responses actualizadas
- `PlayListPictureUploadResponse` — añadido `IntentId` (Guid).
- `TrackUploadUrlsResponse` — añadidos `PictureIntentId` + `AudioIntentId` (Guid).

#### Handlers actualizados

**Request upload** (`RequestPlayListPictureUploadCommandHandler`, `RequestTrackUploadUrlsCommandHandler`):
- Comprueban cuota de usuario (conteo de intents activos + suma de bytes) antes de generar la URL.
- Crean un registro `UploadIntent` (Status = `Issued`) en la misma llamada.
- TTL reducido a `UploadUrlExpiresInSeconds` (configurable, default 120 s vs. los anteriores 600 s hardcodeados).

**Create/Update** (`CreatePlayListCommandHandler`, `CreateTrackCommandHandler`, `UpdatePlayListCommandHandler`):
- Reciben `IntentId` en lugar del nombre de fichero.
- Validan el intent: existe, pertenece al usuario, Status = `Issued`, no caducado.
- Realizan `HEAD` del objeto en S3: debe existir y tamaño ≤ `MaxUploadBytes`.
- Marcan el intent como `Consumed` y guardan todo en el mismo `SaveChangesAsync`.

#### Job de expiración
- `Infrastructure/Jobs/UploadIntentExpirationJob.cs` — `BackgroundService` que:
  - Marca como `Expired` los intents `Issued` con `ExpiresAt < now`.
  - Elimina intents `Expired` cuya fecha de expiración supera la ventana de retención configurable.

#### Migración
- `Infrastructure/Persistence/Migrations/*_AddUploadIntentTable.cs`

---

### Fase 2 — Prefijo temporal + cleanup de S3

#### Rutas temporales
- `TrackRoutes.BuildTempPicturePath` / `BuildTempAudioPath` — construyen `{TempRootPrefix}/{userId}/{ParentFolder}/{objectName}`.
- `PlayListRoutes.BuildTempPicturePath` — ídem para playlists.

#### Handlers de request upload
- La presigned URL ahora apunta a `temp/{userId}/...` (prefijo temporal) en lugar de la ruta final.
- El intent almacena la temp key.

#### Handlers de create/update (consume)
- Al consumir un intent, se llama `CopyFileAsync(tempKey → finalKey)` antes de publicar el evento de procesado.
- El objeto temporal queda en S3 y es eliminado posteriormente por el cleanup job.

#### Job de cleanup de temporales
- `Infrastructure/Jobs/TempUploadsCleanupJob.cs` — `BackgroundService` que:
  - Lista objetos bajo `TempRootPrefix/` usando `ListObjectsAsync`.
  - Borra los que superan `TempUploadsRetentionDays` (default 3 días).
  - Tolerante a errores por objeto (continúa si un borrado falla).

#### DI
- `Infrastructure/Services/ServicesDependencyInjection.AddUploadIntentJobs()` — registra `UploadIntentConfiguration` + ambos background jobs.

---

## 13) Refactor: IntentValidationResult (eliminar tuplas)

**Objetivo**: evitar retornar tuplas `(Result?, UploadIntent?)` desde métodos privados — patrón no idiomático en C#.

### Cambios
- Creado `Application/UploadIntents/IntentValidationResult.cs` — `internal sealed record` con:
  - `Result? Error` / `UploadIntent? Intent`
  - `bool IsSuccess`
  - Factory methods `Ok(intent)` / `Fail(error)`
- Los tres handlers (`CreatePlayListCommandHandler`, `CreateTrackCommandHandler`, `UpdatePlayListCommandHandler`) ahora usan `IntentValidationResult` en `ValidateAndLoadIntentAsync`.
- El tipo es `internal` (no `private`) porque es compartido entre 3 handlers de distintas carpetas.

---

## 14) Fix: race condition en cuota de intents

**Objetivo**: garantizar que el check de cuota + insert del intent sean atómicos y no admitan concurrent bypass.

### Cambios
- Añadida `IDatabaseTransaction` en `Application/Abstractions/Infrastructure/IDatabaseTransaction.cs` — interfaz limpia (`CommitAsync`, `DisposeAsync`) que no expone tipos EF.
- Añadido `BeginTransactionAsync(IsolationLevel, CancellationToken)` a `IDatabase`.
- Implementado en `Infrastructure/Persistence/Database.cs` con un `private sealed class DatabaseTransaction` que envuelve `IDbContextTransaction` de EF.
- `RequestPlayListPictureUploadCommandHandler` y `RequestTrackUploadUrlsCommandHandler`: el bloque quota check + insert ahora se ejecuta dentro de una transacción `SERIALIZABLE`, eliminando la ventana de race condition entre el check y el commit.

---

## 15) Fix: separación AddUploadIntentConfiguration / AddUploadIntentJobs

**Objetivo**: `UploadIntentConfiguration` es necesaria tanto en el host del API (para los handlers) como en el worker (para los jobs). El método anterior mezclaba config + jobs en uno solo, haciendo imposible registrar la config sin registrar los `BackgroundService`.

### Cambios
- `AddUploadIntentConfiguration(services, configuration)` — registra solo la config. Lo llama el host del API **y** el worker.
- `AddUploadIntentJobs(services, configuration)` — llama internamente a `AddUploadIntentConfiguration` y además registra `UploadIntentExpirationJob` + `TempUploadsCleanupJob`. Solo lo llama el worker.

Archivo: `Infrastructure/Services/ServicesDependencyInjection.cs`

---

## 16) Estado final

- `dotnet build` ✅ — 0 errores, 0 advertencias.
