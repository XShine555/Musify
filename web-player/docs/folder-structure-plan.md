# Plan — Organización de carpetas (web-player)

> Plan de trabajo para agentes/IA. Cubre la **estructura de directorios**, no el sistema de
> diseño (para tokens/componentización visual ver `docs/frontend-refactor-plan.md`, P0–P14,
> que sigue siendo la referencia para radios/tamaños/tipografía/colores).
> Basado en una lectura completa de `web-player/src` (2026-09-22, rama `web-player-ref`).

## Cómo usar este documento

1. Cada fase (F1…F7) es independiente y es **un commit**. No mezcles fases.
2. Al mover archivos: actualizar todos los imports que los referencian (`grep` del nombre
   viejo de ruta antes de dar la fase por cerrada).
3. Cierre de cada fase: `npm run check` y `npm run lint` **limpios** (desde `web-player/`).
4. Al terminar una fase, marca su casilla en la sección Registro.

Orden recomendado: `F1 → F2 → F3 → F4 → F6` (independientes entre sí, pueden ir en
cualquier orden). `F5` es opcional y requiere confirmación explícita del usuario antes de
empezarla (cambia URLs públicas / config de Zitadel).

---

## Diagnóstico

- `src/lib/` raíz tiene 12 archivos sueltos mezclando estado reactivo (runes), helpers de
  dominio y utilidades genéricas.
- `src/lib/components/` raíz tiene 4 componentes de layout/chrome sueltos (ni `ui/` ni
  `player/`).
- `src/lib/components/ui/` tiene 38 componentes en un único nivel, sin categorizar.
- `src/routes/` reparte auth en cuatro sitios distintos (`/login`, `/logout`, `/auth/login`,
  `/auth/callback`).

---

## F1 — Agrupar estado reactivo

Crear `src/lib/state/` y mover:

- `menus.svelte.ts`
- `navigation.svelte.ts`
- `playlists.svelte.ts`
- `toggle.svelte.ts`

`lib/player/*.svelte.ts` y `lib/theme/mode.svelte.ts` se quedan donde están (ya tienen
dominio propio, no son huérfanos).

**Cierre:** imports actualizados, `npm run check` y `npm run lint` limpios.

## F2 — Agrupar helpers de dominio

Crear `src/lib/data/` y mover:

- `albums.ts`
- `collections.ts`
- `genres.ts`
- `mixes.ts`
- `recentlyPlayed.ts`
- `search.ts`

**Cierre:** igual que F1.

## F3 — Utilidades genéricas

- `format.ts` → `src/lib/utils/format.ts`.
- `types.ts` se queda en la raíz de `lib/` (si crece mucho, pasar a `src/lib/types/index.ts`
  en una fase futura — no crear la carpeta por adelantado).
- `config.ts` se queda en la raíz de `lib/` (config global, un único archivo, no amerita
  carpeta).

**Cierre:** igual que F1.

## F4 — Componentes de layout/chrome

Crear `src/lib/components/layout/` y mover:

- `MobileHeader.svelte`
- `Sidebar.svelte`
- `TabsBar.svelte`
- `TopBar.svelte`

**Cierre:** igual que F1.

## F5 (opcional, requiere confirmación) — Consolidar rutas de auth

Unificar `/login`, `/logout`, `/auth/login`, `/auth/callback` bajo `/auth/`:

- `/auth` → page (login screen, hoy en `/login/+page.svelte`)
- `/auth/login` → server endpoint (ya existe)
- `/auth/logout` → server endpoint (hoy `/logout/+server.ts`)
- `/auth/callback` → server endpoint (ya existe)

**Cambia URLs públicas.** Si `AUTH_REDIRECT_URI` / `AUTH_POST_LOGOUT_URI` en la config de
Zitadel apuntan a las rutas actuales, hay que actualizarlas ahí también antes de mergear.
No empezar esta fase sin confirmación explícita.

## F6 — Categorizar `components/ui/`

38 componentes en un único nivel dificultan el escaneo visual y el autocompletado del
editor. Agrupar por categoría funcional (no por jerarquía atómica estricta — sería excesivo
para este tamaño):

| Carpeta        | Componentes                                                                                                          | Criterio                                              |
| -------------- | --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ |
| `primitives/`  | Alert, BackLink, Button, Checkbox, Chip, EmptyState, Field, IconButton, InfiniteScroll, Input, Logo, SegmentedControl, Slider, Surface, Textarea | Piezas genéricas, sin conocimiento del dominio música |
| `overlay/`     | AccountMenu, ConfirmDialog, ContextMenu, GlassMenu, MenuItem, Modal                                                    | Todo lo que flota encima del contenido                |
| `media/`       | ArtistLink, Artwork, Avatar, EqBars, ExplicitBadge, ListRow, MediaCard, MediaIdentity, PlayButton, TrackList           | Específicos del dominio música/track                  |
| `forms/`       | AlbumForm, CoverForm, ImageDropzone, PlaylistForm                                                                      | Formularios compuestos de entidad                     |
| `layout/`      | Page, PageHeader, SectionHeading                                                                                       | Andamiaje de página (distinto de `components/layout/` de F4, que es el chrome global) |

Sin barrels/`index.ts` — actualizar cada ruta de import directamente, no hace falta esa
indirección para esto.

Idealmente un sub-commit por categoría, pero en la práctica hay imports relativos
(`./X.svelte`) que cruzan categorías (p. ej. `AlbumForm` → `CoverForm`+`Field`+`Input`, que
caen en `forms/`+`primitives/`), así que mover una categoría a la vez deja `npm run check`
roto a mitad de camino. Se hizo en **un único commit** que mueve las 38 a la vez y reescribe
todos los imports (internos y externos) — sigue siendo revisable porque son solo
renames + cambios de ruta de import, sin lógica nueva.

**Cierre:** las 5 subcarpetas creadas, `ui/` raíz sin `.svelte` sueltos, imports
actualizados, `npm run check` y `npm run lint` limpios.

---

## Registro

- [x] F1 — estado reactivo → `lib/state/`
- [x] F2 — helpers de dominio → `lib/data/`
- [x] F3 — utilidades genéricas (`format.ts` → `lib/utils/`)
- [x] F4 — componentes de layout/chrome → `components/layout/`
- [ ] F5 — consolidar rutas de auth bajo `/auth/` (opcional, pendiente de confirmación)
- [x] F6 — categorizar `components/ui/` (primitives/overlay/media/forms/layout)
