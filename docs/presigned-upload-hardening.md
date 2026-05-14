# Hardening: abuso de pre-signed uploads (Opción B)

## Contexto
Con la Opción B el backend genera URLs pre-firmadas (pre-signed) para que el cliente suba ficheros directamente a S3, y luego el backend crea/actualiza entidades en DB referenciando `pictureName/audioName`.

Esto mejora latencia y descarga la API, pero abre un vector de abuso: un usuario puede llamar muchas veces a `RequestPlayListPictureUpload` / `RequestTrackUploadUrls` y subir muchos objetos a S3 sin llegar a crear la entidad final (objetos “huérfanos”).

## Qué pasa hoy si el usuario spamea
- Se generan muchas URLs pre-firmadas (costo bajo pero no cero).
- El usuario puede subir muchos objetos a S3 sin relación con una entidad en DB.
- El impacto principal es:
  - coste/uso de S3 (storage + requests)
  - posible saturación del endpoint de generación de URLs

## Objetivo
Reducir abuso y coste, manteniendo:
- UX simple
- backend sin streams
- workflows asíncronos + MassTransit Outbox

## Mitigaciones recomendadas (pueden coexistir)

### 1) Rate limiting por usuario/IP (primera línea de defensa)
Aplicar limitación en el host/API (no en `Application`). Ejemplos:
- `N` requests/min por `UserId`
- opcional: `M` requests/min por IP

Ventajas:
- Corta abuso sin tocar DB/S3.
- No cambia dominio.

Nota: requiere acceso al proyecto host (controllers/minimal API) para aplicarlo.

### 2) “Upload intents” en DB (control y conciliación)
Persistir una intención de upload cuando se genera un pre-signed URL.

Modelo sugerido (mínimo):
- `UploadIntent`
  - `Id`
  - `UserId`
  - `Bucket`
  - `Key`
  - `ContentType`
  - `ExpiresAt`
  - `Status`: `Issued | Consumed | Expired`
  - `CreatedAt`

Reglas:
- Al pedir URL (Request*Upload*):
  - insertar intent `Issued`
  - imponer límite por usuario: p.ej. `MaxActiveIntentsPerUser = 5`
  - si supera el límite: devolver `429 Too Many Requests` (o `Conflict`) según convención del API
- Al crear/actualizar entidad (Create*/Update* V2):
  - validar que el `pictureName/audioName` recibido corresponde a un intent `Issued` del mismo `UserId` y no expirado
  - marcar intent como `Consumed`

Ventajas:
- Previene spam con reglas duras y auditables.
- Permite cleanup de huérfanos con certeza.

Coste:
- entidad + migración + lógica de validación.

### 3) Limitar el scope del pre-signed URL (reducción de daño)
Aplicar restricciones conservadoras:
- Reducir TTL:
  - `ExpiresInSeconds` más bajo (p.ej. 60–120s en vez de 600s)
- Validar/limitar:
  - `ContentType` permitido
  - extensiones permitidas (`FileType`)
- Usar keys temporales por usuario:
  - `PlayLists/TempUploads/{userId}/{guid}.{ext}`
  - `Tracks/TempUploads/{userId}/{guid}.{ext}`

Luego, en el workflow/worker:
- mover/copiar de `TempUploads/...` a la ruta final (`OriginalPictures/...` / `OriginalAudios/...`)
- borrar el objeto temporal

Ventajas:
- Cleanup masivo sencillo (por prefijo `TempUploads/{userId}`)
- Reduce el impacto de huérfanos.

## Orden de implementación sugerido
1) Rate limiting en API (rápido + mayor impacto)
2) Upload intents en DB (límite de activos + validación al crear/actualizar)
3) Reducir TTL y mover a rutas temporales (`TempUploads/...`) + cleanup

## Parámetros recomendados (config)
- `UploadUrlExpiresInSeconds` (default: 120)
- `MaxActiveUploadIntentsPerUser` (default: 5)
- `AllowedPictureExtensions` / `AllowedAudioExtensions`
- `AllowedContentTypes` (lista blanca)

## Notas
- Estas defensas complementan el Outbox: el Outbox garantiza entrega de eventos tras commit, pero no evita el abuso de generación de URLs o almacenamiento de objetos huérfanos.
