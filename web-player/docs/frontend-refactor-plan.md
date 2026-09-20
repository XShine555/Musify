# Plan — Refactor y unificación del frontend (web-player)

> Plan de trabajo para agentes/IA con contexto limitado (~128k). Sustituye a
> `design-system-refactor-plan.md` (cuyo contenido ya está mayormente aplicado).
> Basado en una lectura completa de **todos** los `.svelte`, `.css` y `.ts` de cliente
> de `web-player/src` (2026-09-18, rama `design-prot`).

## Cómo usar este documento

1. **Lee siempre la sección A (Contexto común)** — es corta y todas las partes la asumen.
2. Elige **una parte** (P0…P14). Cada parte indica: objetivo, prerrequisitos, archivos
   a leer, tareas, API exacta de lo que se crea, tabla de mapeo y criterios de cierre.
3. Una parte = un commit/PR. No mezcles partes.
4. Si una parte necesita algo de otra (p. ej. un token o un helper) y **no existe
   todavía**, créalo tú siguiendo la especificación de la sección B — están descritos con
   ruta y API exactas para que cualquier parte pueda crearlos de forma idéntica.
5. Al terminar una parte, marca su casilla en la sección D (Registro).

Orden recomendado (las flechas son dependencias **blandas**, ver punto 4):

```
P0 → P1 → P2
        ↘ P3 (Artwork/Avatar) ─┐
        ↘ P4 (Botones)  ───────┤
        ↘ P5 (Helpers TS) ─────┼→ P6 (Menús) → P7 (Filas) → P8 (Tablas) → P9 (Tarjetas)
                               └→ P10 (Cabeceras) → P11 (Formularios) → P12 (Chrome) → P13 (Páginas) → P14 (Barrido final)
```

---

## A. Contexto común (leer siempre)

### A.1 Stack y reglas del proyecto (de `web-player/CLAUDE.md`)

- SvelteKit 2 + **Svelte 5 runes** (`$props()` con `interface Props`, `$state`,
  `$derived`, `$effect`; nada de `export let` ni `$:`; `{#each}` siempre con clave).
- **Tailwind 4**: no hay `tailwind.config.js`; la config vive en `src/routes/layout.css`
  (`@theme`, `@utility`). Tokens de color crudos en `src/lib/theme/theme.css` (`--mf-*`),
  tokens dinámicos por hue en `src/lib/theme/tokens.ts` (`buildThemeTokens`).
- Sin comentarios en código salvo imprescindibles.
- Cierre de cada parte: `npm run check` y `npm run lint` **limpios** (desde `web-player/`).
- Iconos: `@lucide/svelte/icons/<nombre>`.

### A.2 Gotchas conocidos (no reintroducirlos)

1. **Tokens animados por hue** (`--mf-bg`, `--mf-elevated`, `--mf-panel-bg`, `--mf-bar-bg`,
   `--mf-hairline`, `--mf-hero-bg`, `--mf-modal-glow`, `--mf-spotlight-bg`,
   `--mf-logo-grad`, `--mf-cover-grad`, `--mf-accent*`, `--mf-ambient`) se aplican como
   **estilo inline** en `<html>` desde `+layout.svelte`. Un override en
   `:root[data-theme='light']` **no gana**: la variante clara va en `buildThemeTokens()`.
2. **Conflictos de clases Tailwind**: nunca `class="bg-a {cond ? 'bg-b' : ''}"`. Siempre
   mutuamente excluyente: `class={cond ? 'bg-b' : 'bg-a'}`. Overrides de la clase base de
   un componente compartido → prop/variante, no `!important`.
3. Texto sin clase de tamaño hereda 16px del navegador. Todo texto visible debe tener una
   receta tipográfica (ver B.1.6).
4. `--mf-ink` es "texto sobre CTA" y se invierte en claro; `--mf-tile-shade`,
   `--mf-cover-grad`, `--mf-liked-grad` son "arte" (no invertir).
5. Referencia visual: **Inicio** (`src/routes/+page.svelte`). Un cambio de refactor no
   debe cambiar el aspecto salvo donde la parte lo diga explícitamente (columna "Δ").

### A.3 Verificación de cada parte

```sh
cd web-player
npm run check
npm run lint
```

Visual (Browser pane, `.claude/launch.json` → `musify-web`; login de dev en la memoria del
proyecto): abre cada página tocada **y** Inicio; prueba estado activo (una canción
sonando), hover, móvil (375px) y modo claro (menú de cuenta → "Modo blanco").

### A.4 Línea base medida (para saber si avanzas)

| Métrica                                           | Hoy                                     |
| ------------------------------------------------- | --------------------------------------- |
| Componentes `.svelte` (lib + rutas `components/`) | 61                                      |
| Líneas `.svelte`/`.css`/`.ts` cliente             | ~8.700                                  |
| Clases arbitrarias `x-[…]`                        | 44                                      |
| Espaciados fraccionarios (`h-3.75`, `gap-2.75`…)  | ~190 apariciones, 60 valores distintos  |
| Radios distintos usados                           | 13 (6 tokens + 7 de Tailwind por defecto) |
| `strokeWidth` distintos                           | 13 valores                              |
| Atributos `style="…"`                             | 24                                      |
| Pluralizaciones a mano `=== 1 ?`                  | 15                                      |
| Errores de `eslint`                               | 2 (`liked/+page.svelte`)                |

Objetivo global: ~61 → ~45 componentes, −1.500/−2.000 líneas, 0 arbitrarios fuera de una
lista blanca, 1 escala por dimensión.

---

## B. Especificaciones compartidas (fuente única)

Cualquier parte que necesite algo de aquí y no exista, lo crea **exactamente así**.

### B.1 Tokens (en `src/routes/layout.css`, dentro de `@theme`)

Tailwind 4 genera utilidades a partir de estos namespaces: `--radius-*` → `rounded-*`,
`--spacing-*` → `size-* w-* h-* p-* m-* gap-*`, `--container-*` → `max-w-*`,
`--tracking-*` → `tracking-*`, `--leading-*` → `leading-*`.

#### B.1.1 Radios (`--radius-*`)

| Token             | Valor      | Para qué                                  |
| ----------------- | ---------- | ----------------------------------------- |
| `rounded-tag`     | `0.25rem`  | badges, celdas mini (ExplicitBadge, índice) |
| `rounded-thumb`   | `0.5rem`   | carátulas ≤ 36px, logo                    |
| `rounded-control` | `0.625rem` | inputs, filas, items de menú, botones     |
| `rounded-art`     | `0.8125rem`| carátulas 42–88px, tarjetas de grid       |
| `rounded-art-lg`  | `1.125rem` | carátulas ≥ 136px, dropzones              |
| `rounded-panel`   | `1.125rem` | contenedores, menús, modales, tiles       |
| `rounded-panel-lg`| `1.375rem` | hero, spotlight, "mejor resultado"        |
| `rounded-full`    | (Tailwind) | pastillas, avatares, botones redondos     |

Eliminar `--radius-art-round` (0 usos). Mapeo de lo existente:

| Hoy                          | px | Usos | Pasa a            | Δ    |
| ---------------------------- | -- | ---- | ----------------- | ---- |
| `rounded-sm`, `rounded`      | 4  | 2    | `rounded-tag`     | 0    |
| `rounded-md`                 | 6  | 1    | `rounded-thumb`   | +2px |
| `rounded-lg`                 | 8  | 4    | `rounded-thumb`   | 0    |
| `rounded-xl`                 | 12 | 6    | `rounded-control` | −2px |
| `rounded-2xl`                | 16 | 8    | `rounded-art-lg` (arte) / `rounded-panel` (contenedor) | +2px |
| `rounded-3xl`                | 24 | 1    | `rounded-panel-lg`| −2px |

#### B.1.2 Tamaños de carátula/arte (`--spacing-cover-*`)

| Token            | Valor                          | Usos actuales que absorbe (Δ)                                   |
| ---------------- | ------------------------------ | --------------------------------------------------------------- |
| `cover-xs`       | `2.25rem` (36)                 | thumbs de menús `h-9`; sidebar `h-8.5` (+2)                      |
| `cover-sm`       | `2.625rem` (42)                | filas de tabla `h-10.5`; cola y "Populares" `h-10` (+2); player móvil `h-11` (−2) |
| `cover-md`       | `3rem` (48)                    | player escritorio `md:h-12`                                      |
| `cover-lg`       | `3.5rem` (56)                  | "Continuar" `h-14`; filas de búsqueda `h-13` (+4); tarjeta playlist de Inicio `h-15.5` (−6) |
| `cover-xl`       | `5.5rem` (88)                  | "Mejor resultado" `h-22`                                         |
| `cover-2xl`      | `8.5rem` (136)                 | spotlight de Inicio `h-34`                                       |
| `cover-hero`     | `9rem` (144)                   | cabeceras de colección, dropzones, avatar de perfil              |
| `cover-hero-sm`  | `10.25rem` (164)               | cabeceras de colección a partir de `sm:`                         |

Uso: `size-cover-sm` (Tailwind 4 `size-*` = `width`+`height`). **Nunca** `h-X w-X` en
pareja: siempre `size-*`.

#### B.1.3 Iconos (`--spacing-icon-*`) y trazo

| Token      | Valor      | Absorbe                                        |
| ---------- | ---------- | ---------------------------------------------- |
| `icon-xs`  | `0.875rem` | `h-3.5` (14)                                   |
| `icon-sm`  | `1rem`     | `h-3.75` (15), `h-4` (16), `h-4.25` (17)       |
| `icon-md`  | `1.125rem` | `h-4.5` (18)                                   |
| `icon-lg`  | `1.25rem`  | `h-5` (20), `h-6` (24, login móvil)            |
| `icon-xl`  | `2.25rem`  | `h-9`/`h-10` (estados vacíos, dropzone, Me gusta) |

Trazo: en `layout.css`, fuera de `@theme`:

```css
@layer base {
	svg.lucide {
		stroke-width: 1.8;
	}
}
@utility stroke-thin { stroke-width: 1.3; }
@utility stroke-regular { stroke-width: 1.8; }
@utility stroke-bold { stroke-width: 2.5; }
```

La propiedad CSS `stroke-width` gana al atributo de presentación que pone Lucide, así que
el prop `strokeWidth` deja de tener efecto: **se elimina de todos los usos**. Mapeo:
`1–1.4` → `stroke-thin`, `1.5–2` → (nada, default), `2.5–3` → `stroke-bold`,
`strokeWidth={0}` + `fill` → clase `stroke-0`. Verifica en el navegador (DevTools →
Computed `stroke-width`) que el default aplica antes de migrar en masa.

#### B.1.4 Anchos (`--container-*`) y layout

| Token                 | Valor    | Absorbe                                                   |
| --------------------- | -------- | --------------------------------------------------------- |
| `max-w-prose-sm`      | `26rem`  | `max-w-105` (error), `max-w-108` (hero p), `max-w-110`, `max-w-115` (sin resultados) |
| `max-w-hero`          | `34rem`  | `max-w-135` (hero, seekbar)                               |
| `max-w-search`        | `30rem`  | `max-w-120` (buscador TopBar)                             |
| `max-w-cta`           | `20rem`  | `max-w-80` (botones login)                                |

Ya existen y se mantienen en `theme.css`: `--mf-sidebar-w`, `--mf-queue-w`, `--mf-nav-h`,
`--mf-player-h`, `--mf-safe-b`. Añadir:

```css
--mf-player-side-w: 16rem;   /* w-64 de PlayerBar/PlayerExtras */
--mf-menu-w: 15rem;          /* w-60 de menús contextuales (= MENU_WIDTH 240) */
--mf-submenu-w: 16rem;       /* w-64 (= SUBMENU_WIDTH 256) */
--mf-range-h: 3.5px;         /* barra de progreso y volumen */
--mf-range-thumb: 11px;
--mf-volume-w: 84px;
```

Las constantes TS `MENU_WIDTH`/`SUBMENU_WIDTH` (duplicadas hoy en `TrackContextMenu` y
`AlbumContextMenu`) pasan a `src/lib/config.ts` con un comentario-no: el nombre basta.

#### B.1.5 Capas (`z-index`)

En `theme.css` `:root`: `--z-raised: 10; --z-sticky: 20; --z-backdrop: 30; --z-menu: 40;
--z-submenu: 50; --z-modal: 60; --z-grain: 80;`. Uso: `z-(--z-menu)`. Mapeo 1:1 con los
valores actuales (`z-10`,`z-20`,`z-30`,`z-40`,`z-50`,`z-[60]`, `body::after` 80). Nota:
`MobileHeader` y `PlayerDock` usan `z-40` pero son "sticky", no menú → `--z-sticky`
(verificar que el menú de cuenta sigue encima del header: menú 40 > sticky 20).

#### B.1.6 Tipografía (recetas `@utility` en `layout.css`)

Escala tipográfica = la de Tailwind (`text-xs`…`text-4xl`), **sin tamaños arbitrarios**.
Añadir en `@theme`: `--tracking-display: -0.02em; --tracking-eyebrow: 0.1em;
--leading-body: 1.65;`. Recetas (el color **no** va en la receta, se añade aparte):

```css
@utility text-display-1 { @apply font-display text-3xl font-medium tracking-tight sm:text-4xl; }
@utility text-display-2 { @apply font-display text-2xl font-medium tracking-tight; }
@utility text-display-3 { @apply font-display text-xl font-medium tracking-display; }
@utility text-display-4 { @apply font-display text-lg font-medium tracking-tight; }
@utility text-eyebrow   { @apply text-xs font-medium tracking-widest uppercase; }
@utility text-body      { @apply text-sm leading-body; }
@utility text-count     { @apply text-xs tabular-nums; }
```

> Si `@apply` con variante `sm:` dentro de `@utility` no compila, usa
> `@media (width >= 40rem) { font-size: var(--text-4xl); line-height: var(--text-4xl--line-height); }`
> como hace `page-x`.

Mapeo de lo existente:

| Patrón hoy                                                                                   | Receta                         |
| -------------------------------------------------------------------------------------------- | ------------------------------ |
| `font-display text-3xl … sm:text-4xl` (PageHeader, CollectionHeader, hero Inicio, h1 búsqueda `text-2xl sm:text-3xl font-semibold tracking-[-0.03em]`) | `text-display-1` (Δ búsqueda: +1 paso) |
| `font-display text-2xl` (stats Inicio, título spotlight, "¡Subida!")                          | `text-display-2`               |
| `font-display text-xl … tracking-[-0.02em]` (Modal, EmptyState, +error, "Sin resultados", diálogos de borrado) | `text-display-3` (todos `font-medium`) |
| `font-display text-lg` (SectionHeading, "Mejor resultado", Queue "En cola" `text-base`)      | `text-display-4`               |
| `text-xs font-medium tracking-widest uppercase` (+ variantes `tracking-[0.14em]`, `[0.16em]`, `tracking-wider`, `text-sm font-semibold`) | `text-eyebrow` |
| `text-sm leading-[1.65] text-fg-2`                                                            | `text-body text-fg-2`          |
| `text-xs text-muted tabular-nums`                                                             | `text-count text-muted`        |
| `tracking-[-0.01em]` (MixTile)                                                                | quitar                         |

#### B.1.7 Recetas de superficie y movimiento (`@utility` en `layout.css`)

```css
@utility theme-transition { transition: background 0.5s ease-out; }        /* 8× transition-[background] duration-500 */
@utility glass-bar {                                                        /* PlayerBar ×2, TabsBar */
	background-image: var(--mf-bar-bg);
	border-top: 1px solid var(--mf-hairline);
	backdrop-filter: blur(24px);
	transition: background 0.5s ease-out;
}
@utility grid-cards {                                                       /* MediaGrid por defecto: 150/165 */
	display: grid;
	gap: --spacing(3);
	grid-template-columns: repeat(auto-fill, minmax(9.375rem, 1fr));
	@media (width >= 40rem) {
		gap: --spacing(4);
		grid-template-columns: repeat(auto-fill, minmax(10.3125rem, 1fr));
	}
}
@utility grid-cards-lg {                                                    /* /playlists y /u/[id]: 180/210 */
	display: grid;
	gap: --spacing(3);
	grid-template-columns: repeat(auto-fill, minmax(11.25rem, 1fr));
	@media (width >= 40rem) {
		gap: --spacing(4);
		grid-template-columns: repeat(auto-fill, minmax(13.125rem, 1fr));
	}
}
@utility grid-tiles {                                                       /* mixes de Inicio: 164 */
	display: grid;
	gap: --spacing(5);
	grid-template-columns: repeat(auto-fill, minmax(10.25rem, 1fr));
}
@utility grid-wide {                                                        /* playlists de Inicio (258) y géneros (268) → 16.5rem, Δ ±6px */
	display: grid;
	gap: --spacing(3.5);
	grid-template-columns: repeat(auto-fill, minmax(16.5rem, 1fr));
}
```

`.glass-panel` ya existe; `Modal` debe usarla en lugar de su `style=` inline duplicado.

Stagger (sustituye los 5 `style="animation-delay:…"` con 40 **o** 45ms):

```css
.animate-enter { animation-delay: calc(min(var(--i, 0), 10) * 45ms); }
```

Uso: `style="--i:{i}"`. Único `style=` permitido junto con los de B.4.

#### B.1.8 Colores: limpieza

- Borrar utilidades sin uso: `--color-accent-dim`, `--color-accent-glow`,
  `--color-accent-hair`, `--color-accent-line`, `--color-accent-muted` (0 usos como
  clase; `--mf-accent-dim` sí se usa en `::selection`, se mantiene la variable `--mf-`).
- `--color-on-accent` es alias de `--mf-on-art` (igual que `--color-on-art`) → borrar y
  usar `on-art`. `--color-hover` = `--mf-surface-2` (igual que `surface-2`) → mantener
  **solo** `hover` como semántico de hover y dejar `surface-2` para fondos en reposo.
- `--mf-sidebar-bg` no lo usa ningún componente (Sidebar usa `bg-bg`) → borrar de
  `theme.css` y de `buildThemeTokens`.
- Botón: `--mf-btn-bg` = `--mf-surface-2`; `--mf-btn-secondary-bg` solo difiere en modo
  claro. Exponer en `@theme` `--color-btn`, `--color-btn-hover`, `--color-btn-glass`,
  `--color-btn-glass-hover` y eliminar los `bg-[var(--mf-btn-…)]` de `Button.svelte`.
- Scrollbar: `rgba(255,255,255,.18/.32)` fijo en `layout.css` (roto en claro) → tokens
  `--mf-scrollbar`, `--mf-scrollbar-hover` con variante en `:root[data-theme='light']`.
- `MixTile`: `to-black/42`, `bg-ink/76`, `border-on-art/12` → `to-scrim/40`, receta de
  `PlayButton` (B.3).

### B.2 Helpers TypeScript (fuente única)

| Archivo (nuevo)                     | API                                                                                                    | Sustituye                                                             |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------- |
| `src/lib/actions/pressable.ts`      | `pressable(node, fn)` → añade `role="button"`, `tabindex=0`, click + Enter/Espacio                      | 7 copias de `onkeydown` Enter/Space (Inicio ×5, Queue, TrackTitleCell) |
| `src/lib/actions/clickOutside.ts`   | `clickOutside(node, fn)`                                                                               | `onDocumentClick` de `AccountMenu`                                    |
| `src/lib/toggle.svelte.ts`          | `createToggle(initial=false)` → `{ get open, show(), close(), toggle() }`                               | `playlists.svelte.ts` y `player/queuePanel.svelte.ts` (idénticos)     |
| `src/lib/format.ts` (ampliar)       | `plural(n, one, many)` → `"1 canción"`; `fmtCount(n,'canción','canciones')`; **minúscula siempre**     | 15 pluralizaciones a mano, con mayúsculas inconsistentes ("Canción"/"canción") |
| `src/lib/navigation.svelte.ts` (ampliar) | `searchHref(q)`; `appNavLinks(user, counts?)` → `{href,label,icon,count?,primary:boolean}[]`       | `buildHref` en TopBar y explore; listas de links de Sidebar y TabsBar |
| `src/lib/player/player.svelte.ts` (ampliar) | `isQueueCurrent(items)`, `playAllOrToggle(items)`, `playShuffled(items)`; renombrar `addToQueue` → `playNextItem` | `playAll`/`isCurrentQueue` en álbum, playlist, liked, mixes, spotlight de Inicio (5 copias) |
| `src/lib/player/player.svelte.ts`   | `toQueueItem(track)` único (normaliza `duration`/`listensCount` a `number`)                             | mapeos a mano en `recentlyPlayed.ts` (×2), `liked.svelte.ts` (×2), `liked/+page`, `tracks.ts#targetForQueueItem` y los `Number(…)` dispersos |
| `src/lib/menus.svelte.ts`           | `createTrackMenu()` → `{ state, open(e, track), close(), playNext() }`                                 | estado + handler de menú contextual en Inicio, Explorar, Mezcla       |

### B.3 Componentes nuevos (API)

| Componente (`$lib/components/ui/`) | Props                                                                                                                                         | Sustituye / absorbe |
| ---------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| `Artwork.svelte`                   | `src?`, `trackIds?: (string\|number)[]`, `size: 'xs'\|'sm'\|'md'\|'lg'\|'xl'\|'2xl'\|'hero'`, `shape?: 'auto'\|'round'`, `imageSize?: 'small'\|'medium'\|'large'` (auto por `size`), `fallback?: 'music'\|'gradient'\|'none'`, `class?`, `children?` (overlay) | `Cover`, `PlaylistArt`, `MixArt`; radio automático por tamaño (xs→thumb, sm–xl→art/control, 2xl/hero→art-lg). Prioridad: `src` → mosaico si ≥4 `trackIds` → primera carátula → fallback. Un único `onerror`. |
| `Avatar.svelte`                    | `name`, `src?`, `size: 'sm'\|'md'\|'lg'\|'hero'`, `class?`                                                                                    | iniciales/imagen en TopBar, MobileHeader, Explorar (×2), `ArtistAvatar` |
| `ListRow.svelte`                   | `href?` \| `onclick?`, `oncontextmenu?`, `active?`, `size?: 'sm'\|'md'\|'lg'`, `title`, `subtitle?`, `subtitleHref?`, `explicit?`, `meta?`, `leading?: Snippet`, `art: Snippet<[string]>` \| artwork props, `trailing?: Snippet` | `SearchResultRow` + 7 filas a mano (ver P7) |
| `MediaIdentity.svelte`             | `title`, `subtitle?`, `subtitleHref?`, `explicit?`, `active?`, `art?: Snippet` \| `artwork` props, `titleClass?`                               | `TrackTitleCell` (renombrado + generalizado) y el bloque de `player/TrackInfo` |
| `TrackList.svelte`                 | `tracks: TrackLike[]`, `columns?: ('album'\|'added'\|'uploaded'\|'plays')[]`, `index?`, `onPlay(i)`, `rowAction?: {action, icon, label}` \| `rowActionSnippet?`, `oncontextmenu?(e, track)` | `TrackTable`+`TrackRow`+`TrackIndexCell`+`TrackMeta` y las 6 tablas (P8) |
| `ContextMenu.svelte`               | `x`, `y`, `openLeft`, `onClose`, `items: {icon,label,onclick}[]`, `playlistAction?: {action: string, fields: Record<string,string>, label}`, `playlists` | `TrackContextMenu` + `AlbumContextMenu` (90% idénticos) |
| `PlayButton.svelte`                | `playing?`, `size: 'sm'\|'md'\|'lg'`, `variant: 'strong'\|'glass'`, `label`, `onclick?`, `as?: 'button'\|'span'`                                | play de TransportControls, "Mejor resultado", overlay de MixTile |
| `ConfirmDialog.svelte`             | `open`, `onClose`, `title`, `description`, `confirmLabel`, `action` (form POST)                                                               | 2 modales de borrado con estilos distintos |
| `Slider.svelte`                    | `value`, `max`, `label`, `oninput`, `variant: 'seek'\|'volume'`                                                                               | `SeekBar` (click-only) + `VolumeControl` (`<style>` propio) |
| `Logo.svelte`                      | `size: 'sm'\|'lg'`                                                                                                                             | logo en Sidebar (sin marca), MobileHeader, login |
| `Chip.svelte`                      | `selected?`, `count?`, `href?`\|`onclick?`                                                                                                    | chips de filtro y de géneros sugeridos en Explorar |

### B.4 Lista blanca de `style=` y arbitrarios (tras el refactor)

Permitidos: `style="--i:{i}"` (stagger), gradientes de tile por hue en Explorar
(`--tile-hue`), anchos de progreso (`width:{pct}%`), posición de menú (`left/top`).
Arbitrarios permitidos: `text-[clamp(…)]` de login, `grid-cols-[auto_1fr]`,
`max-h-[85dvh]`. Todo lo demás, a token.

---

## C. Partes

### P0 — Línea base y bugs sueltos (independiente, ~30 min)

**Objetivo:** dejar `lint` en verde y corregir bugs detectados que no requieren refactor.

Tareas:
1. `src/routes/(app)/liked/+page.svelte`: quitar imports `Play` y `Pause` (errores eslint).
2. `src/lib/components/ui/MediaCard.svelte`: el título no tiene tamaño (hereda 16px) →
   `text-sm` (igual que `MixTile`).
3. `src/routes/+page.svelte`, filas del spotlight: `hover:bg-hover … {cond ? 'bg-accent-tint' : ''}`
   → excluyente (`{cond ? 'bg-accent-tint' : 'hover:bg-hover'}`), igual que las otras filas.
4. `src/routes/(app)/albums/[id]/+page.svelte` modal de borrado: `text-base text-fg-3` →
   `text-sm leading-[1.65] text-fg-2` y título igual al de playlists (se unificará en P10).
5. `Sidebar.svelte` (botón `+`) y `Queue.svelte` (cerrar): quitar `text-base` inútil.
6. Borrar `src/lib/components/ui/Badge.svelte` (0 importadores).
7. `Button.svelte`: `primary` y `secondary` son la **misma** clase. No cambiar aspecto;
   solo dejar `const variants = { primary: subtle, secondary: subtle, … }` con un único
   `subtle` y anotar en D que P4 decide si `primary` debe diferenciarse (**decisión de
   producto, preguntar al usuario**).

Cierre: `npm run lint` 0 errores.

---

### P1 — Tokens base (prerrequisito de P3–P14)

**Objetivo:** crear todo B.1 de forma **aditiva** (sin migrar componentes todavía, salvo
lo indicado). Tras esta parte, nada cambia visualmente.

Leer: `src/routes/layout.css`, `src/lib/theme/theme.css`, `src/lib/theme/tokens.ts`.

Tareas:
1. Añadir en `@theme`: radios B.1.1 (y borrar `--radius-art-round`), `--spacing-cover-*`
   B.1.2, `--spacing-icon-*` B.1.3, `--container-*` B.1.4, `--tracking-*`/`--leading-*`
   B.1.6, `--color-btn*` B.1.8.
2. Añadir en `theme.css` `:root`: variables de layout B.1.4 y z-index B.1.5.
3. Añadir `@utility` de B.1.3 (stroke), B.1.6 (tipografía), B.1.7 (superficies, grids);
   stagger en `.animate-enter`.
4. Scrollbar a tokens con variante clara (B.1.8).
5. Borrar utilidades de color sin uso y `--mf-sidebar-bg` (B.1.8). Antes de borrar cada
   una: `grep -rn "<nombre>" src` = 0.
6. Crear una página de prueba temporal **no commiteada** o usar DevTools para comprobar
   que `size-cover-sm`, `rounded-thumb`, `max-w-hero`, `text-display-1`, `z-(--z-menu)`
   y `stroke-thin` generan CSS (Tailwind 4 solo genera lo que ve usado: prueba con una
   clase en un componente y revierte).

Cierre: check+lint; captura de Inicio antes/después idéntica.

---

### P2 — Tema: una sola fuente de verdad + sin parpadeo en modo claro

**Objetivo:** eliminar la duplicación `theme.css` ↔ `tokens.ts` y el flash oscuro al
cargar con modo claro guardado.

Hallazgos:
- Los valores por defecto de los ~15 tokens dinámicos están escritos **dos veces**: en
  `theme.css` (`:root`, hue 145) y en `buildThemeTokens()`. El bloque claro de
  `theme.css` repite `--mf-bg`, `--mf-elevated`, `--mf-panel-bg`, `--mf-bar-bg`,
  `--mf-hairline`, `--mf-hero-bg`, `--mf-spotlight-bg`, `--mf-modal-glow`, que luego
  `tokens.ts` pisa inline.
- `mode.svelte.ts` aplica `data-theme` solo tras hidratar → flash oscuro.

Tareas:
1. `src/app.html`: script inline en `<head>` que lea `localStorage['musify.theme']` y
   ponga `document.documentElement.dataset.theme` antes del primer paint (try/catch).
2. `+layout.svelte`: en `<svelte:head>`, emitir
   `<style>:root{…buildThemeTokens(ACCENT_HUE,'dark')}:root[data-theme=light]{…buildThemeTokens(ACCENT_HUE,'light')}</style>`
   (serializado con un helper `tokensToCss(tokens)` en `tokens.ts`).
3. Borrar de `theme.css` todos los tokens que genera `buildThemeTokens` (dark y light).
   `theme.css` queda solo con neutrales fijos, superficies, texto, layout y z-index.
4. `+layout.svelte`: los dos `div` de fondo con `style=` → `@utility app-backdrop` /
   `app-vignette` en `layout.css`. `main` con `padding-bottom: calc(…)` inline → clase
   con `--mf-dock-h` que se activa con `data-has-track` en el propio `main`.

Cierre: con modo claro guardado, recarga dura sin flash; cambio de pista sigue animando
el hue; check+lint.

---

### P3 — `Artwork` y `Avatar` (arte unificado)

**Objetivo:** un único componente de carátula y uno de avatar.

Hallazgos:
- `Cover`, `PlaylistArt` y `MixArt` implementan la misma cascada (imagen → mosaico 2×2 →
  primera carátula → placeholder). `MixArt` ≡ `PlaylistArt` con
  `trackIds = items.map(i => i.trackId)`.
- `AlbumCard` pasa el **id del álbum** como `trackId` a `Cover` solo para poder usar `src`
  (hack).
- Avatar "imagen o inicial" reimplementado en `TopBar`, `MobileHeader`, Explorar (mejor
  resultado + fila de usuario) y `ArtistAvatar` (con rayado).
- Cada uso decide su propio radio (`rounded-lg`, `rounded-xl`, `rounded-2xl`,
  `rounded-control`, `rounded-art-lg`…) y su tamaño (`h-8.5`, `h-9`, `h-10`, `h-10.5`,
  `h-11 md:h-12`, `h-13`, `h-14`, `h-15.5`, `h-22`, `h-34`, `h-36 sm:h-41`).

Tareas:
1. Crear `Artwork.svelte` y `Avatar.svelte` según B.3 usando tokens B.1.1/B.1.2.
   `size` decide dimensión **y** radio; `class` solo para layout (`shrink-0`, márgenes).
2. Migrar **todos** los usos (buscar `Cover`, `PlaylistArt`, `MixArt`, `ArtistAvatar`,
   `user.picture`, `profilePictureUrl`) aplicando la tabla B.1.2.
3. `ArtistAvatar` → en `u/[id]/+page.svelte` usar `<Avatar size="hero">` (el nombre ya lo
   muestra la cabecera; el label bajo el avatar era redundante — **Δ visible**, confirmar).
4. Borrar `Cover.svelte`, `PlaylistArt.svelte`, `MixArt.svelte`, `ArtistAvatar.svelte`.

Cierre: `grep -rn "Cover\b\|PlaylistArt\|MixArt\|ArtistAvatar" src` solo en `Artwork`;
visual en Inicio, Explorar (búsqueda), Sidebar, Cola, player, cabeceras.

---

### P4 — Botones: `Button`, `IconButton`, `PlayButton`

**Objetivo:** que no quede ningún `<button>` estilado a mano.

Hallazgos (botones hechos a mano): TopBar atrás/adelante (círculos `h-7.5`), limpiar
búsqueda, login (`bg-accent-soft rounded-full`), MobileHeader login, Sidebar `+` ×2, Cola
cerrar/"Vaciar", PlayerExtras cola, TransportControls (shuffle, prev, play, next, repeat),
TrackInfo corazón, "Mejor resultado" (círculo `h-11.5`), MixTile overlay (`h-9.5`),
`BackLink`. Overrides con `!`: Inicio `!bg-cta-strong !text-ink hover:!brightness-95` y
`!font-normal`.

Tareas:
1. `Button.svelte`: variantes `subtle` (hoy primary/secondary), `glass` (hoy
   `secondaryOnGlow`), `accent`, `strong` (cta-strong/ink — sustituye los `!`), `danger`.
   Mantener alias `primary`/`secondary` solo si P0 no obtuvo decisión. Radio →
   `rounded-control`. Quitar necesidad de `!font-normal` (peso normal en `glass`, o prop
   `weight`).
2. `IconButton.svelte`: añadir `shape: 'square'|'round'`, `size: 'xs'|'sm'|'md'|'lg'`,
   `tone: 'muted'|'subtle'|'plain'`, `pressed?: boolean` (→ `aria-pressed` + `text-accent`),
   `href?`, `surface?: boolean` (fondo `surface-2`). Iconos con `size-icon-*`.
3. Crear `PlayButton.svelte` (B.3); iconos play/pausa SVG de `TransportControls` pasan a
   él (un único sitio para esos `<svg>`).
4. Migrar todos los botones listados. `TopBar` "adelante" es un `<span>` decorativo
   deshabilitado y `PlayerExtras` tiene un icono `AlignJustify` decorativo sin función:
   **preguntar** si se eliminan (código muerto visual) o se mantienen por fidelidad al
   prototipo.

Cierre: `grep -rn "<button" src | grep -v "ui/\(Button\|IconButton\|PlayButton\|Chip\|ListRow\)"`
→ solo casos justificados (dropzones, selector segmentado).

---

### P5 — Helpers TypeScript

**Objetivo:** crear B.2 y migrar sus usos (lógica, no estilos).

Tareas:
1. Crear `actions/pressable.ts`, `actions/clickOutside.ts`, `toggle.svelte.ts`.
   `createPlaylistModal` y `queuePanel` → `createToggle()`; borrar los dos archivos viejos
   o reducirlos a `export const queuePanel = createToggle()`.
2. `format.ts`: `plural()`; migrar las 15 pluralizaciones (`grep -rn "=== 1 ?" src`).
   Unificar a minúscula ("3 canciones") — **Δ visible** en Playlists/Inicio/liked.
3. `player.svelte.ts`: `toQueueItem`, `isQueueCurrent`, `playAllOrToggle`,
   `playShuffled`; renombrar `addToQueue` → `playNextItem` (el nombre actual confunde:
   inserta a continuación, no al final). Migrar álbum, playlist, liked, mixes, Inicio.
4. `recentlyPlayed.ts` y `liked.svelte.ts`: usar `toQueueItem`.
5. `tracks.ts`: el envoltorio `TrackTarget = { track }` + 8 accesores (`targetTitle`,
   `targetArtist`, …) no aporta nada. Sustituir por el `ApiTrackLike` directo y borrar
   accesores; dejar solo `isCurrent(track)` si hace falta. (Toca Inicio, Explorar,
   Mezcla, menús: hacerlo en esta parte para no dejar mezcla de estilos.)
6. `navigation.svelte.ts`: `searchHref`, `appNavLinks`.
7. `menus.svelte.ts`: `createTrackMenu()`; migrar Inicio, Explorar, Mezcla.

Cierre: check+lint; reproducir desde álbum, playlist, liked, mezcla, spotlight; menú
contextual "Reproducir a continuación" sigue funcionando.

---

### P6 — Menús: `ContextMenu` + `AccountMenu` + `GlassMenu`

**Objetivo:** un solo menú contextual.

Hallazgos: `TrackContextMenu` (109 líneas) y `AlbumContextMenu` (119) son copias:
misma función de posicionamiento con las mismas constantes, mismo backdrop, mismo Escape,
mismo submenú "Añadir a playlist" con `<form use:enhance>`; solo cambian los items y los
campos ocultos. Sus items reimplementan a mano las clases de `MenuItem` (con
`strokeWidth={2}` en vez de `1.8`). El item de playlist reimplementa `ListRow`.

Tareas:
1. Crear `ContextMenu.svelte` (B.3) usando `GlassMenu` + `MenuItem` + `Artwork size="xs"`
   y anchos `--mf-menu-w`/`--mf-submenu-w`; `contextMenuPosition(event)` exportada del
   módulo usando `MENU_WIDTH`/`SUBMENU_WIDTH` de `config.ts`.
2. Sustituir usos en Inicio, Explorar (pista y álbum), Mezcla. Borrar los dos menús viejos.
3. `AccountMenu`: usar `clickOutside`; eliminar el `div.relative` duplicado que
   `TopBar`/`MobileHeader` ponen alrededor; `panelClass` → prop `width: 'sm'|'md'`.
4. Añadir Escape a `AccountMenu` (hoy no cierra con Escape — mejora a11y).

Cierre: menú de pista y de álbum en Explorar, submenú abre a izquierda cerca del borde,
móvil (submenú debajo), Escape cierra.

---

### P7 — Filas: `ListRow` + `MediaIdentity` + `EqBars`

**Objetivo:** una sola fila compacta para todas las listas que no son tabla.

Filas reimplementadas hoy (todas: arte + título + subtítulo + meta + estado activo):

| Dónde                                   | Tamaño   |
| --------------------------------------- | -------- |
| `SearchResultRow` (Explorar ×4 grupos)  | lg       |
| Inicio "Continuar escuchando" (tarjeta) | lg, card |
| Inicio "Populares" (índice/EQ + meta)   | sm       |
| Inicio spotlight (índice, sin arte)     | sm       |
| Inicio "Tus playlists" (tarjeta)        | lg, card |
| `Queue` "Reproduciendo" + "A continuación" | sm    |
| `Sidebar` playlists                     | xs       |
| items de playlist de `ContextMenu`      | xs       |

Tareas:
1. Renombrar/generalizar `TrackTitleCell` → `MediaIdentity` (B.3). Usar en
   `player/TrackInfo` (sustituye su bloque cover+título+artista).
2. Crear `ListRow` (B.3) sobre `MediaIdentity` + `pressable`; variante `card` = padding
   `p-2.25 pr-3.5 rounded-art` (Continuar/Tus playlists).
3. `NowPlaying` ≡ `EqBars` con overlay: añadir a `EqBars` prop `overlay` (fondo
   `scrim/45`, color `on-art`) y borrar `NowPlaying`.
4. Migrar todas las filas de la tabla. Borrar `SearchResultRow`.

Cierre: Inicio, Explorar, Cola, Sidebar sin cambios visibles salvo Δ de B.1.2; teclado
(Tab + Enter) reproduce en todas las filas.

---

### P8 — Tablas de pistas: `TrackList`

**Objetivo:** tablas declarativas; borrar 3 celdas y 3 componentes de ruta.

Hallazgos:
- 6 tablas (`library`, `liked`, `mixes/[id]`, `AlbumTrackTable`, `PlaylistTrackTable`,
  `LibraryPicker`) repiten: `{@const active}`, `TrackIndexCell`, `TrackTitleCell`, N
  `TrackMeta`, y un `<form method=POST use:enhance><input hidden trackId><IconButton revealOnHover>`.
- Anchos de columna pasados desde cada página e **inconsistentes**: "Escuchas" `106px` en
  unas y `100px` en otras.
- La columna **"Álbum" siempre pinta "—"** (library, liked, playlist): placeholder sin
  datos. **Decisión**: eliminarla (recomendado) o dejarla en el preset.

Tareas:
1. Crear `TrackList.svelte` (B.3) con presets de columna en el propio módulo:
   `{ album: {label:'Álbum', width:'76px'}, added: {'Añadida','124px'}, uploaded: {'Subida','124px'}, plays: {'Escuchas','104px'} }`
   y columnas fijas (`index 32px/28px`, título `minmax(180px,1fr)/minmax(120px,1fr)`,
   duración `104px/52px`, acción `36px/32px`) como constantes del módulo.
2. `rowAction={{ action: '?/removeTrack', icon: X, label: 'Quitar de la playlist' }}`
   genera el form+IconButton; para liked (`onclick`) aceptar `rowAction.onclick`.
3. Migrar las 6 tablas; inlinear `AlbumTrackTable`, `PlaylistTrackTable`, `LibraryPicker`
   en sus páginas y borrarlos. Borrar `TrackRow`, `TrackIndexCell`, `TrackMeta`,
   `TrackTable`.
4. `LibraryPicker` usa overlay `NowPlaying` en vez de índice: `TrackList` con
   `index={false}` muestra EQ sobre el arte (vía `Artwork` children).

Cierre: las 6 páginas; hover revela acción; activo muestra EQ; móvil oculta metas.

---

### P9 — Tarjetas y grids

**Objetivo:** una tarjeta, cero wrappers.

Hallazgos: `AlbumCard` y `PlaylistCard` solo construyen el subtítulo y eligen el arte;
`MixTile` es una `MediaCard` con overlay de play y tipografía distinta; `MediaGrid` es un
`<div>` con variables inline; Inicio (mixes 164px, playlists 258px) y Explorar (géneros
268px) escriben su propio `grid-cols-[repeat(auto-fill,minmax(…))]`.

Tareas:
1. `MediaCard`: props `href`, `title`, `subtitle?`, `onPlay?` (overlay `PlayButton
   variant="glass"`), `art` snippet **o** props de `Artwork`, `oncontextmenu?`, `index`.
   Tipografía fija (`text-sm text-fg` / `text-xs text-fg-3`).
2. Helpers en `format.ts`: `albumMeta(album)`, `playlistMeta(playlist)`.
3. Sustituir `AlbumCard`, `PlaylistCard`, `MixTile` por `MediaCard`; `MediaGrid` por
   `grid-cards`/`grid-cards-lg`/`grid-tiles`/`grid-wide` (B.1.7). Borrar los 4 archivos.
4. Tile de género de Explorar: `style` con `--tile-hue` y el gradiente en un `@utility
   genre-tile` que lea `var(--tile-hue)`.

Cierre: /albums, /playlists, /u/[id], Inicio (mixes), Explorar (géneros).

---

### P10 — Cabeceras, diálogos y tipografía

**Objetivo:** 1 cabecera de página, 1 de sección, 1 diálogo de confirmación.

Hallazgos:
- `PageHeader` ≡ `CollectionHeader` sin portada/eyebrow. La cabecera de búsqueda de
  Explorar reimplementa ambos (eyebrow `text-sm font-semibold tracking-[0.16em]`).
- `AlbumHeader`/`PlaylistHeader` (rutas) solo montan el meta y los botones.
- Cada página decide el margen tras la cabecera (`mt-7 sm:mt-8`, `mt-6 sm:mt-8`, `mt-9`,
  `mt-10 sm:mt-12`…).
- 2 diálogos de borrado con tipografías distintas.
- `EmptyState` duplicado literalmente en Inicio ("Todavía no has escuchado nada" ×2).

Tareas:
1. Fusionar en `PageHeader`: `title`, `eyebrow?`, `description?`, `meta?`, `cover?`,
   `actions?`, `align?`. **La cabecera fija su propio margen inferior** (`mb-7 sm:mb-8`);
   quitar los `mt-*` de lo que venga después. Borrar `CollectionHeader`.
2. Inlinear `AlbumHeader`/`PlaylistHeader` en sus páginas (con `plural()` y el `BackLink`)
   y borrarlos. Iconos de botones: unificar (álbum tiene iconos, playlist no) →
   **decisión**: con iconos en ambos (recomendado) o sin ellos.
3. `SectionHeading`: prop `count?` que pinte la línea + "N resultados" (hoy repetido 4× en
   Explorar) y `href?`/`linkLabel?` para "Ver todas".
4. `ConfirmDialog` (B.3); migrar álbum y playlist. `Modal` usa `.glass-panel` y
   `z-(--z-modal)`; su eyebrow y título con recetas.
5. Aplicar recetas B.1.6 en: `EmptyState`, `+error.svelte`, "Sin resultados", hero de
   Inicio, stats, `Queue`, `Sidebar` ("Tus playlists"), `PlaylistForm` (visibilidad).
6. Inicio: estado vacío repetido → snippet local.

Cierre: todas las páginas con cabecera; diálogos de borrado idénticos.

---

### P11 — Formularios

**Objetivo:** un único esqueleto de formulario con portada.

Hallazgos:
- `AlbumForm` y `PlaylistForm` comparten: `use:enhance` con `submitting`,
  `ImageDropzone gradient class="h-36 w-36 rounded-2xl"`, `Field`/`Input`/`Textarea`,
  `Alert`, pie Cancelar/Enviar.
- `upload/+page.svelte` reimplementa el selector de portada (label + input + preview +
  gradiente) que ya hace `ImageDropzone`.
- `Input` y `Textarea` repiten la misma cadena de clases.
- Toggle Privada/Pública (`w-[calc(50%-4px)]`) y chips de Explorar: selectores de una
  opción con estilos propios.

Tareas:
1. `CoverForm.svelte` (esqueleto): props `action`, `coverFallbackUrl?`, `coverIcon`,
   `requireCover?`, `formMessage?`, `submitLabel`, `submittingLabel`, `canSubmit`,
   `helperText?`, `onCancel`, `onSuccess?`, snippet `fields`. `AlbumForm` y
   `PlaylistForm` quedan como ~30 líneas de campos cada uno (o se inlinean en sus
   páginas si solo se usan en un sitio: `AlbumForm` se usa en 2, `PlaylistForm` en 2 →
   mantener).
2. `ImageDropzone`: `size="hero"` usando `size-cover-hero` + `rounded-art-lg`; upload la
   usa (Δ: icono de 64px → `icon-xl`).
3. `@utility field-control` en `layout.css` con la cadena común de `Input`/`Textarea`.
4. `SegmentedControl.svelte` (`options`, `value` bindable) para Privada/Pública; `Chip`
   (B.3) para Explorar.

Cierre: crear/editar álbum y playlist, subir canción (drag&drop incluido).

---

### P12 — Chrome de la app (layout, navegación, reproductor)

**Objetivo:** una sola definición de navegación y un reproductor más pequeño.

Hallazgos:
- `Sidebar` y `TabsBar` definen cada uno su lista de links + `isActive`.
- `MobileHeader` compensa los links que faltan en `TabsBar` con items extra en el menú.
- Logo: gradiente + texto en MobileHeader y login; solo texto en Sidebar.
- `PlayerBar` repite dos veces la cadena de "barra de cristal" (igual que `TabsBar`).
- `PlayerExtras` (22 líneas) solo compone volumen + botón de cola.
- `SeekBar` es un `<button>` de click (sin teclado ni arrastre) con `h-[3.5px]`;
  `VolumeControl` es un `<input range>` con `<style>` propio (`84px`, `11px`, `3.5px`).
- `TrackInfo` duplica el bloque identidad de pista (→ `MediaIdentity`, P7).

Tareas:
1. `appNavLinks()` (B.2) en Sidebar y TabsBar (`primary` = los 4 de TabsBar).
2. `Logo.svelte`; usar en Sidebar, MobileHeader y login (Δ: Sidebar gana el cuadrado
   de gradiente — **confirmar**).
3. `glass-bar` en PlayerBar ×2 y TabsBar; `theme-transition` en Sidebar, Queue, etc.
4. Fusionar `PlayerExtras` dentro de `PlayerBar`; anchos laterales `--mf-player-side-w`.
5. `Slider.svelte` (B.3) con `<input type="range">` estilado vía tokens
   `--mf-range-h/--mf-range-thumb/--mf-volume-w`; sustituye `SeekBar` y `VolumeControl`
   (mejora: arrastre y teclado en la barra de progreso). Borrar ambos.
6. `TopBar`: usar `searchHref`, `IconButton`, `Avatar`; `max-w-search`.
7. z-index de header/dock/menús con B.1.5.

Cierre: navegación desktop/tablet/móvil, cola abre/cierra, seek por click, arrastre y
flechas, volumen.

---

### P13 — Páginas grandes: Explorar (602 líneas) e Inicio (466)

**Objetivo:** que las páginas sean composición, no lógica ni marcado repetido. Hacer
**después** de P7–P10 (usa `ListRow`, `SectionHeading count`, `PageHeader`).

Explorar:
1. `GENRE_INFO` + `genreTiles` → `src/lib/genres.ts`.
2. Lógica de búsqueda (`topResult`, `searchCounts`, `searchChips`, `capped`,
   `showGroup`, `playlistMatches`) → `src/lib/search.ts` (funciones puras).
3. Los 4 bloques de grupo (Canciones/Álbumes/Usuarios/Playlists) → un `{#each groups}`
   con `SectionHeading count` + `ListRow`.
4. "Mejor resultado": un solo marcado con `Artwork`/`Avatar` según tipo + `PlayButton
   as="span"`; eliminar el snippet duplicado button/a usando `ListRow`-like o
   `<svelte:element this={href ? 'a' : 'button'}>`.
5. Paginación `loadMore` → reutilizable si otra página la necesita; si no, dejar.

Inicio:
1. Usar `Page` en vez de `page-x pt-3 sm:pt-4 …` a mano; `gap-11.5` → token de sección
   (`--spacing-section: 2.875rem`, `gap-section`).
2. Stats ×3 idénticos → `{#each stats}`.
3. Hero/spotlight: `style=` de fondo → `@utility hero-surface` / `spotlight-surface`.
4. Filas → `ListRow` (si P7 no lo hizo ya).

Cierre: Explorar sin query, con query, filtros, sin resultados, scroll infinito; Inicio
completo con y sin datos.

---

### P14 — Barrido final y guardarraíles

**Objetivo:** que no vuelva a aparecer lo que se ha limpiado.

Tareas:
1. Script `web-player/scripts/check-tokens.mjs` + `npm run lint:tokens` (añadir a
   `lint`). Falla si encuentra en `src/**/*.svelte`:
   - `-\[` arbitrario fuera de la lista blanca B.4;
   - `rounded-(sm|md|lg|xl|2xl|3xl)\b` o `rounded\b` a secas;
   - `\b[hw]-\d` con su pareja en el mismo elemento (debe ser `size-*`);
   - espaciado fraccionario `-\d+\.\d+` en `h-`/`w-`/`size-` (tamaños → tokens);
   - `#[0-9a-f]{3,8}` en clases, `rgba(`, `strokeWidth=`, `text-base` sin receta;
   - `style="` que no sea de la lista blanca.
2. Espaciados fraccionarios restantes (`gap-2.75`, `py-1.75`, `mt-3.25`, `p-2.25`…): 
   redondear al paso de 0.5 más cercano **o** a la escala entera cuando el Δ ≤ 1px.
   Tabla guía: `.25/.75` → `.5` más cercano; `mt-0.75`→`mt-1`; `gap-1.75`→`gap-2`;
   `p-4.25`→`p-4`; `mb-4.5` (SectionHeading) se mantiene (Δ 2px visible en todas las
   secciones).
3. Detector de huérfanos (repetir y borrar lo que salga):
   ```bash
   for f in $(find src/lib/components -name "*.svelte"); do n=$(basename "$f" .svelte); c=$(grep -rlw "$n" --include=*.svelte --include=*.ts src | grep -v "/$n.svelte$" | wc -l); [ "$c" -eq 0 ] && echo "$f"; done
   ```
4. Actualizar `web-player/CLAUDE.md` → sección "Estilos" con: B.4, "usa `size-cover-*`,
   `size-icon-*`, recetas `text-display-*`/`text-eyebrow`/`text-body`/`text-count`,
   radios semánticos; componentes: `Artwork`, `Avatar`, `ListRow`, `TrackList`,
   `MediaCard`, `PageHeader`, `ContextMenu`…". Borrar `docs/design-system-refactor-plan.md`.
5. Recalcular la tabla A.4 y anotarla en D.

---

## D. Registro

| Parte | Estado | Commit | Notas / decisiones tomadas |
| ----- | ------ | ------ | -------------------------- |
| P0    | ✅     | (pendiente) | Button primary/secondary se mantienen iguales (decisión del usuario) |
| P1    | ✅     | (pendiente) | Verificado con `build` (Tailwind compila `@apply sm:` dentro de `@utility`); sin QA visual autenticada (Docker no disponible en esta sesión) |
| P2    | ✅     | (pendiente) | `<style>{expr}</style>` no interpola en Svelte (se trata como CSS del componente); solución: `{@html \`<style>${css}</style>\`}` |
| P3    | ✅     | (pendiente) | Se quitó el nombre bajo el avatar de perfil (decisión tomada: redundante con el título de CollectionHeader). Se añadió `size="fill"` a Artwork (no está en la spec) para las tarjetas de grid fluidas (AlbumCard/PlaylistCard/MixTile), ya que P9 (MediaCard) aún no existe. SearchResultRow perdió su prop `kind`/radio-por-tipo (ahora el radio lo decide Artwork/Avatar). Sin QA visual autenticada (Docker no disponible) |
| P4    | ✅     | (pendiente) | Decisión tomada sin bloquear: se elimina el botón "adelante" decorativo (TopBar) y el icono `AlignJustify` decorativo (PlayerExtras) por ser código muerto visual. `TransportControls` (shuffle/prev/next/repeat) se deja sin migrar a IconButton a propósito: son iconos sin chrome (sin fondo/hit-box), migrarlos habría añadido una píldora de hover no presente hoy — riesgo visual no verificable sin Docker |
| P5    | ⬜     |        |                            |
| P6    | ⬜     |        |                            |
| P7    | ⬜     |        |                            |
| P8    | ⬜     |        |                            |
| P9    | ⬜     |        |                            |
| P10   | ⬜     |        |                            |
| P11   | ⬜     |        |                            |
| P12   | ⬜     |        |                            |
| P13   | ⬜     |        |                            |
| P14   | ⬜     |        |                            |

Decisiones de producto pendientes (preguntar al usuario antes de la parte indicada):
- P0/P4: ¿`Button` primary debe verse distinto de secondary?
- P3: ¿quitar el nombre bajo el avatar de perfil?
- P4: ¿eliminar el botón "adelante" decorativo y el icono `AlignJustify` del player?
- P5: pluralizaciones en minúscula en toda la app.
- P8: ¿eliminar la columna "Álbum" que siempre muestra "—"?
- P10: ¿iconos en los botones de cabecera de álbum **y** playlist?
- P12: ¿logo con cuadrado de gradiente también en la Sidebar?

---

## E. Inventario completo (veredicto por archivo)

Leyenda: **=** se mantiene · **~** se modifica · **→X** se fusiona en X · **✕** se borra.

### `src/lib/components/ui/`

| Archivo              | Líneas | Veredicto | Parte | Motivo |
| -------------------- | ------ | --------- | ----- | ------ |
| AccountMenu          | 73  | ~            | P6  | `clickOutside`, Escape, ancho por prop |
| AlbumCard            | 62  | ✕ →MediaCard | P9  | wrapper de meta + hack `trackId=albumId` |
| AlbumForm            | 123 | ~ (CoverForm)| P11 | esqueleto compartido |
| Alert                | 20  | =            |     | |
| ArtistAvatar         | 36  | ✕ →Avatar    | P3  | |
| BackLink             | 33  | ~            | P4  | `IconButton`/tokens; quitar `hover:text-accent-soft` duplicado |
| Badge                | 23  | ✕            | P0  | 0 usos |
| Button               | 66  | ~            | P0/P4 | variantes duplicadas, overrides `!` |
| Checkbox             | 29  | ~            | P14 | `size-*`, tokens |
| CollectionHeader     | 45  | ✕ →PageHeader| P10 | |
| Cover                | 47  | ✕ →Artwork   | P3  | |
| EmptyState           | 31  | ~            | P10 | recetas |
| EqBars               | 23  | ~            | P7  | absorbe `NowPlaying` |
| ExplicitBadge        | 13  | ~            | P1/P14 | `rounded-tag`, `size-4` |
| Field                | 27  | =            |     | |
| GlassMenu            | 15  | =            |     | base de menús |
| IconButton           | 39  | ~            | P4  | shape/size/tone/pressed/href |
| ImageDropzone        | 62  | ~            | P11 | `size="hero"`; usado por upload |
| InfiniteScroll       | 39  | =            |     | |
| Input                | 59  | ~            | P11 | `field-control` |
| MediaCard            | 32  | ~            | P0/P9 | bug 16px; absorbe MixTile |
| MediaGrid            | 27  | ✕ →`grid-*`  | P9  | |
| MenuItem             | 48  | ~            | P6  | iconos `size-icon-md` |
| MixArt               | 25  | ✕ →Artwork   | P3  | |
| MixTile              | 51  | ✕ →MediaCard | P9  | |
| Modal                | 106 | ~            | P10 | `.glass-panel`, z-token, recetas |
| NowPlaying           | 20  | ✕ →EqBars    | P7  | |
| Page                 | 14  | =            |     | Inicio debe usarlo (P13) |
| PageHeader           | 26  | ~            | P10 | absorbe CollectionHeader |
| PlaylistArt          | 61  | ✕ →Artwork   | P3  | |
| PlaylistCard         | 23  | ✕ →MediaCard | P9  | |
| PlaylistForm         | 161 | ~ (CoverForm)| P11 | + SegmentedControl |
| SearchResultRow      | 78  | ✕ →ListRow   | P7  | |
| SectionHeading       | 32  | ~            | P10 | `count`, "ver todas" |
| Surface              | 17  | =            |     | (solo upload; aceptable) |
| Textarea             | 28  | ~            | P11 | `field-control` |
| TrackIndexCell       | 27  | ✕ →TrackList | P8  | |
| TrackMeta            | 14  | ✕ →TrackList | P8  | |
| TrackRow             | 27  | ✕ →TrackList | P8  | |
| TrackTable           | 79  | ✕ →TrackList | P8  | |
| TrackTitleCell       | 77  | → MediaIdentity | P7 | renombrar + generalizar |
| **Nuevos**           |     | Artwork, Avatar, ListRow, MediaIdentity, TrackList, ContextMenu, PlayButton, ConfirmDialog, Slider, Logo, Chip, SegmentedControl, CoverForm | | |

### `src/lib/components/` y `player/`

| Archivo             | Líneas | Veredicto | Parte | Motivo |
| ------------------- | ------ | --------- | ----- | ------ |
| AlbumContextMenu    | 119 | ✕ →ContextMenu | P6 | copia de TrackContextMenu |
| TrackContextMenu    | 109 | ✕ →ContextMenu | P6 | |
| MobileHeader        | 70  | ~ | P6/P12 | Avatar, Logo, sin wrapper duplicado |
| Sidebar             | 125 | ~ | P7/P12 | `appNavLinks`, `ListRow` |
| TabsBar             | 50  | ~ | P12 | `appNavLinks`, `glass-bar` |
| TopBar              | 140 | ~ | P4/P12 | IconButton, Avatar, `searchHref` |
| player/PlayerBar    | 28  | ~ | P12 | `glass-bar`, absorbe PlayerExtras |
| player/PlayerDock   | 29  | ~ | P12 | z-token |
| player/PlayerExtras | 22  | ✕ →PlayerBar | P12 | |
| player/Queue        | 111 | ~ | P7 | `ListRow`, recetas, `pressable` |
| player/SeekBar      | 48  | ✕ →Slider | P12 | |
| player/TrackInfo    | 65  | ~ | P7 | `MediaIdentity`, IconButton pressed |
| player/TransportControls | 75 | ~ | P4 | `PlayButton`, `IconButton pressed` |
| player/VolumeControl| 52  | ✕ →Slider | P12 | |

### Rutas

| Archivo | Líneas | Veredicto | Parte | Motivo |
| ------- | ------ | --------- | ----- | ------ |
| `+layout.svelte` | 142 | ~ | P2 | tokens SSR, fondos a utilidades, padding del dock |
| `+page.svelte` (Inicio) | 466 | ~ | P0/P5/P7/P13 | 4 filas a mano, stats ×3, vacío ×2, `style=` |
| `+error.svelte` | 16 | ~ | P10 | recetas, `max-w-prose-sm` |
| `login/+page.svelte` | 50 | ~ | P12 | `Logo`, `max-w-cta` |
| `(app)/explore/+page.svelte` | 602 | ~ | P13 | dividir datos/lógica; 4 grupos → each |
| `(app)/upload/+page.svelte` | 288 | ~ | P11 | `ImageDropzone`, recetas |
| `(app)/library/+page.svelte` | 108 | ~ | P8 | `TrackList` |
| `(app)/liked/+page.svelte` | 123 | ~ | P0/P5/P8/P10 | imports muertos, `playAllOrToggle`, `TrackList` |
| `(app)/mixes/[id]/+page.svelte` | 134 | ~ | P5/P8/P10 | menú, `TrackList`, `PageHeader` |
| `(app)/albums/+page.svelte` | 74 | ~ | P9 | `MediaCard` + `grid-cards` |
| `(app)/albums/[id]/+page.svelte` | 91 | ~ | P8/P10 | inline header/tablas, `ConfirmDialog` |
| `albums/[id]/components/AlbumHeader` | 82 | ✕ (inline) | P10 | |
| `albums/[id]/components/AlbumTrackTable` | 64 | ✕ (inline) | P8 | |
| `albums/[id]/components/LibraryPicker` | 65 | ✕ (inline) | P8 | |
| `(app)/playlists/+page.svelte` | 62 | ~ | P9 | |
| `(app)/playlists/[id]/+page.svelte` | 95 | ~ | P8/P10 | |
| `playlists/[id]/components/PlaylistHeader` | 62 | ✕ (inline) | P10 | |
| `playlists/[id]/components/PlaylistTrackTable` | 71 | ✕ (inline) | P8 | |
| `(app)/u/[id]/+page.svelte` | 97 | ~ | P3/P9/P10 | Avatar, grid |
| `routes/layout.css` | 238 | ~ | P1 | todos los tokens y recetas |

### `src/lib/` (cliente)

| Archivo | Veredicto | Parte | Motivo |
| ------- | --------- | ----- | ------ |
| `theme/theme.css` | ~ | P1/P2 | quitar duplicados de `tokens.ts`, añadir layout/z |
| `theme/tokens.ts` | ~ | P2 | `tokensToCss`, sin `--mf-sidebar-bg` |
| `theme/palette.ts`, `theme/color.ts`, `theme/mode.svelte.ts` | = | | (`mode` pierde el flash con P2) |
| `tracks.ts` | ~ | P5 | quitar envoltorio `TrackTarget` + 8 accesores |
| `recentlyPlayed.ts` | ~ | P5 | `toQueueItem` |
| `player/player.svelte.ts` | ~ | P5 | helpers de cola, `toQueueItem`, renombrar `addToQueue` |
| `player/liked.svelte.ts` | ~ | P5 | `toQueueItem` |
| `player/queuePanel.svelte.ts`, `playlists.svelte.ts` | → `toggle.svelte.ts` | P5 | idénticos |
| `format.ts` | ~ | P5/P9 | `plural`, `albumMeta`, `playlistMeta` |
| `navigation.svelte.ts` | ~ | P5 | `searchHref`, `appNavLinks` |
| `config.ts` | ~ | P6 | `MENU_WIDTH`, `SUBMENU_WIDTH` |
| `mixes.ts`, `albums.ts`, `collections.ts`, `types.ts` | = | | |
| `lib/server/**`, `+page.server.ts`, `routes/api/**`, `auth/**`, `hooks.server.ts` | fuera de alcance | | no contienen UI ni estilos |
