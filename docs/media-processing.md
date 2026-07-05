# Procesado de medios (Worker + MassTransit)

El procesado pesado (transcode de audio, miniaturas, transferencias en el bucket) ocurre **fuera del request HTTP**, en el **Worker**, orquestado con **MassTransit** sobre RabbitMQ.

## Por qué asíncrono y con routing slips

- Transcodificar audio y redimensionar imágenes es CPU/IO intensivo: no debe bloquear una llamada a la API.
- Es un proceso **multi-paso con efectos secundarios** (descargar de S3 → transcodificar → subir resultados → actualizar DB). Si un paso falla, hay que **deshacer** los anteriores. Para eso se usan **routing slips** (patrón Saga de MassTransit): cada paso es una *activity* con su **compensación**.
- **Outbox transaccional** (EF + MassTransit): el evento se publica de forma consistente con el cambio en la DB (no se pierde ni se duplica).

## Disparo

Al crear/actualizar una pista o playlist, el handler publica un evento (p. ej. `CreateTrackResourcesEvent`). Un **consumer** (`CreateTrackConsumer`, `UpdateTrackPictureConsumer`, …) construye un **routing slip** con la secuencia de activities y lo lanza.

## Builders de routing slip

- `CreateTrackRoutingSlipBuilder` — alta de pista (consume intents, mueve ficheros, dispara procesado).
- `PictureWorkflowRoutingSlipBuilder` — pipeline de imágenes (descargar → redimensionar → subir → actualizar).
- `AudioWorkflowRoutingSlipBuilder` — pipeline de audio (descargar → transcodificar a `.m4a` → subir carpeta → actualizar).
- `DeleteTrackRoutingSlipBuilder`, `DeletePlayListRoutingSlipBuilder`, `PlayListPictureSourceRoutingSlipBuilder`.

## Activities (pasos)

Agrupadas por área:
- **Files**: `DownloadFileFromBucketActivity`, `UploadFileToBucketActivity`, `TransferFilesToBucketActivity`, `CopyFileInBucketActivity`, `RemoveFileFromBucketActivity`.
- **Audio**: `GenerateAudioWorkflowPathsActivity`, `TranscodeAudioActivity` (ffmpeg → `.m4a`), `UpdateTrackAudioActivity` (guarda `AudioFolderName` y marca `Completed`).
- **Pictures**: `GeneratePictureWorkflowPathsActivity`, `ResizePictureActivity` (ImageSharp), `UpdateTrackPictureActivity`, `UpdatePlayListPictureActivity`.
- **Tracks/PlayLists**: `MarkTrackAsRemovingActivity`, `DeleteTrackFromDbActivity`, `PublishTrackProcessingEventsActivity`, y equivalentes de playlist.
- **UploadIntents**: `ConsumeUploadIntentsActivity`.

`RoutingSlipCleanUpConsumer` reacciona a `Completed`/`Faulted` para limpiar (p. ej. borrar el directorio temporal de trabajo).

## Transcode de audio

`AudioTranscoderService` invoca **ffmpeg** leyendo el original por stdin y produciendo en un directorio de trabajo un único `audio.m4a`: AAC con codec/bitrate/sample-rate/profile configurables y `-movflags +faststart` (moov al inicio → seek por range). Luego `TransferFilesToBucketActivity` sube esa carpeta a `Tracks/ProcessedAudios/{folder}/` y `UpdateTrackAudioActivity` guarda el `AudioFolderName` (la carpeta **de destino en S3**, no la local) y pone el estado en `Completed`.

## Configuración que necesita el Worker

El Worker registra: storage (S3), transcoder (ffmpeg), pictures, DB, los consumers de MassTransit y los jobs de upload intents. Su `AppSettings` debe tener las secciones correctas: `Bucket` (= `webapi-storage`, el mismo que la API), `PlayList`, `Track`, `MassTransit`, `AudioTranscoder`, `UploadIntent`, `Workers` (directorio temporal), `InfrastructureStorage`, `Database`.

## Jobs de fondo

- `UploadIntentExpirationJob` — marca intents caducados como expirados (libera cuota) cada `ExpirationJobIntervalSeconds`.
- `TemporalUploadsCleanUpJob` — limpia subidas temporales antiguas.

Corren en el host del Worker (`AddUploadIntentJobs`). Si no se registran en ningún proceso, los intents colgados no se limpian solos.
