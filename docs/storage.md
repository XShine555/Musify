# Almacenamiento (SeaweedFS / S3)

## Qué es

**SeaweedFS** es el almacén de objetos, compatible con la API S3. Guarda los originales subidos y los derivados (audio `.m4a`, miniaturas). Se accede vía `AWSSDK.S3` desde `StorageService` (`IStorageService`).

Expone:
- **S3 API** (`:8333`) — operaciones S3 (presigned URLs, put/get/list…).
- **filer HTTP** (`:8888`) — sirve los ficheros por ruta; lo usa el **StreamingGateway**.
- master (`:9333`) y volume server — internos.

Bucket de la app: **`webapi-storage`** (`ApplicationStorage:Bucket`). Importante: la WebApi y el Worker usan **el mismo bucket** (el worker procesa lo que sube la API).

## Estructura de claves (layout)

- `temp/{userId}/Tracks/{guid}.{ext}` — subidas temporales (objetivo de las presigned PUT, antes de consumir el intent).
- `uploads/{userId}/Tracks/OriginalPictures|OriginalAudios/...` — originales ya consumidos.
- `Tracks/SmallPictures|MediumPictures|LargePictures/...` — miniaturas generadas.
- `Tracks/ProcessedAudios/{AudioFolderName}/` — audio transcodificado (`audio.m4a`, AAC con faststart).

Las claves se construyen **siempre** con el helper `StorageKey.Combine(...)` (en `Application/Shared`), que une segmentos con `/` limpiando espacios y barras. Está centralizado para estandarizar y no repetir lógica.

> En el filer, los objetos S3 viven bajo `/buckets/{bucket}/{key}` — de ahí que el gateway reescriba `/media/{key}` → `/buckets/webapi-storage/{key}`.

## URLs prefirmadas (presigned)

Para **no** hacer pasar ficheros grandes por la API, el cliente habla directo con S3 mediante URLs firmadas de corta vida:

- **Subidas (PUT)**: `RequestTrackUploadUrls` / `RequestPlayListPictureUpload` generan presigned PUT (con `Content-Type` y `If-None-Match: *` firmados, expiración ~120s). El cliente sube directo a S3.
- **Imágenes (GET)**: las miniaturas se sirven con presigned GET (un objeto = una URL).

**Por qué presigned y no proxiar**: subir un audio de ~100 MB a través de la API consumiría memoria/ancho de banda de los servidores de aplicación y escalaría mal. La presigned PUT directa a S3 es el patrón estándar y correcto.

### Upload intents y cuota
Cada `upload-urls` crea **UploadIntents** (reservas en estado `Issued`) y devuelve las URLs. Al crear la pista/playlist se **consumen**. Hay cuota por usuario (`MaxActiveUploadIntentsPerUser`, bytes…) para evitar abuso. El `UploadIntentExpirationJob` (en el Worker) marca como expirados los intents caducados y libera la cuota; si ese job no corre, los intents colgados se acumulan y bloquean nuevas subidas.

## Qué es público y qué privado (despliegue)

| Exponer a internet | Mantener privado |
|---|---|
| S3 API `:8333` (tras TLS) — necesario para presigned PUT/GET del cliente | filer `:8888` (solo el gateway, en red interna) |
| StreamingGateway `:8081` | master `:9333`, volume server, UI de admin |

- **El audio** se sirve por el gateway → SeaweedFS puede quedar privado para ese flujo.
- **Subidas e imágenes** usan presigned → el cliente toca el endpoint S3 directamente, así que `:8333` debe ser alcanzable (pero inútil sin firma válida; es el modelo de cualquier bucket S3/MinIO).
- En **producción**: `InfrastructureStorage:Address` debe ser el **endpoint S3 público** (la firma se calcula para ese host); si los servidores no lo alcanzan internamente, se usa config dividida (host público para firmar / interno para operaciones server-side).

**Regla**: el gateway y, como mucho, el `:8333` firmado son la única superficie pública; master/volume/filer/admin **nunca** se exponen.
