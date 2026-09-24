# Plan de estructura del frontend

Plan derivado de la revisión de estructura del `web-player` (2026-09-24), hecha después de cerrar `frontend-audit-plan.md` (P0–P12). Cada parte es un PR independiente que deja `npm run check` y `npm run lint` en verde y no cambia el comportamiento visible. Antes de empezar una parte, mira el **Registro** del final.

Leyenda de tamaño: **S** (< 1 h) · **M** (medio día) · **L** (1 día o más).

## Orden y dependencias

```
E1 Layout del grupo (app)
 └─ E2 Carpeta shell
     └─ E3 Frontera de ui/ ─ E6 Explorar en componentes

E4 Ubicación de estados   (independiente)
E5 Capa de servidor       (independiente)
E7 Acento fuera del reproductor (después de E1)

E9 Tests (antes o junto con E1 y E5)

E8 Documentación (al final)
```

E1 → E2 → E3 van en orden porque tocan los mismos imports. E4 y E5 se pueden hacer en cualquier momento. Conviene hacer E9 primero, o al menos sus puntos 1 y 2, para que E1 y E5 muevan el código de sesión con tests que detecten si algo se rompe.

---

## E1 · Layout del grupo `(app)` (M)

Problema: el grupo `(app)` no tiene `+layout`. El shell y las cargas de la app están en la raíz, que distingue la pantalla de login a mano (`isAuthPage` en `+layout.svelte`, `PUBLIC_PATHS` en `+layout.server.ts`). Por eso `/auth` también consulta `playbackConfig`, y `+error.svelte` se pinta fuera del shell (un 404 dentro de la app pierde la barra lateral y el reproductor).

1. **`routes/+layout.svelte`** se queda con lo global: fuentes, `theme.css`, `layout.css`, favicon, el `<style>` inicial del tema, `animateThemeHue` y `<DialogHost />`. Renderiza `children` sin envoltorio.
2. **`routes/+layout.server.ts`** desaparece (o queda vacío). Si alguna ruta fuera de `(app)` necesita `user`, lo lee de `locals` en su propio `load`.
3. **Nuevo `routes/(app)/+layout.server.ts`** con todo lo que hoy hay en la raíz: `getAllowAnonymousListening`, el guard (`loginRedirect` si no hay usuario ni escucha anónima), `accountUrl`, playlists del usuario, me gusta y última pista. Sin `PUBLIC_PATHS`.
4. **Nuevo `routes/(app)/+layout.svelte`** con el shell: backdrop, `Sidebar`, `MobileHeader`, `TopBar`, `<main>` con el `{#key}`, `Queue`, `PlayerDock`, el modal de crear playlist, la hidratación de `liked` y `player`, la apertura automática de la cola y el atajo de la barra espaciadora. Desaparecen `isAuthPage` y la rama `!data.user && !data.allowAnonymousListening`, porque el guard ya ha redirigido.
5. **Nuevo `routes/(app)/+error.svelte`** (el contenido del actual) para que los errores dentro de la app se vean con el shell. El `+error.svelte` de la raíz se queda como fallback mínimo para errores fuera del grupo.
6. **No quitar** los `requireUser`/`optionalUser` de las páginas ni `authedAction`. Los `load` de página se ejecutan en paralelo con los del layout, y las acciones de formulario no ejecutan el `load` del layout, así que el guard del layout no basta.

**Cierre:** `grep -rn "isAuthPage\|PUBLIC_PATHS" src` vacío; `/auth` no llama a la API; un 404 en `/albums/xxx` se ve dentro del shell; prueba manual con sesión, sin sesión y con escucha anónima.

---

## E2 · Carpeta `shell` (S)

Problema: hay dos carpetas `layout` con significados distintos (`components/layout/` es el shell de la app y `components/ui/layout/` son piezas para montar páginas). `AccountMenu` está en `ui/overlay/`, pero solo lo usan `TopBar` y `MobileHeader`.

1. Renombrar `components/layout/` a `components/shell/` (`Sidebar`, `TopBar`, `TabsBar`, `MobileHeader`, `navLinks.ts`).
2. Mover `components/ui/overlay/AccountMenu.svelte` a `components/shell/AccountMenu.svelte`.
3. Actualizar imports en `(app)/+layout.svelte` y `components/player/PlayerDock.svelte`.

**Cierre:** `grep -rn "components/layout/\|overlay/AccountMenu" src` vacío.

---

## E3 · Frontera de `components/ui/` (M)

Problema: `ui/` se presenta como el design system, pero cuatro componentes dependen del estado global de la app. `TrackList` y `PlayAllButton` importan `player`. `TrackContextMenu` y `AlbumContextMenu` importan `player`, y `AlbumContextMenu` también importa `$lib/data/albums`. El resto de `ui/media/` son componentes de presentación sin estado (reciben props), y así deben quedarse.

1. **Nueva carpeta `components/music/`** para los componentes que conectan la interfaz con el estado de la app:
   - `ui/media/TrackList.svelte` → `music/TrackList.svelte`
   - `ui/media/PlayAllButton.svelte` → `music/PlayAllButton.svelte`
   - `ui/overlay/TrackContextMenu.svelte` → `music/TrackContextMenu.svelte`
   - `ui/overlay/AlbumContextMenu.svelte` → `music/AlbumContextMenu.svelte`
2. **Regla de ESLint:** un bloque con `files: ['src/lib/components/ui/**']` y `no-restricted-imports` que prohíba `$lib/player/*`, `$lib/data/*`, `$lib/server/*`, `$lib/components/music/*`, `$lib/components/shell/*` y `$lib/components/player/*`. Hay que repetir en ese bloque el patrón `../*` del bloque general, porque la configuración de un override sustituye a la del bloque general, no se suma.
3. Comprobar que la regla pasa sin excepciones. `ui/` puede seguir importando `$lib/state/*` (historial, diálogos), `$lib/utils/*`, `$lib/actions/*`, `$lib/theme/*` y `$lib/types`.

**Cierre:** `npm run lint` en verde; si un componente de `ui/` importa `player`, el lint falla.

---

## E4 · Ubicación de estados (S)

1. `player/liked.svelte.ts` → `state/liked.svelte.ts`. Guarda los me gusta del usuario, no el estado del reproductor, y lo usan `Sidebar`, `TrackInfo`, el layout y las páginas.
2. `data/recentlyPlayed.ts` → `player/recentlyPlayed.ts`. Solo mezcla `player.recentlyPlayed` con el historial, y hoy es el único archivo de `data/` que depende del reproductor.

**Cierre:** `grep -rn "player/liked\|data/recentlyPlayed" src` vacío; `data/` no importa de `player/`.

---

## E5 · Capa de servidor (S–M)

Problema: `server/api.ts` mezcla el cliente de la API, los guards de sesión, la lectura de formularios y el envoltorio de acciones. Los parsers de `server/forms/` importan `api.ts` solo para usar `formString`/`formFile`.

1. **`server/api.ts`** se queda con el cliente y el manejo de errores: `createApiClient`, `ApiClient`, `apiFor`, `unwrapOrError`, `apiErrorDetail`, `failOnError`, `requireData`.
2. **Nuevo `server/guards.ts`:** `loginRedirect`, `requireUser`, `optionalUser` (14 imports en `routes/` más `(app)/+layout.server.ts`).
3. **Nuevo `server/forms/fields.ts`:** `formString`, `formFile`. Los parsers de `forms/` lo importan desde ahí.
4. **Nueva carpeta `server/actions/`:**
   - `authedAction.ts` con `authedAction` y `ActionContext`.
   - `followActions.ts` → `actions/follow.ts`.
   - `playlistActions.ts` → `actions/playlist.ts`.

**Cierre:** `server/forms/` no importa `server/api`; `api.ts` no importa `redirect` ni `RequestEvent` salvo para `apiFor`.

---

## E6 · Explorar en componentes (M)

Problema: `(app)/explore/+page.svelte` (393 líneas) resuelve tres vistas en una: la rejilla de géneros, la vista de un género y los resultados de búsqueda (mejor resultado y cuatro grupos).

1. Sacar componentes junto a la página (SvelteKit ignora los archivos sin `+` dentro de `routes/`):
   - `explore/GenreGrid.svelte`: la rejilla de géneros y el estado vacío.
   - `explore/TopResult.svelte`: el snippet `topResultBody` y `playTopResult`.
   - `explore/SearchResults.svelte`: los cuatro grupos, `capped`, `showGroup` y el `InfiniteScroll` de pistas.
2. `+page.svelte` queda como selector de vista: cabecera, chips, mensaje de "sin resultados" y los dos menús contextuales.
3. El estado compartido (`createPagedList`, `trackMenu`, `albumMenu`) se crea en la página y se pasa por props; los componentes no leen `page.data`.

**Cierre:** `explore/+page.svelte` por debajo de ~150 líneas; ninguna vista nueva supera las ~150.

---

## E7 · Acento fuera del reproductor (S)

Problema: `player.svelte.ts` importa `theme/accent.svelte` y en `#announce` llama a `accent.follow(...)`, así que el estado del reproductor depende del tema visual.

1. Quitar `accent` y `trackCover` de `player.svelte.ts`; `#announce` solo actualiza `mediaSession`.
2. En `(app)/+layout.svelte`, un `$effect` que siga a `player.currentId` y llame a `accent.follow(trackCover(id, 'small'), () => player.currentId === id)`.
3. Comprobar que la hidratación de la última pista también actualiza el acento (el efecto depende de `currentId`, así que debería).

**Cierre:** `player/` no importa de `theme/`; el color cambia al cambiar de canción y al cargar la página con una última pista.

---

## E8 · Documentación y restos (S)

1. **`CLAUDE.md`:**
   - Estructura de carpetas con `components/shell/`, `components/music/`, la regla de frontera de `ui/`, `state/liked.svelte.ts`, `server/guards.ts`, `server/forms/fields.ts` y `server/actions/`.
   - Documentar lo que falta: `lib/actions/` (`clickOutside`, `onEscape`, `pressable`), `state/toggle.svelte.ts` y `theme/color.ts`.
   - Corregir la referencia a `$lib/state/navigation.svelte.ts` en "App shell" (ahora es `restoreScroll` en `$lib/state/scroll.ts`) y describir el shell en `(app)/+layout.svelte`.
   - La skill `musify-web` que cita la introducción no está en `web-player/.claude/skills`: añadirla o quitar la referencia.
2. **Conversiones fuera de los mappers.** La regla de P5 dice que los `Number(...)` sobre DTOs solo se hacen en `server/mappers.ts`, pero quedan varios en `routes/`:
   - `(app)/+page.server.ts`: las estadísticas de escucha (nuevo `toListeningStats`, con test) y `totalItemCount` de las playlists.
   - `(app)/explore/+page.server.ts`: `albumsTotal` y `usersTotal`.
   - `(app)/user/[id]/+page.server.ts`: `followersCount`.
   - La carga de playlists del layout (que tras E1 está en `(app)/+layout.server.ts`): `totalItemCount`.

   Los totales pueden salir de `toPage(...).totalItemCount` o de un helper `toCount` en los mappers.

3. **Planes:** marcar `frontend-audit-plan.md` como cerrado (o archivarlo) y dejar este como el plan activo.

**Cierre:** cada ruta citada en `CLAUDE.md` existe; `grep -rn "Number(" src/routes` solo devuelve lecturas de parámetros o formularios.

---

## E9 · Tests de los arreglos y del código con efectos (M)

Los tests se quedan junto al archivo que prueban (`x.ts` + `x.test.ts`); no se crea una carpeta `tests/`. Hoy cubren bien la lógica pura (formatos, mappers, parsers, cola, búsqueda), pero no el código con fetch, estado o tiempo, ni los arreglos de seguridad de P1 y P2, que pueden volver sin que nada falle.

1. **Sesión y guards.**
   - `server/auth.ts`: `encodeSession` → `decodeSession` devuelve la misma sesión; un token manipulado, cifrado con otra clave o vacío devuelve `null`. Para el secreto, usar `vi.mock('$env/dynamic/private')` con un `SESSION_SECRET` de prueba, no el `.env` local (en CI no existe).
   - Guards (`requireUser`, `optionalUser`, `loginRedirect`; en `server/guards.ts` tras E5): redirigen a `/auth?returnTo=…` con la ruta y la query codificadas, y dejan pasar cuando toca.
   - Tras E1, el `load` de `(app)/+layout.server.ts`: redirige sin sesión ni escucha anónima, devuelve datos vacíos en modo anónimo y no llama a la API sin usuario.
2. **Endpoints `/api` (arreglos de P1).** Llamar al `RequestHandler` con un evento falso y un `fetch` simulado:
   - `api/likes`: 401 sin token, 400 con `trackId` ausente, vacío o no string, 502 si la API falla.
   - `api/listens/[id]/progress`: 400 con JSON inválido, con `playedSeconds` negativo, no finito o no numérico; 404 si la API devuelve 404.
   - `api/tracks`: `pageSize` se limita a `MAX_PAGE_SIZE` y `pageNumber` inválido pasa a 1.
3. **Rollback de "Me gusta" (`liked.svelte.ts`, arreglo de P2).** Con `fetch` simulado que falla: se revierte solo esa pista y se conservan los cambios hechos mientras tanto en otras. También los casos de dar y quitar el me gusta, y que `hydrate` solo se aplica una vez.
4. **`ListenTracker`.** Con `vi.useFakeTimers()` y `fetch` simulado:
   - Suma solo los avances de hasta 2,5 s; un salto mayor (seek) o hacia atrás no cuenta.
   - `interrupt()` hace que el siguiente `tick` no sume.
   - Envía a los 30 s y en `flush()`; no reenvía si no hay progreso nuevo.
   - Si el envío falla, el siguiente `flush()` vuelve a enviar el total.
   - `begin()` envía lo pendiente de la escucha anterior antes de empezar la nueva.
5. **`createPagedList`.** Añade la página siguiente sin duplicados, actualiza `hasMore`, ignora un `loadMore` con otro en curso y, si falla, marca `error` y conserva los elementos. `reset` limpia el error.
6. **`apiErrorDetail`.** Mensajes de `errors` (varios campos, arrays), después `detail`, después `title`, y `undefined` para valores que no son objeto o sin texto.

Fuera de alcance, a propósito: tests de componentes (sobre todo presentación, ya vigilada por `check-tokens`), utilidades triviales (`storage`, `menuPosition`, `navLinks`), `mediaSession`, `progressClock`, `palette` (necesita canvas) y `player.svelte.ts` entero (depende de `<audio>`; su lógica está en `queue.ts`, que ya tiene tests). E2E con Playwright queda para cuando CI pueda levantar el backend y Zitadel.

**Cierre:** cada punto anterior tiene su `*.test.ts` junto al módulo; `npm run test` en verde sin `.env` local; volver a introducir cualquiera de los bugs de P1/P2 cubiertos hace fallar un test.

---

## Registro

| Parte                           | Estado | PR / notas                               |
| ------------------------------- | ------ | ---------------------------------------- |
| E1 Layout del grupo (app)       | ✅     |                                          |
| E2 Carpeta shell                | ✅     |                                          |
| E3 Frontera de ui/              | ✅     | Regla de ESLint sobre `components/ui/**` |
| E4 Ubicación de estados         | ✅     |                                          |
| E5 Capa de servidor             | ✅     |                                          |
| E6 Explorar en componentes      | ✅     | `+page.svelte` queda en ~180 líneas      |
| E7 Acento fuera del reproductor | ✅     |                                          |
| E8 Documentación y restos       | ✅     |                                          |
| E9 Tests                        | ✅     | 121 tests, sin `.env` local              |

Leyenda: ⬜ pendiente · 🟡 en curso · ✅ hecho.
