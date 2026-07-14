# Musify — Guía de páginas

Documento vivo de qué debe contener cada página del frontend web. Sirve como mapa
para priorizar features. El sistema de diseño (tokens, recetas, componentes) vive en
el skill `musify-web`; aquí describimos **contenido y estructura**, no estilos.

Leyenda de estado: ✅ hecho · 🟡 parcial / maqueta · ⬜ pendiente.

> **Nota (rediseño app):** se está portando el prototipo `Musify.dc.html` (app: sidebar +
> vistas + player persistente con acento dinámico por pista). La landing de marketing se
> eliminó. Las secciones de abajo describen el diseño anterior y quedan pendientes de
> actualizar a las vistas de la app (Inicio, Buscar, Biblioteca, Subir, Importar, Descargas,
> Playlists). El **player** ya está hecho por componentes en `$lib/components/player/` con el
> tema en `$lib/theme/` (`theme.css` paleta + `color.ts` acento/gradiente).

## Inicio (app) `/` 🟡

- **Hero**: saludo + "Tu música. Sin límites." con color-wash del hue de la pista actual.
- **Acceso rápido = escuchado recientemente** (`player.recentlyPlayed`, hoy en cliente).
  Estado vacío con icono + CTA «Explorar».
- **Tus playlists**: tarjetas mosaico de las playlists (hoy datos de muestra). Estado vacío.

### Backend pendiente (ASP.NET) para Inicio

- **Escuchado recientemente**: registrar reproducciones por usuario (tabla de historial:
  `userId`, `trackId`, `playedAt`) y exponer `GET /me/recently-played?limit=` (o
  `/users/{id}/recently-played`) devolviendo pistas ordenadas por `playedAt` desc, sin
  duplicados. El front lo cargaría en un `load` y lo pasaría al store en vez de `recentlyPlayed`.
- **Playlists por última escucha**: añadir `lastPlayedAt` a la playlist (hoy el schema solo
  tiene `createdAt`/`updatedAt`) y permitir ordenar `GET /playlists/users/{userId}` por ese
  campo (p. ej. `?sort=lastPlayed`), para mostrar «tus últimas escuchadas».
- Las pistas necesitarán **artista** y un **color/hue** (o portada real de la que extraerlo)
  para el arte y el acento dinámico; hoy `TrackApplicationResponse` no trae artista ni color.

## Estructura de rutas

Rutas agrupadas para separar lo público de lo autenticado (los grupos no cambian la URL):

```
src/routes/
  (marketing)/        Público, sin sesión obligatoria
    +page.svelte        /            Landing            ✅
  (app)/              Experiencia de la app
    explore/            /explore     Explorar / buscar  ✅
    upload/             /upload      Subir música       🟡 maqueta
    library/            /library     Tu biblioteca      ⬜
    playlists/          /playlists   Tus playlists      ⬜
      [id]/             /playlists/:id  Detalle playlist ⬜
  login  logout  auth/callback       Auth (endpoints)   ✅
  api/tracks/[id]/stream             Proxy de stream    ✅
```

Componentes globales (en todas las páginas): **Navbar**, **Footer** y **PlayerBar**
(barra de reproducción fija, aparece al reproducir).

---

## 1. Landing `/` ✅ (marketing)

Página de entrada para no autenticados. Vender el producto y llevar a registro/explorar.

- **Hero**: badge de estado (beta), título, subtítulo, CTAs primario (Empezar gratis →
  registro) y secundario (Explorar canciones).
- **Features**: 3 cards (escuchar, playlists, subir).
- **Pendiente sugerido**: sección de capturas/demo, prueba social, FAQ corta.

## 2. Login / Auth ✅ (endpoints)

No es una página visual propia: `/login` inicia el flujo OIDC de Zitadel y redirige.
`?mode=register` arranca en registro; `?returnTo=` vuelve a la ruta pedida tras entrar.
`/logout` cierra sesión. `/auth/callback` intercambia el código y crea la cookie
cifrada `mf_session`. La UI de credenciales la sirve Zitadel, no Musify.

## 3. Explorar / Buscar `/explore` ✅ (app)

Descubrimiento y búsqueda de canciones del catálogo.

- **Buscador**: input de texto (`?q=`) que filtra por nombre en el backend.
- **Contador de resultados** y **grid de tracks** (2→4 columnas): portada, título, fecha,
  botón play/pausa en hover que encola y reproduce vía el player global.
- **Paginación** anterior/siguiente (`?page=`, 24 por página).
- **Estado vacío**: sin resultados / catálogo vacío.
- **Pendiente sugerido**: filtros (más recientes, por usuario), skeletons de carga,
  portada real del track (hoy la API de listado no devuelve la key de imagen).

## 4. Subir música `/upload` 🟡 maqueta (app)

Alta de una canción nueva. Flujo real de backend (para cuando se cablee):
1. `POST /tracks/upload-urls` → URLs presignadas para portada y audio + `intentId`s.
2. `PUT` de cada archivo a su URL presignada (subida directa al almacenamiento).
3. `POST /tracks` con `title`, `pictureIntentId`, `audioIntentId`.
4. El backend procesa el audio (transcodifica a DASH) de forma asíncrona.

Contenido de la página:

- **Zona de audio**: drag & drop o selector; muestra nombre y tamaño del archivo, validación
  de tipo (audio) y tamaño.
- **Portada**: selector de imagen con vista previa cuadrada.
- **Metadatos**: título (obligatorio). Espacio para futuros campos (descripción, etc.).
- **Pipeline visible**: pasos Subir → Procesar → Publicar con su estado.
- **Barra de progreso** de subida y estado de procesado.
- **Estados**: vacío, archivo seleccionado, subiendo, procesando, publicado, error.
- **Requiere sesión** (al cablear: redirigir a `/login?returnTo=/upload`).

## 5. Tu biblioteca `/library` ⬜ (app)

Panel personal del usuario autenticado: lo suyo en un sitio.

- **Tabs / secciones**: «Tus canciones» (`GET /tracks/users/{userId}`) y «Tus playlists»
  (`GET /playlists/users/{userId}`).
- **Tus canciones**: lista con play, y acciones de gestión (borrar `DELETE /tracks/{id}`,
  añadir a playlist).
- **Tus playlists**: grid de portadas + botón «Crear playlist».
- **Estado vacío** con CTA a subir / crear.
- **Requiere sesión**.

## 6. Playlists `/playlists` ⬜ (app)

Listado de las playlists del usuario.

- **Grid** de playlists (portada, nombre, nº de tracks) → enlaza al detalle.
- **Crear playlist**: modal/página con nombre, descripción y portada
  (`POST /playlists`, portada vía `POST /playlists/upload-picture`).
- **Estado vacío** con CTA.
- **Requiere sesión**.

## 7. Detalle de playlist `/playlists/[id]` ⬜ (app)

- **Cabecera**: portada grande, nombre, descripción, nº de tracks, botón «Reproducir todo».
- **Lista de tracks** (`GET /playlists/{id}/tracks`, paginada): índice, título, fecha,
  play, quitar de la playlist (`DELETE /playlists/{id}/tracks/{trackId}`).
- **Acciones del dueño**: editar (`PUT`), borrar (`DELETE`) la playlist, reordenar (futuro).
- **Añadir tracks**: desde explorar/biblioteca (`POST /playlists/{id}/tracks/{trackId}`).

## 8. Perfil de usuario `/users/[id]` ⬜ (futuro)

Vista pública de un usuario: avatar, nombre, sus canciones y playlists públicas
(`GET /users/{id}`, `GET /tracks/users/{id}`, `GET /playlists/users/{id}`).

---

## Player global (PlayerBar) ✅

Barra fija inferior visible al reproducir. Info de la pista, transporte
(anterior/play-pausa/siguiente), barra de progreso y control de volumen. Encola listas
desde cualquier grid (`player.playQueue`). Reproduce DASH vía el proxy
`/api/tracks/:id/stream`, que pide manifiesto + ticket firmado al backend.

## Backend disponible (resumen)

| Recurso | Endpoints |
| --- | --- |
| Tracks | listar/buscar, detalle, por usuario, stream, crear, borrar, upload-urls |
| Playlists | CRUD, tracks de la playlist, add/remove track, upload-picture |
| Users | listar/buscar, detalle, crear |

Cliente tipado en `$lib/server/api.ts` (`openapi-fetch`, estilo ErrorOr). Consumir en
`load`/actions server, nunca desde cliente. Tipos en `src/lib/api/schema.d.ts`.
