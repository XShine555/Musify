# Plan — Unificar Deletes con Workflows (RoutingSlip)

## Contexto

Actualmente hay dos problemas de diseño en los deletes:

**PlayLists:** el borrado disparaba múltiples `RemoveFileEvent` sueltos desde `DeletePlayListCommandHandler` (4 llamadas separadas para original/small/medium/large) y luego eliminaba la entidad en DB. Sin workflow, sin trazabilidad.

**Tracks:** existía un workflow (`DeleteTrackEvent` → consumer → RoutingSlip → `DeleteTrackActivity`), pero `DeleteTrackActivity` hacía **demasiado en un único paso**: carga la entidad, borra todos los archivos de S3 (pictures + audios), y elimina de DB. Esto rompe el principio de actividades granulares y reutilizables, hace que el slip no sea "seguible" paso a paso, y dificulta los reintentos parciales.

La idea es **homogeneizar y corregir ambos**: que los deletes de Track y PlayList sigan un RoutingSlip con actividades pequeñas, ordenadas, repetibles e independientes.

---

## Objetivos

- **Un único camino** de borrado por aggregate (Track / PlayList), basado en **RoutingSlip con actividades granulares**.
- El handler de comando hace lo mínimo: validar ownership/reglas, cambiar estado a `Removing`, y publicar el evento de borrado (outbox).
- El workflow ejecuta pasos **ordenados, reutilizables y reiniciables** individualmente.
- **Reutilizar** `RemoveFileFromBucketActivity` (ya existente) para cada archivo a borrar, en lugar de lógica ad-hoc dentro de una activity monolítica.
- Eliminar `RemoveFileEvent`, `RemoveFileConsumer` y su registration si no tienen otros usos.

No objetivos (por ahora):
- No introducir observabilidad extra (tracing/telemetría) fuera de lo ya existente.

---

## Problemas del enfoque actual

### PlayLists
- El handler implementa orquestación (decide qué archivos borrar y cómo) → acoplamiento alto.
- Publica múltiples `RemoveFileEvent` sueltos, sin agruparlos en un proceso único.
- No hay log de RoutingSlip que permita seguir el progreso/estado.

### Tracks ← **nuevo problema identificado**
- `DeleteTrackActivity` es monolítica: carga entidad, borra pictures, borra audios, elimina de DB, todo en un solo `Execute`.
- No reutiliza `RemoveFileFromBucketActivity` ya existente.
- Si falla a mitad, no hay granularidad de reintento (no se sabe qué paso falló).
- El RoutingSlip tiene un único nodo → no es realmente un "slip" con pasos.

---

## Diseño propuesto

## Implementación final (nombres reales)

Este plan se implementó con los siguientes nombres finales (para facilitar traceo/debug):

### Colas (consumers)
- `delete-track` (`DeleteTrackConsumer`)
- `delete-playList` (`DeletePlayListConsumer`)

### Execute endpoints (activities)
MassTransit crea las colas de execute como `<endpointName>_execute`.

- `remove-file-from-bucket` → cola `remove-file-from-bucket_execute`
- `mark-track-as-removing` → cola `mark-track-as-removing_execute`
- `delete-track-from-db` → cola `delete-track-from-db_execute`
- `mark-playList-as-removing` → cola `mark-playList-as-removing_execute`
- `delete-playList-from-db` → cola `delete-playList-from-db_execute`

### Estado de borrado (DB)
- Se introdujo `LifeCycleStatus` en `Track` y `PlayList` con valores `Active` y `Removing`.
- El handler marca `Removing` antes de publicar el evento (y persiste con `SaveChanges`).

> Nota: se eliminó el enfoque de “estado Removing” implícito en `ProcessingStatus` para deletes. El tracking de delete queda en `LifeCycleStatus`.

### Actividades compartidas / reutilizables

Ya existe (y se mantiene):
- `RemoveFileFromBucketActivity` — borra un único objeto de S3. Se usará N veces en cada slip.

Se añaden actividades de DB por aggregate:
- `MarkTrackAsRemovingActivity` — cambia estado `LifeCycleStatus` a `Removing` y persiste.
- `DeleteTrackFromDbActivity` — elimina el Track de EF (cascade elimina relaciones).
- `MarkPlayListAsRemovingActivity` — cambia estado `LifeCycleStatus` a `Removing` y persiste.
- `DeletePlayListFromDbActivity` — elimina la PlayList de EF (cascade elimina relaciones).

### Estructura del RoutingSlip para Track

```
DeleteTrackEvent
  └─► DeleteTrackConsumer (queue: `delete-track`)
    └─► DeleteTrackRoutingSlipBuilder.BuildAsync(...)
      ├─ MarkTrackAsRemovingActivity           (endpoint: `mark-track-as-removing`)
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — original picture
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — small (si IsPicturesProcessed)
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — medium (si IsPicturesProcessed)
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — large (si IsPicturesProcessed)
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — original audio
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — processed audio (si IsAudioProcessed)
      └─ DeleteTrackFromDbActivity             (endpoint: `delete-track-from-db`)
```

> El builder carga la entidad para construir el plan (keys condicionales) antes de arrancar el slip. Las actividades de S3 son idempotentes (NotFound → log Information + continue).

### Estructura del RoutingSlip para PlayList

```
DeletePlayListEvent
  └─► DeletePlayListConsumer (queue: `delete-playList`)
    └─► DeletePlayListRoutingSlipBuilder.BuildAsync(...)
      ├─ MarkPlayListAsRemovingActivity        (endpoint: `mark-playList-as-removing`)
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — original (uploads/{userId}/...) ← solo si no es preset
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — small                          ← solo si no es preset
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — medium                         ← solo si no es preset
      ├─ RemoveFileFromBucketActivity          (endpoint: `remove-file-from-bucket`) — large                          ← solo si no es preset
      └─ DeletePlayListFromDbActivity          (endpoint: `delete-playList-from-db`)
```

### Handler de comando (ambos aggregates)

```csharp
// DeletePlayListCommandHandler — ANTES
await PublishRemoveFileEventsAsync(...); // 4 eventos sueltos
database.PlayLists.Remove(playlist);
await database.SaveChangesAsync();

// DESPUÉS
playlist.LifeCycleStatus = LifeCycleStatus.Removing;
await eventBus.PublishAsync(new DeletePlayListEvent(command.PlayListId, command.UserId));
await database.SaveChangesAsync();
// El workflow se encarga de borrar ficheros y eliminar en DB.
```

```csharp
// DeleteTrackCommandHandler — ANTES
// (ya publicaba DeleteTrackEvent, se mantiene igual)

// DESPUÉS
track.LifeCycleStatus = LifeCycleStatus.Removing;
await eventBus.PublishAsync(new DeleteTrackEvent(command.TrackId, command.UserId));
await database.SaveChangesAsync();

// La diferencia es que ahora el slip tiene pasos granulares, no una activity monolítica.
```

---

## Reglas de negocio a preservar

- Ownership se valida en el handler del comando (igual que ahora), antes de publicar el evento.
- PlayList: solo borrar original/sizes si no son presets.
- PlayList: el original está bajo `uploads/{userId}/...` (cambio reciente).
- Track: respetar estados de procesamiento — si audio está `Pending/Processing`, el handler ya bloquea el delete antes de llegar al workflow.
- EF cascade elimina `PlayListHasTrack` al borrar la PlayList → no hace falta activity explícita para ello.

---

## Idempotencia / reintentos

- RoutingSlip + reintentos de endpoint ya configurados en MassTransit.
- Operaciones S3: idempotentes por diseño. Si el key no existe (`NoSuchKey`/`NotFound`) → log `Information` y `Completed()` (no throw).
- DB: si la entidad ya no existe cuando llega `DeleteTrackFromDbActivity` / `DeletePlayListFromDbActivity`, el workflow se considera `Completed` (misma lógica que ahora).
- Estado `Removing`: permite identificar entidades en proceso de borrado si el sistema se reinicia a mitad del slip.

---

## Plan de implementación (paso a paso)

### 1. Inventario
- Confirmar todos los usos de `RemoveFileEvent` / `RemoveFileConsumer` en la solución.
- Si no tienen otros usos fuera de playlist delete → marcarlos para eliminación en el paso 7.

### 2. Refactorizar Track delete ← **nuevo**
- Crear `MarkTrackAsRemovingActivity` (execute-activity): carga Track, setea estado `Removing`, persiste.
- Crear `DeleteTrackFromDbActivity` (execute-activity): carga Track, llama `Remove()`, persiste.
- Actualizar `DeleteTrackRoutingSlipBuilder`:
  - Carga el Track para calcular el plan (keys condicionales según `IsPicturesProcessed`, `IsAudioProcessed`).
  - Encadena: `MarkTrackAsRemoving` → N × `RemoveFileFromBucket` → `DeleteTrackFromDb`.
- Eliminar `DeleteTrackActivity` monolítica (ya no se usa).
- Ajustar registration DI de MassTransit (quitar activity vieja, registrar las nuevas).

### 3. Crear PlayList delete: evento + consumer + builder
- Añadir `DeletePlayListEvent(PlayListId, UserId)` en `Application/Events`.
- Crear `DeletePlayListConsumer` en Infrastructure: recibe el evento y ejecuta el builder.
- Crear `DeletePlayListRoutingSlipBuilder`:
  - Carga la PlayList para calcular el plan (keys condicionales por presets, ruta de original).
  - Encadena: `MarkPlayListAsRemoving` → N × `RemoveFileFromBucket` → `DeletePlayListFromDb`.

### 4. Crear actividades de PlayList
- `MarkPlayListAsRemovingActivity` (execute-activity): carga PlayList, setea `Removing`, persiste.
- `DeletePlayListFromDbActivity` (execute-activity): carga PlayList, llama `Remove()`, persiste (cascade).

### 5. Actualizar `RemoveFileFromBucketActivity` para NotFound
- Si el SDK lanza excepción por key inexistente → capturar, log `Information("Key {Key} not found, skipping")`, retornar `Completed()`.
- Si es otro error → log `Error` + throw (reintento normal).

### 6. Cambiar `DeletePlayListCommandHandler`
- Reemplazar `PublishRemoveFileEventsAsync` por `eventBus.PublishAsync(new DeletePlayListEvent(...))`.
- No publicar más `RemoveFileEvent` para playlist delete.

### 7. Ajustar DI / registration MassTransit
- Registrar: `DeletePlayListConsumer`, `MarkPlayListAsRemovingActivity`, `DeletePlayListFromDbActivity`.
- Registrar: `MarkTrackAsRemovingActivity`, `DeleteTrackFromDbActivity`.
- Desregistrar: `DeleteTrackActivity` (monolítica), y si procede `RemoveFileConsumer`.

### 8. Eliminar código muerto
- `DeleteTrackActivity` monolítica.
- `RemoveFileEvent` y `RemoveFileConsumer` (si no tienen otros usos).
- Endpoints y registrations asociados.

### 9. Build y verificación
- `dotnet build -c Release` sin warnings de referencias muertas.
- Smoke test manual: borrar un Track y una PlayList, verificar logs de cada activity en orden.