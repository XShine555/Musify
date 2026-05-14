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

## 10) Estado final

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
