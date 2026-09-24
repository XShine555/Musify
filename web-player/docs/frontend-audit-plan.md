# Plan de refactor tras la auditoría del frontend

Plan de trabajo derivado de la auditoría del `web-player` (2026-09-24). Cada parte es un PR independiente, revisable por separado, que deja `npm run check` y `npm run lint` en verde. Antes de empezar una parte, mira el **Registro** del final.

Leyenda de tamaño: **S** (< 1 h) · **M** (medio día) · **L** (1 día o más). Las partes marcadas con 🔗 dependen de cambios en `Musify.Api`.

## Orden y dependencias

```
P0 Red de seguridad
 └─ P1 Seguridad ─ P2 Reproductor ─ P3 Bugs de páginas ─ P4 Código muerto
                                                            └─ P5 Modelo Track único
                                                                 ├─ P6 Capa de servidor ─ P7 Formularios y validación
                                                                 │                         └─ P8 Rendimiento 🔗
                                                                 └─ P9 Responsabilidades ─ P10 Componentes y páginas
                                                                                            └─ P11 Consistencia ─ P12 Guardarraíles y docs
```

P1 a P4 son arreglos acotados y se pueden mergear en cualquier orden. De P5 en adelante el orden importa: cada parte simplifica la siguiente.

---

## P0 · Red de seguridad (M)

Objetivo: tener tests de las funciones puras antes de tocar lógica.

- Añadir Vitest (`vitest`, script `npm run test`, incluido en `lint` o en CI).
- Tests para lo que ya es puro: `fmtTime`, `fmtDurationLong`, `plural`, `albumMeta` (`utils/format.ts`), `findTopResult` (`data/search.ts`), `findConflict`, `genreHue` (`data/genres.ts`), `pickGreeting` (`server/greeting.ts`), `parseAlbumForm` (`server/albumForm.ts`), `appendUnique`, `shuffle`.
- Extraer las operaciones de cola de `PlayerState` a funciones puras en `player/queue.ts` (`insertNext`, `append`, `move`, `nextIndex`, `previousIndex`) y testearlas. `PlayerState` pasa a llamarlas. Esto prepara P2.

**Cierre:** `npm run test` en verde y cubre las funciones anteriores.

---

## P1 · Seguridad (S)

1. **Redirección abierta con `returnTo`.**
   - Nueva función `safeReturnTo(value: string | null | undefined): string` en `$lib/server/auth.ts`: acepta solo rutas que empiecen por `/` y no por `//` ni `/\`; si no, devuelve `/`.
   - Aplicarla en `routes/auth/+page.server.ts` (ambos usos), en `routes/auth/login/+server.ts` (antes de guardar la cookie) y en `routes/auth/callback/+server.ts` (al leer la cookie).
2. **`SESSION_SECRET` obligatorio.** En `$lib/server/config.ts`, `sessionSecret` pasa por `required()` y exige una longitud mínima de 32 caracteres. Actualizar el comentario de `.env.example`.
3. **Validar las entradas de los endpoints `/api`.**
   - `api/likes/+server.ts`: no reenviar el body tal cual; leer `trackId`, comprobar que es un string no vacío y devolver 400 si no.
   - `api/listens/[id]/progress/+server.ts`: envolver `request.json()` para devolver 400 con JSON inválido en vez de 500.
   - `server/image.ts`: `encodeURIComponent(id)` al construir la URL de upstream.
   - `api/tracks/+server.ts`: acotar `pageSize` con un máximo (`MAX_PAGE_SIZE` en `config.ts`).

**Cierre:** `/auth?returnTo=https://example.com` acaba en `/`; arrancar sin `SESSION_SECRET` falla con un mensaje claro; tests de `safeReturnTo`.

---

## P2 · Reproductor y cola (M)

1. **Duplicados en la cola** (`player.svelte.ts` `playNext`/`appendToQueue`, `Queue.svelte`).
   - `playNext` mueve la pista si ya estaba en la cola, en vez de duplicarla.
   - `appendToQueue` omite las pistas que ya están pendientes.
   - Con esto las claves `(track.id)` vuelven a ser únicas y `#index()` es fiable.
2. **Fin de cola.** Con repetir desactivado, al terminar la última pista el reproductor se detiene en vez de volver al principio. Si más adelante se quiere "repetir todo", `repeat` pasará a `'off' | 'all' | 'one'` (queda fuera de este plan).
3. **La cola se abre sola** (`+layout.svelte:44`). Abrirla solo la primera vez que empieza a sonar algo en la sesión (`currentId` pasa de `null` a un valor) y no volver a abrirla si el usuario la cierra.
4. **Rollback de "Me gusta"** (`liked.svelte.ts:69`). En caso de error, revertir solo la entrada de ese id, no sustituir el mapa entero.
5. **localStorage seguro.**
   - Nuevo `$lib/utils/storage.ts` con `readStorage(key, fallback)` y `writeStorage(key, value)` envueltos en try/catch.
   - Volumen: validar y acotar a 0–100 (hoy puede quedar `NaN`).
   - Unificar claves: `musify.player.volume`, `musify.player.muted`, `musify.theme`.
   - Persistir el volumen siempre, no solo cuando existe el elemento `<audio>`.

**Cierre:** añadir la misma pista dos veces a la cola no rompe nada; la cola termina; tests de `player/queue.ts` actualizados; prueba manual en el Browser pane.

---

## P3 · Bugs de páginas y datos (M)

1. **Permisos en la playlist** (`playlists/[id]`).
   - El `load` devuelve `isOwner`, comparando el propietario de la playlist con `locals.user.sub`. Si la API no expone el propietario, añadirlo al DTO 🔗.
   - Mostrar Editar, Eliminar y "Quitar" solo al propietario. Añadir `BackLink` como en el álbum y la mezcla, y `section: '/playlists'` cuando sea propia.
2. **Una sola fuente para las playlists del usuario.**
   - `+layout.server.ts` carga las playlists del usuario con `PLAYLIST_PICKER_PAGE_SIZE` y devuelve también `totalItemCount`.
   - La barra lateral muestra las 8 primeras y usa el total real en el contador.
   - Los menús contextuales (inicio, explorar, mezcla) usan esa lista desde `page.data` y así explorar y mezcla dejan de pedirla aparte.
3. **Playlist destacada de inicio.**
   - Nuevo endpoint `api/playlists/[id]/tracks/+server.ts`, análogo al de álbumes. "Reproducir" y "Aleatorio" cargan la lista completa en ese momento.
   - La meta muestra el total real. La duración se quita hasta que la API la devuelva (P8).
   - La promesa se resuelve con `{#await}` en vez de con un `$effect` que copia a estado.
4. **Borrar canción de la biblioteca con confirmación.** `ConfirmDialog` acepta `fields` (inputs ocultos) y `library` lo usa, igual que álbumes y playlists.
5. **Explorar.**
   - Usar `plural()` para el número de canciones de un álbum.
   - Contadores de chips y "N coincidencias" a partir de `totalItemCount` de cada respuesta, no de lo cargado.
6. **Enlace `/terms`** en la subida: quitar el enlace hasta que exista una página de términos (o crearla, si se decide).
7. **`playbackConfig.ts`:** no guardar en caché cuando la respuesta trae `error`.
8. **`ImageDropzone`:** liberar la URL anterior con `URL.revokeObjectURL` al cambiar de archivo y al desmontar.
9. **Textos.**
   - "Populares esta semana" duplica "Continuar escuchando": quitar la sección hasta que haya un endpoint real de más escuchadas (o renombrarla a "Escuchado recientemente").
   - `+error.svelte`: quitar la promesa de modo sin conexión.
   - "Retomalo" → "Retómalo"; "Modo blanco" → "Modo claro".

**Cierre:** un no propietario no ve acciones de edición; la barra lateral cuenta bien; la destacada reproduce la lista completa.

---

## P4 · Código muerto (S)

Borrar sin sustituto:

- `player.svelte.ts`: `playPlaylist`, `playlistTracks`, `playlists`, interfaz `Playlist`, getter `isPlaying`, `playNextItem` (usar `playNext([item])`).
- `theme/palette.ts`: `Accent.gradient` (y con ello el tipo `Accent`, que queda en `string`).
- `IconButton`: el mapa `TEXT` y la prop `tone` pasan a `plain` o por defecto; `muted` y `subtle` son idénticos.
- `Artwork`: prop `fallback` y sus ramas `gradient` y `none`.
- `MediaIdentity.badge`, `FollowButton.userId`, `EqBars.size` y `EqBars.barWidth`.
- Acciones `follow` y `unfollow` en `explore/+page.server.ts` y en `user/[id]/[list=followList]/+page.server.ts`, con el `form?.message` que solo servía para ellas.
- Utilidades `media-grid` y `text-count` de `layout.css` (y su mención en `CLAUDE.md`).
- `TrackList.svelte`: el `use:enhance` redundante (queda `use:enhance`) y los `stopPropagation` de la línea 141.
- `+page.server.ts` (inicio): el `redirect` previo a `requireUser`. `+layout.server.ts`: la rama `accountUrl = null`.
- Clases que no hacen nada: `hover:text-fg-2` sobre `text-fg-2` (SegmentedControl, Sidebar, ArtistLink) y el `hidden sm:flex` anidado de `PlayerBar`.
- Separadores vacíos (`<span></span>`, `<div class="flex-1">`) en `CoverForm` y `TopBar`: sustituir por `ml-auto`.
- `sessionCookieOptions` deja de exportarse.

**Cierre:** `grep` de cada símbolo sin resultados; la app se comporta igual.

---

## P5 · Modelo `Track` único (L)

Objetivo: una sola forma de pista, normalizada en la frontera del servidor. Es el cambio que más código quita.

1. **Tipos de dominio** en `$lib/types.ts`:
   ```ts
   export interface Track {
   	id: string;
   	title: string;
   	artist: string | null;
   	ownerUserId: string | null;
   	explicit: boolean;
   	duration: number;
   	listensCount: number;
   }
   export interface LikedTrack extends Track {
   	likedAt: number;
   }
   ```
   Más `Album` y `Playlist` normalizados (`trackCount: number`, `releaseYear: number | null`, ids como `string`).
2. **Mappers de servidor** en `$lib/server/mappers.ts`: `toTrack`, `toAlbum`, `toPlaylist`, `toMixTrack` (sustituye a `mixItemTrack`). Hacen todas las conversiones `Number()` y `String()`. Todos los `load` y los endpoints `/api` que devuelven pistas los usan.
3. **El reproductor usa `Track`.**
   - Se eliminan `PlayerTrack`, `QueueItem`, `ApiTrackLike`, `LikeToggleInput` y `TrackListTrack` (que pasa a ser `Track & { date?: string | number }`).
   - Se eliminan `toQueueItems`, `toQueueItem`, `trackFromQueueItem` y `toTrack` del cliente.
   - `current` pasa a ser `Track | null` en vez del centinela `EMPTY` con `id: ''`.
4. **Páginas.** Quitar todos los `Number(...)`, los `?? undefined` y los mapeos intermedios (`listTracks`, `songRows`, `spotlightRows`, `popularTracks`…).

**Cierre:** `grep -rn "Number(" src/routes --include=*.svelte` vacío; `isExplicit` solo aparece en `mappers.ts`; tests de los mappers.

---

## P6 · Capa de servidor (M)

1. **Helpers en `$lib/server/api.ts`.**
   - `apiFor(event)` sustituye los 15 `createApiClient({ fetch, accessToken: locals.accessToken ?? undefined })`.
   - Exportar `type ApiClient` (hoy se redefine en `follows.ts`).
   - `loginRedirect(url)`, usado por `requireUser`, `optionalUser` y `+layout.server.ts`.
2. **Envoltorio de acciones** `authedAction(handler)`: comprueba el token (401 si falta), crea `api`, lee `form` y pasa `{ api, form, params, locals }`. Sustituye el bloque `requireAccessTokenAction` + `typeof` + `formData` + `createApiClient` de las ~10 acciones.
3. **Lectura de formularios:** `formString(form, key)` y `formFile(form, key)` (devuelve `null` si el archivo viene vacío). Sustituyen los 17 `String(form.get(...) ?? '')` y los `instanceof File && size > 0`.
4. **Nombres.**
   - `unwrapOrFail` → `failOnError` (no desenvuelve nada).
   - Desestructurar siempre como `const { data, error } = ...` y usar `catch (err)`.
   - La acción `rename` de playlist pasa a `edit` y devuelve `{ edited: true }`, como la de álbum.
5. **Sin aserciones no nulas:** quitar `urlsResult.data!` y `trackResult.data!` de `upload/+page.server.ts` usando el patrón de `failOnError` que estrecha el tipo.
6. **`+layout.server.ts`:** una sola función para las tres cargas con fallback, sin `try/catch` duplicado (openapi-fetch solo lanza con error de red).
7. **Tamaños de página:** todos los `pageSize` salen de `config.ts` (quitar `50`, `200`, `500` sueltos y el `PAGE_SIZE` local de `library`). Quitar el alias `EXPLORE_PAGE_SIZE as PAGE_SIZE`.

**Cierre:** ninguna acción repite la plantilla de autenticación; `grep "createApiClient(" src/routes` vacío.

---

## P7 · Formularios y validación compartida (M)

1. **Límites en un único sitio:** `$lib/validation.ts` (compartido entre cliente y servidor):
   ```ts
   export const LIMITS = {
   	playlistName: 100,
   	playlistDescription: 300,
   	albumTitle: 200,
   	albumDescription: 256,
   	trackTitle: 100
   } as const;
   ```
   Antes de fijarlos, comprobar los validadores de `Musify.Api` y usar sus valores. `ALBUM_EARLIEST_YEAR` se mueve aquí.
2. **Parsers de servidor** en `$lib/server/forms/`: `parseAlbumForm` (existente), `parsePlaylistForm` (nuevo, sustituye el código duplicado de `create` y `edit`), `parseTrackUploadForm` (sacado de `upload/+page.server.ts`). Todos validan con `LIMITS`.
3. **Portadas:** `uploadOptionalCover(api, endpoint, file)` sustituye los 4 bloques `if (cover ...) uploadPresignedImage(...)`.
4. **Formularios de cliente:** `AlbumForm`, `PlaylistForm` y la subida usan `LIMITS` en `maxlength` y en el contador. Añadir el contador que falta en `AlbumForm` para que los tres se comporten igual.
5. `formatSize` de la subida pasa a `utils/format.ts`.

**Cierre:** cambiar un límite en `LIMITS` actualiza cliente y servidor; tests de los parsers.

---

## P8 · Rendimiento 🔗 (M, parte en backend)

1. **Backend:** añadir `trackCount` y `durationSeconds` a la respuesta de playlist.
2. **Frontend:**
   - `playlists/+page.server.ts` deja de pedir hasta 500 pistas por playlist.
   - `user/[id]/+page.server.ts` deja de hacer una petición por playlist.
   - La destacada de inicio recupera la duración real.
3. **Explorar:** cachear `/genres` en `$lib/server/genres.ts` (como `playbackConfig`, con TTL de unos minutos) y lanzar todas las peticiones en paralelo en vez de esperar primero a los géneros.
4. **Añadir álbum a playlist** (`playlistActions.ts`): hoy hace un POST por pista en serie y se corta en 200. Pedir un endpoint de inserción múltiple 🔗; mientras tanto, paralelizar con un límite de concurrencia.

**Cierre:** la página de playlists hace una sola petición a la API; el perfil hace dos.

---

## P9 · Separación de responsabilidades en `lib/` (L)

1. **Dividir `player.svelte.ts`:**
   - `player/queue.ts`: operaciones de cola (de P0).
   - `player/stream.ts`: `resolveStream(id)` (fetch de `/stream`, ticket, `listenId`) y `readErrorMessage`.
   - `player/mediaSession.ts`: registro de acciones y sincronización de metadatos y estado.
   - `player/progressClock.ts`: el bucle `requestAnimationFrame` de progreso suave.
   - `player/actions.ts`: `playAllOrToggle`, `playShuffled`, `isQueueCurrent`.
   - `player/player.svelte.ts`: solo el estado y la orquestación.
2. **Acento como número.**
   - Nuevo `theme/accent.svelte.ts`: caché, `extractAccent` y el tono actual como `number`.
   - Ahí mismo vive el fundido de tono que hoy está en `+layout.svelte` (`animateThemeHue()`).
   - Se elimina `parseHue` (ya no hay que leer el tono de una cadena `oklch(...)`).
3. **Dividir `state/navigation.svelte.ts`:**
   - `state/history.svelte.ts`: `trackNavigation`, `previousPage`.
   - `state/scroll.ts`: `restoreScroll`.
   - `utils/hrefs.ts`: `searchHref`, `genreHref` y las URLs de portada (`trackCover(id, size)`, `playlistCover(playlist, size)` con el `v=updatedAt`, `albumCover(id, size)`), que hoy se escriben a mano en más de 15 sitios.
   - `components/layout/navLinks.ts`: `appNavLinks` e `isNavActive` (sustituye el `isActive` duplicado de Sidebar y TabsBar).
4. **Menús.** `contextMenuPosition` y `MenuPosition` pasan a `utils/menuPosition.ts`, sin `preventDefault()` dentro (lo hace quien llama). `state/menus.svelte.ts` deja de importar de un componente.
5. **Movimientos de archivos:**
   - `data/collections.ts` → `utils/collections.ts`.
   - `player/queuePanel.svelte.ts` y `state/playlists.svelte.ts` → `state/panels.svelte.ts` (`queuePanel`, `createPlaylistModal`).
   - `data/search.ts`: claves internas (`tracks`, `albums`, `playlists`, `users`) y etiquetas aparte en un mapa de textos.
6. Tipar `+layout.svelte` con `interface Props`.

**Cierre:** `player.svelte.ts` por debajo de ~250 líneas; ningún archivo de `state/` importa de `components/`.

---

## P10 · Componentes extraídos y páginas más finas (L)

1. **Menú contextual.**
   - `state/menu.svelte.ts` con `createMenu<T>()` genérico: sustituye `createTrackMenu` y el `albumMenu` hecho a mano de explorar.
   - Componente `TrackContextMenu` que encapsula el bloque de 15 líneas repetido en inicio, explorar y mezcla. `AlbumContextMenu` para explorar.
2. **`PlayAllButton {items}`** con el "Reproducir/Pausar" de álbum, playlist, mezcla, me gusta y la portada de inicio.
3. **`createPagedList(fetcher)`** en `state/pagedList.svelte.ts` para la paginación infinita de explorar y de seguidores (estado, `appendUnique`, `hasMore` y un estado de error visible en vez de tragarse el fallo).
4. **Primitivas.**
   - `Button`, `IconButton`, `Chip` y `MenuItem` usan `<svelte:element this={href ? 'a' : 'button'}>` en vez de duplicar las dos ramas.
   - Acción `use:onEscape` para `Modal`, `ContextMenu` y `AccountMenu`.
   - `AccountMenu` incluye su propio disparador con el `Avatar`; `TopBar` y `MobileHeader` dejan de duplicarlo.
   - `Artwork` y `Avatar` usan `{#key src}` en vez del `$effect` que reinicia `failed`.
5. **Páginas.**
   - Quitar los alias `const x = $derived(data.x)` y las funciones de una línea (`playSpotlight`, `shuffleSpotlight`, `openContextMenu`, `stepClass`…).
   - Sustituir los `$effect` que copian `data` a estado local por `$derived` escribible cuando sea posible.
   - Navegación con enlaces reales: `ListRow href` en vez de `onclick={() => goto(...)}`, y la destacada de inicio como `<a>`.
   - `upload`: el indicador de pasos solo refleja estados reales (o se simplifica a "subiendo / listo").

**Cierre:** ninguna página repite el bloque del menú ni el botón de reproducir; las tarjetas se pueden abrir con clic central.

---

## P11 · Consistencia (M, mecánico)

1. **Imports:** siempre `$lib/...`, nunca `../`. Se hace cumplir en P12.
2. **Convenciones de nombres** (se documentan en `CLAUDE.md`):
   - Mapas de variantes en constantes de módulo en mayúsculas (`SIZE`, `VARIANT`).
   - Errores: `error` al desestructurar y `err` en `catch`.
   - Manejadores de reproducción: `playFrom(index)`, `playAll()`.
   - Acciones: `create`, `edit`, `delete`, `addTrack`, `removeTrack`.
3. **Glosario de interfaz:** "Playlist" (nunca "Lista"), "Mezcla" (nunca "Mix"), "Modo claro". Duraciones totales siempre con `fmtDurationLong`.
4. **Tokens.**
   - Iconos siempre con `size-icon-*`; añadir `size-icon-2xs` si hace falta para los de 14 px.
   - Nada de `size={16}` en iconos Lucide.
   - No usar tokens de icono como espaciado (`pb-icon-md`, `gap-icon-md`).
   - Anchos siempre con clase (`w-(--mf-sidebar-w)`), no con `style`.
   - Nueva utilidad `focus-ring` usada por `Button`, `IconButton`, `Chip` y los enlaces interactivos.
5. **Rutas:** mover `routes/+page.svelte` y `+page.server.ts` (inicio) dentro de `(app)`, para que el grupo tenga sentido: todo lo de la app dentro y `auth/` fuera.

**Cierre:** `npm run lint` en verde con las reglas nuevas de P12.

---

## P12 · Guardarraíles y documentación (S)

1. **ESLint:**
   - `no-restricted-imports` que prohíba `../*`.
   - `@typescript-eslint/no-non-null-assertion`.
   - `svelte/no-unused-props`.
2. **`scripts/check-tokens.mjs`:** fallar con `size={N}` en iconos y con `size-N` sobre componentes Lucide.
3. **Opcional:** `knip` para detectar exportaciones y archivos sin uso.
4. **Documentación:**
   - `CLAUDE.md`: nueva estructura de carpetas, helpers de servidor (`apiFor`, `authedAction`, mappers), convenciones de nombres y glosario.
   - `docs/paginas.md`: está desactualizado (menciona el grupo `(marketing)` y la landing, que ya no existen).

**Cierre:** las reglas nuevas fallan si se reintroduce cualquiera de los patrones eliminados.

---

## Registro

| Parte                       | Estado | PR / notas                                                                                                             |
| --------------------------- | ------ | ---------------------------------------------------------------------------------------------------------------------- |
| P0 Red de seguridad         | ✅     | Vitest + 37 tests; `player/queue.ts` extraído. `lint` ahora ejecuta `test`                                             |
| P1 Seguridad                | ✅     | Hecho en la rama `claude/frontend-audit-plan-4f9432`                                                                   |
| P2 Reproductor y cola       | ✅     | Sin probar en navegador (API caída)                                                                                    |
| P3 Bugs de páginas          | ✅     | Backend: `ownerUserId` en playlist. Sin probar en navegador                                                            |
| P4 Código muerto            | ✅     | Se mantiene el redirect a `/explore` de inicio (cubre escucha anónima)                                                 |
| P5 Modelo Track único       | ✅     | `Paged<T>`, `Mix`, `PlaylistSummary` añadidos. Sin probar en navegador                                                 |
| P6 Capa de servidor         | ✅     | `failOnError` + `requireData`; `parseAlbumForm` usa `formString`                                                       |
| P7 Formularios y validación | ✅     | Límites según las columnas de BD del backend (playlist 50/256, pista 200)                                              |
| P8 Rendimiento 🔗           | ✅     | Backend: `trackCount` y `durationSeconds` en playlist. Añadir álbum usa `POST /playlists/{id}/albums/{albumId}` (bulk) |
| P9 Responsabilidades        | ✅     | `player.svelte.ts` queda en ~320 líneas                                                                                |
| P10 Componentes y páginas   | ✅     | Quedan algunos alias `$derived(data.x)`; `{#key}` sustituido por `failedSrc`                                           |
| P11 Consistencia            | ✅     |                                                                                                                        |
| P12 Guardarraíles y docs    | ✅     | `knip` no añadido (opcional)                                                                                           |

Leyenda: ⬜ pendiente · 🟡 en curso · ✅ hecho.
