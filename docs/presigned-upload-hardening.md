# Hardening: abuso de pre-signed uploads (Opción B)

## Contexto
Con la Opción B el backend genera URLs pre-firmadas (pre-signed) para que el cliente suba ficheros directamente a S3, y luego el backend crea/actualiza entidades en DB referenciando `pictureName/audioName`.

Esto mejora latencia y descarga el host/servicio, pero abre un vector de abuso: un usuario puede llamar muchas veces a `RequestPlayListPictureUpload` / `RequestTrackUploadUrls` y subir muchos objetos a S3 sin llegar a crear la entidad final (objetos “huérfanos”).

## Qué pasa hoy si el usuario spamea
- Se generan muchas URLs pre-firmadas (costo bajo pero no cero).
- El usuario puede subir muchos objetos a S3 sin relación con una entidad en DB.
- El impacto principal es:
  - coste/uso de S3 (storage + requests)
  - posible saturación del componente de generación de URLs

## Objetivo
Reducir abuso y coste, manteniendo:
- UX simple
- backend sin streams
- workflows asíncronos + MassTransit Outbox

## Qué significa “sólido” aquí
Como el upload es directo a S3 (o compatible), el backend no puede garantizar “single-use” del upload en el lado del storage mientras una URL sea válida. Por tanto, el objetivo realista es:

- Asegurar que **cualquier abuso solo pueda impactar un prefijo temporal** barato de limpiar.
- Poner límites duros (cuotas) en DB para que el coste tenga un techo.
- No aceptar referencias a objetos no válidos (verificación antes de consumir).
- Evitar pérdida de datos en reemplazos (no borrar la versión previa hasta éxito).

## Principio de configuración (no hardcode)
Todas las rutas/prefijos/TTLs/límites deben ser **configurables**.

Regla:
- En `Application`/`Infrastructure` no debe haber strings hardcodeadas de keys S3 (p.ej. `temp/`, `uploads/`, `PlayLists/`, `Tracks/`).
- Las rutas deben centralizarse en opciones/config (p.ej. `StorageRoutes`, `PlayListRoutes`, `TrackRoutes`, o similar), de forma que cambiar la convención de keys no requiera tocar lógica.

Los ejemplos de este documento son **convenciones sugeridas** y deben leerse como plantillas parametrizadas.

## Invariantes de seguridad (reglas que no se rompen)
- El cliente solo puede escribir en rutas temporales bajo `{TempRootPrefix}/...` (o el prefijo temporal equivalente). La ruta final **nunca** recibe credenciales pre-firmadas.
- Cada intent emite un `Key` **nuevo** (GUID). No hay keys “estables” para reusar.
- Una entidad solo apunta a un `pictureName/audioName` si su intent asociado se valida y se consume.
- Reemplazos: el borrado de la versión anterior ocurre **al final** del workflow, tras actualizar DB.

## Prioridad (coste alto)
Si el coste es el principal riesgo, el orden recomendado es:
1) **Cuotas duras en DB** (intents activos + bytes activos) + job de expiración.
2) Prefijo temporal + cleanup por prefijo (lifecycle si existe, y job como fallback).
3) Validación de objeto (HEAD) antes de consumir.
4) (Opcional) Migrar a pre-signed POST si el backend de storage lo soporta bien.

## Decisiones validadas (no tocaría)
- El orden de prioridad (cuotas → prefijo temporal → HEAD → POST) es el más sensato cuando el **coste** es el riesgo principal.
- La advertencia sobre S3-compatible / SeaweedFS es importante: el diseño debe ser seguro sin depender de features avanzadas.
- El principio de no hardcodear rutas y centralizar prefijos/convenciones en config es coherente con el estilo del repo.
- La semántica de errores debe permanecer separada del mapeo a códigos/respuestas (responsabilidad del host/presentación).
- En reemplazos, no borrar la versión anterior hasta éxito del workflow es clave para resiliencia con Courier.

## Mitigaciones recomendadas (pueden coexistir)

### 1) “Upload intents” en DB (control y conciliación)
Persistir una intención de upload cuando se genera un pre-signed URL.

> Importante: **no usar el nombre original del fichero** como `pictureName/audioName`. La key debe usar un **identificador generado** (p.ej. GUID) para evitar colisiones, inputs “raros” y facilitar cleanup.

Sugerencia práctica:
- `objectName = {guid}.{ext}` (o directamente `{guid}` si la extensión no aporta valor)
- Guardar `originalFileName` solo como metadata opcional (si se necesita para UI), pero **nunca** como key.

Modelo sugerido (mínimo):
- `UploadIntent`
  - `Id`
  - `UserId`
  - `Bucket`
  - `Key`
  - `ContentType`
  - `ExpectedSizeBytes` (opcional; si el caller conoce el tamaño)
  - `ExpiresAt`
  - `Status`: `Issued | Consumed | Expired`
  - `CreatedAt`

Opcional (útil para auditoría/UX):
- `OriginalFileName`
- `Purpose` (`TrackPicture | TrackAudio | PlayListPicture`)

Reglas:
- Al pedir URL (Request*Upload*):
  - insertar intent `Issued`
  - imponer límites por usuario (ver “Cuotas”)
  - si supera el límite: fallar con un error semántico (p.ej. `UploadIntentLimitExceeded`)
- Al crear/actualizar entidad (Create*/Update* V2):
  - validar que el `pictureName/audioName` recibido corresponde a un intent `Issued` del mismo `UserId` y no expirado
  - validar que el objeto existe en S3 (HEAD) y que tamaño / `Content-Type` cumplen la policy del backend
  - marcar intent como `Consumed`

#### Cuotas (anti-cost) 🔴 Alta
Además del conteo de intents, imponer **cuotas por bytes** para que el coste tenga techo.

Recomendación mínima:
- `MaxActiveUploadIntentsPerUser` (número)
- `MaxActiveUploadBytesPerUser` (bytes) — suma de objetos temporales “en vuelo” por usuario

Cómo aproximarlo sin streams:
- Guardar `ExpectedSizeBytes` en el intent (si está disponible).
- Si no se dispone del tamaño, usar un valor conservador por `Purpose` (p.ej. imagen <= X, audio <= Y) y validarlo en HEAD antes de consumir.

> Nota: si el caller puede mentir sobre tamaño, no confíes ciegamente en `ExpectedSizeBytes`; úsalo como guardrail y valida al consumir.

#### Semántica de errores (intent inválido) 🟡 Media
Definir errores semánticos y dejar el mapeo al “caller” (host/presentación):
- `UploadIntentLimitExceeded`
- `UploadIntentNotFoundOrNotAccessible` (si se quiere no filtrar existencia)
- `UploadIntentAlreadyConsumed`
- `UploadIntentExpired`
- `UploadObjectNotFound`
- `UploadObjectPolicyViolation` (tamaño, `Content-Type`, extensión, etc.)

#### Nota sobre race conditions en el límite 🟡 Media
El check “`count(Issued) < Max`” es vulnerable a concurrencia (dos requests paralelas pueden pasar el check y crear 2 intents extra).

Mitigaciones (de menor a mayor robustez):
- Ejecutar **check + insert** en la **misma transacción** con aislamiento fuerte (p.ej. `SERIALIZABLE`).
- Usar un mecanismo de lock por usuario (p.ej. advisory lock en PostgreSQL) durante la emisión.
- Modelar el límite con una tabla/contador por usuario y aplicar **update condicional** (estilo “compare-and-swap”).

#### Job de expiración de intents 🔴 Alta
Aunque `ExpiresAt` esté en DB, hace falta un mecanismo que marque y limpie intents caducados:
- Un job periódico (worker/cron) que:
  - marque intents `Issued` con `ExpiresAt < now` como `Expired`
  - opcionalmente encole el borrado del objeto S3 (si existe)

Si se aplica lifecycle en S3 (ver sección 2), el job puede ser “solo DB” (marcar `Expired`) y dejar el borrado real a S3.

Adicionalmente, este job es un buen sitio para:
- borrar intents `Expired` antiguos (retención de auditoría configurable)
- emitir métricas/contadores (intents expirados por usuario)

Ventajas:
- Previene spam con reglas duras y auditables.
- Permite cleanup de huérfanos con certeza.

Coste:
- entidad + migración + lógica de validación.

### 2) Limitar el scope del pre-signed URL (reducción de daño)
Aplicar restricciones conservadoras:
- Reducir TTL:
  - `ExpiresInSeconds` más bajo (p.ej. 60–120s en vez de 600s)
- Validar/limitar:
  - `ContentType` permitido
  - extensiones permitidas (`FileType`)
- Usar keys temporales por usuario:
  - Opción recomendada (facilita lifecycle por prefijo):
    - `{TempRootPrefix}/{userId}/playlists/{guid}.{ext}`
    - `{TempRootPrefix}/{userId}/tracks/{guid}.{ext}`
  - Alternativa (si se quiere mantener todo bajo `uploads/{userId}`):
    - `{UploadsRootPrefix}/{userId}/PlayLists/TempUploads/{guid}.{ext}`
    - `{UploadsRootPrefix}/{userId}/Tracks/TempUploads/{guid}.{ext}`

Luego, en el workflow/worker:
- mover/copiar desde la ruta temporal (p.ej. `temp/...` o `.../TempUploads/...`) a la ruta final (`OriginalPictures/...` / `OriginalAudios/...`)
- borrar el objeto temporal

Ventajas:
- Cleanup masivo sencillo (por prefijo `{TempRootPrefix}/`)
- Reduce el impacto de huérfanos.

#### S3 Lifecycle Rule en `TempUploads` 🔴 Alta
Configurar una **Lifecycle Rule** en el bucket para auto-expirar objetos bajo el prefijo de temporales, por ejemplo:
- Prefijo: `{TempRootPrefix}/`
- Expiración: 1–7 días (según tolerancia a reintentos)

Nota: las reglas de lifecycle por **prefijo** no soportan comodines en medio (p.ej. `uploads/*/...`).
Si se usa la alternativa `uploads/{userId}/.../TempUploads/...`, para filtrar correctamente se recomienda:
- cambiar la convención de key para que el prefijo sea común (p.ej. `{TempRootPrefix}/...`), o
- usar **tags** en los objetos (p.ej. `temporary=true`) y filtrar la lifecycle rule por tag (si se puede firmar el tagging en el upload).

Esto evita que el coste de huérfanos crezca indefinidamente incluso si el job de cleanup falla.

#### Cleanup backend (fallback) 🔴 Alta
No dependas solo de lifecycle del storage (en S3 compatibles puede variar). Añadir un job de cleanup que:
- liste por prefijo temporal `{TempRootPrefix}/`
- borre objetos con edad > `TempUploadsRetentionDays`

Este job debe ser:
- incremental (paginación)
- tolerante a errores (reintentos)
- con límites de throughput (para no auto-generar costes por borrar en masa)

#### Restricción de `Content-Type` en S3 🟡 Media
Si el objetivo es enforcement “duro” a nivel de S3 (no solo validación posterior), **pre-signed POST** suele ser más robusto que PUT.

**pre-signed PUT** (lo actual en este repo):
- Permite fijar `Content-Type` si se pre-firma incluyendo ese header.
- Es más limitado para imponer condiciones ricas (tamaño, tags, etc.).
- Recomendación: combinar con validación posterior (HEAD) antes de consumir el intent.

**pre-signed POST** (recomendado si buscas más control):
- Permite policy conditions como:
  - `content-length-range` (tamaño máximo)
  - `eq`/`starts-with` para `Content-Type`
  - key/prefijo (el cliente no elige keys fuera del scope)
  - headers requeridos (p.ej. SSE)
  - opcional: tagging (p.ej. `temporary=true`) para lifecycle por tag

Trade-off: el cliente sube con `multipart/form-data` (POST), pero el backend sigue sin manejar streams.

#### Nota S3-compatible / SeaweedFS 🟡 Media
No todos los backends S3 compatibles implementan al 100%:
- policy conditions de POST
- tagging en upload
- respuestas/errores idénticos a AWS

Recomendación:
- Diseñar el sistema para ser seguro **sin depender** de condiciones avanzadas (keys únicas + prefijo temporal + cuotas + verificación HEAD).
- Tratar POST/condiciones como una mejora opcional, tras validar compatibilidad del backend.

#### Reemplazos: no borrar originales antiguos hasta éxito ✅
Al actualizar una imagen/audio existente:
- Subir el nuevo binario a la ruta temporal (p.ej. `{TempRootPrefix}/.../{newGuid}`).
- Procesar (resize/transcode) y subir a rutas finales con nombres nuevos.
- Actualizar la entidad en DB para apuntar a los nuevos `*PictureName/*AudioFolderName`.
- **Solo al final** borrar los objetos antiguos (original + sizes / audio procesado), cuando el workflow haya completado.

Esto evita perder la versión anterior si el worker falla a mitad (punto crítico para resiliencia).

## Validación del objeto antes de consumir (HEAD) 🔴 Alta
Antes de consumir un intent y actualizar DB:
- `HEAD Bucket/Key`
- validar `Content-Type` esperado (según `Purpose`)
- validar tamaño máximo (`MaxUploadBytes` o por propósito)
- (opcional) validar extensión inferida de `Key` si se usa

Si falla la validación:
- no consumir el intent
- marcarlo como violación o dejarlo expirar
- el cleanup se encarga del objeto

## Orden de implementación sugerido
1) Upload intents en DB (límite de activos + validación al crear/actualizar)
2) Reducir TTL y mover a rutas temporales + S3 lifecycle + cleanup
3) Si se requiere enforcement fuerte: migrar a pre-signed POST + policy conditions

## Parámetros recomendados (config)
- `UploadUrlExpiresInSeconds` (default: 120)
- `MaxActiveUploadIntentsPerUser` (default: 5)
- `MaxActiveUploadBytesPerUser` (default sugerido: depende de coste; p.ej. 200MB)
- `AllowedPictureExtensions` / `AllowedAudioExtensions`
- `AllowedContentTypes` (lista blanca)
- `TempUploadsRetentionDays` (default: 3)
- `PresignedUploadMethod` (default sugerido: `POST` si se quiere enforcement fuerte; `PUT` si se prioriza simplicidad)
- `MaxUploadBytes` (límite máximo de tamaño por upload)

Jobs (config):
- `UploadIntentsExpirationJobIntervalSeconds`
- `TempUploadsCleanupJobIntervalSeconds`
- `ExpiredIntentsRetentionDays`

Rutas/prefijos (ejemplos):
- `TempRootPrefix` (default sugerido: `temp`)
- `UploadsRootPrefix` (default sugerido: `uploads`)

## Notas
- Estas defensas complementan el Outbox: el Outbox garantiza entrega de eventos tras commit, pero no evita el abuso de generación de URLs o almacenamiento de objetos huérfanos.
