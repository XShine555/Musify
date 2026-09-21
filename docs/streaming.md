# Streaming de audio

## Objetivo

Reproducir el audio **sin que los bytes pasen por la API**, pero con control de acceso. La API solo emite un permiso corto; el reverse proxy sirve los bytes desde SeaweedFS.

## Formato: fichero único AAC (.m4a) por HTTP range

El audio se transcodifica a **un único `audio.m4a`** (AAC, contenedor MP4 con `+faststart` para que el átomo `moov` quede al inicio y el seek por range funcione), dentro de la carpeta `Tracks/ProcessedAudios/{AudioFolderName}/`. No hay manifest ni segmentos: el reproductor descarga el fichero por **HTTP range requests** (modelo tipo Spotify para VOD musical).

Multi-bitrate no está implementado hoy; el camino para añadirlo es transcodificar varias calidades (`audio_96.m4a`, …) y que el cliente elija una al iniciar (sin ABR fluido a mitad de pista, igual que Spotify).

## El acceso: stream-ticket de prefijo

Se autoriza **el prefijo de la carpeta** de la pista con un *stream-ticket* de corta vida, y el proxy lo valida en cada petición. Aunque hoy sea un único objeto, el ticket sigue acotado al prefijo `Tracks/ProcessedAudios/<folder>/` (cubre el fichero actual y cualquier calidad futura).

## Flujo

1. `GET /tracks/{id}/stream` (autenticado) → la API autoriza y devuelve:
   ```json
   { "manifestUrl": "http://<gateway>/media/Tracks/ProcessedAudios/<folder>/audio.m4a",
     "ticket": "<JWT RS256>", "expiresInSeconds": 3600 }
   ```
   El ticket lleva el claim `prefix = "Tracks/ProcessedAudios/<folder>/"`.
2. El reproductor abre la URL del audio añadiéndole `?t=<ticket>` (o la cabecera `X-Stream-Ticket`).
3. El **StreamingGateway** (YARP) valida el ticket y reenvía al filer; los bytes van SeaweedFS → cliente (con range/seek).

## El ticket (RS256)

- Lo firma la API con clave **privada** (`StreamTicketService`); el gateway valida con la **pública** (`TicketValidator`). Así el gateway **no puede emitir** tickets, solo verificarlos.
- Claims: `sub`, `prefix`, `aud = media-gateway`, `iss = musify-webapi`, `exp` (TTL ~1h).

**Por qué RS256 (asimétrica)**: separa responsabilidades — emisor (la API) y verificador (gateway) no comparten secreto; el gateway, aunque se comprometa, no puede crear permisos.

## El gateway (validación + proxy)

`TicketValidationMiddleware` en cada petición a `/media/**`:
1. Lee `?t=`, valida firma/aud/iss/exp.
2. Comprueba que la clave pedida **empieza por** el `prefix` del ticket (`StartsWith`). Si no → 403.
3. Quita el `?t=` y deja pasar a YARP, que reescribe `/media/{key}` → `/buckets/webapi-storage/{key}` del filer.

- Sin ticket → **401**; fuera de prefijo → **403**; ok → **200** (y **206** en range requests, así que el seek funciona).
- Pasa `Range`/`Accept-Ranges` y expone esos headers por CORS.

## ¿Un ticket por canción?

Sí. Cada ticket está acotado a **una** carpeta (una pista). Para reproducir otra, se pide otro ticket. Es barato (la API solo firma un JWT) y es lo más seguro: cada permiso abre solo lo que vas a reproducir.

- Si algún día molesta (muchísimos cambios de pista), el mismo mecanismo del claim `prefix` permite ampliar el alcance: a nivel de playlist o de toda la biblioteca (`Tracks/ProcessedAudios/`). Trade-off seguridad ↔ comodidad. Por defecto, **por canción**.

## Pendiente / notas de seguridad

- La política de quién puede pedir el ticket es hoy "cualquier autenticado" (TODO: restringir con `UserHasTrack`/visibilidad).
- El check de prefijo es `StartsWith` literal; conviene endurecer rechazando claves con `..` (path traversal), aunque navegadores y ASP.NET normalizan `..`.
- El ticket viaja en la query (`?t=`); el gateway lo elimina antes de reenviar (no llega a los logs del filer).
