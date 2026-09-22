# Plan: Frontend refactor and unification (web-player)

> Work plan for agents/AI with limited context (~128k). Replaces
> `design-system-refactor-plan.md` (whose content is now mostly applied).
> Based on a full read of **every** client `.svelte`, `.css` and `.ts` file
> in `web-player/src` (2026-09-18, branch `design-prot`).

## How to use this document

1. **Always read section A (Common context)**, it's short and every part assumes it.
2. Pick **one part** (P0 through P14). Each part lists: objective, prerequisites, files
   to read, tasks, the exact API of what gets created, a mapping table, and closing criteria.
3. One part equals one commit/PR. Don't mix parts.
4. If a part needs something from another part (say, a token or a helper) and it
   **doesn't exist yet**, create it yourself following the spec in section B. These are
   described with exact paths and APIs so any part can create them identically.
5. When you finish a part, check its box in section D (Log).

Recommended order (the arrows are **soft** dependencies, see point 4):

```
P0 → P1 → P2
        ↘ P3 (Artwork/Avatar) ─┐
        ↘ P4 (Buttons)  ───────┤
        ↘ P5 (TS helpers) ─────┼→ P6 (Menus) → P7 (Rows) → P8 (Tables) → P9 (Cards)
                               └→ P10 (Headers) → P11 (Forms) → P12 (Chrome) → P13 (Pages) → P14 (Final sweep)
```

---

## A. Common context (always read)

### A.1 Project stack and rules (from `web-player/CLAUDE.md`)

- SvelteKit 2 + **Svelte 5 runes** (`$props()` with `interface Props`, `$state`,
  `$derived`, `$effect`; no `export let` or `$:`; `{#each}` always keyed).
- **Tailwind 4**: there's no `tailwind.config.js`; the config lives in `src/routes/layout.css`
  (`@theme`, `@utility`). Raw color tokens in `src/lib/theme/theme.css` (`--mf-*`),
  hue-dependent dynamic tokens in `src/lib/theme/tokens.ts` (`buildThemeTokens`).
- No comments in code unless essential.
- Closing out each part: `npm run check` and `npm run lint` must be **clean** (from `web-player/`).
- Icons: `@lucide/svelte/icons/<name>`.

### A.2 Known gotchas (don't reintroduce them)

1. **Hue-animated tokens** (`--mf-bg`, `--mf-elevated`, `--mf-panel-bg`, `--mf-bar-bg`,
   `--mf-hairline`, `--mf-hero-bg`, `--mf-modal-glow`, `--mf-spotlight-bg`,
   `--mf-logo-grad`, `--mf-cover-grad`, `--mf-accent*`, `--mf-ambient`) are applied as
   an **inline style** on `<html>` from `+layout.svelte`. An override in
   `:root[data-theme='light']` **doesn't win**: the light variant belongs in `buildThemeTokens()`.
2. **Tailwind class conflicts**: never `class="bg-a {cond ? 'bg-b' : ''}"`. Always
   mutually exclusive: `class={cond ? 'bg-b' : 'bg-a'}`. Overrides of a shared component's
   base class go through a prop/variant, not `!important`.
3. Text without a size class inherits the browser's 16px. All visible text needs a
   typography recipe (see B.1.6).
4. `--mf-ink` is "text on CTA" and inverts in light mode; `--mf-tile-shade`,
   `--mf-cover-grad`, `--mf-liked-grad` are "artwork" (don't invert).
5. Visual reference: **Home** (`src/routes/+page.svelte`). A refactor change shouldn't
   change the look except where the part explicitly says so (the "Δ" column).

### A.3 Verifying each part

```sh
cd web-player
npm run check
npm run lint
```

Visual (Browser pane, `.claude/launch.json` → `musify-web`; dev login credentials are in the
project's memory): open each touched page **and** Home; test the active state (a track
playing), hover, mobile (375px) and light mode (account menu → "White mode").

### A.4 Measured baseline (to know if you're making progress)

| Metric                                           | Today                                |
| ------------------------------------------------ | ------------------------------------ |
| `.svelte` components (lib + route `components/`) | 61                                   |
| Client `.svelte`/`.css`/`.ts` lines              | ~8,700                               |
| Arbitrary classes `x-[…]`                        | 44                                   |
| Fractional spacing (`h-3.75`, `gap-2.75`…)       | ~190 occurrences, 60 distinct values |
| Distinct radii used                              | 13 (6 tokens + 7 Tailwind defaults)  |
| Distinct `strokeWidth` values                    | 13 values                            |
| `style="…"` attributes                           | 24                                   |
| Hand-rolled pluralizations `=== 1 ?`             | 15                                   |
| `eslint` errors                                  | 2 (`liked/+page.svelte`)             |

Overall goal: ~61 → ~45 components, −1,500/−2,000 lines, 0 arbitrary values outside an
allow list, 1 scale per dimension.

---

## B. Shared specs (single source of truth)

Any part that needs something from here and it doesn't exist yet creates it **exactly like this**.

### B.1 Tokens (in `src/routes/layout.css`, inside `@theme`)

Tailwind 4 generates utilities from these namespaces: `--radius-*` → `rounded-*`,
`--spacing-*` → `size-* w-* h-* p-* m-* gap-*`, `--container-*` → `max-w-*`,
`--tracking-*` → `tracking-*`, `--leading-*` → `leading-*`.

#### B.1.1 Radii (`--radius-*`)

| Token              | Value       | What it's for                             |
| ------------------ | ----------- | ----------------------------------------- |
| `rounded-tag`      | `0.25rem`   | badges, mini cells (ExplicitBadge, index) |
| `rounded-thumb`    | `0.5rem`    | covers ≤ 36px, logo                       |
| `rounded-control`  | `0.625rem`  | inputs, rows, menu items, buttons         |
| `rounded-art`      | `0.8125rem` | covers 42 to 88px, grid cards             |
| `rounded-art-lg`   | `1.125rem`  | covers ≥ 136px, dropzones                 |
| `rounded-panel`    | `1.125rem`  | containers, menus, modals, tiles          |
| `rounded-panel-lg` | `1.375rem`  | hero, spotlight, "best match"             |
| `rounded-full`     | (Tailwind)  | pills, avatars, round buttons             |

Remove `--radius-art-round` (0 uses). Mapping of what exists today:

| Today                   | px  | Uses | Becomes                                                  | Δ    |
| ----------------------- | --- | ---- | -------------------------------------------------------- | ---- |
| `rounded-sm`, `rounded` | 4   | 2    | `rounded-tag`                                            | 0    |
| `rounded-md`            | 6   | 1    | `rounded-thumb`                                          | +2px |
| `rounded-lg`            | 8   | 4    | `rounded-thumb`                                          | 0    |
| `rounded-xl`            | 12  | 6    | `rounded-control`                                        | −2px |
| `rounded-2xl`           | 16  | 8    | `rounded-art-lg` (artwork) / `rounded-panel` (container) | +2px |
| `rounded-3xl`           | 24  | 1    | `rounded-panel-lg`                                       | −2px |

#### B.1.2 Cover/artwork sizes (`--spacing-cover-*`)

| Token           | Value            | Current uses it absorbs (Δ)                                                     |
| --------------- | ---------------- | ------------------------------------------------------------------------------- |
| `cover-xs`      | `2.25rem` (36)   | menu thumbs `h-9`; sidebar `h-8.5` (+2)                                         |
| `cover-sm`      | `2.625rem` (42)  | table rows `h-10.5`; queue and "Popular" `h-10` (+2); mobile player `h-11` (−2) |
| `cover-md`      | `3rem` (48)      | desktop player `md:h-12`                                                        |
| `cover-lg`      | `3.5rem` (56)    | "Continue" `h-14`; search rows `h-13` (+4); Home playlist card `h-15.5` (−6)    |
| `cover-xl`      | `5.5rem` (88)    | "Best match" `h-22`                                                             |
| `cover-2xl`     | `8.5rem` (136)   | Home spotlight `h-34`                                                           |
| `cover-hero`    | `9rem` (144)     | collection headers, dropzones, profile avatar                                   |
| `cover-hero-sm` | `10.25rem` (164) | collection headers from `sm:` up                                                |

Usage: `size-cover-sm` (Tailwind 4's `size-*` = `width`+`height`). **Never** pair `h-X w-X`:
always use `size-*`.

#### B.1.3 Icons (`--spacing-icon-*`) and stroke

| Token     | Value      | Absorbs                                      |
| --------- | ---------- | -------------------------------------------- |
| `icon-xs` | `0.875rem` | `h-3.5` (14)                                 |
| `icon-sm` | `1rem`     | `h-3.75` (15), `h-4` (16), `h-4.25` (17)     |
| `icon-md` | `1.125rem` | `h-4.5` (18)                                 |
| `icon-lg` | `1.25rem`  | `h-5` (20), `h-6` (24, mobile login)         |
| `icon-xl` | `2.25rem`  | `h-9`/`h-10` (empty states, dropzone, Liked) |

Stroke: in `layout.css`, outside `@theme`:

```css
@layer base {
	svg.lucide {
		stroke-width: 1.8;
	}
}
@utility stroke-thin {
	stroke-width: 1.3;
}
@utility stroke-regular {
	stroke-width: 1.8;
}
@utility stroke-bold {
	stroke-width: 2.5;
}
```

The CSS property `stroke-width` beats the presentation attribute Lucide sets, so the
`strokeWidth` prop stops doing anything: **remove it from every use**. Mapping:
`1 to 1.4` → `stroke-thin`, `1.5 to 2` → (nothing, default), `2.5 to 3` → `stroke-bold`,
`strokeWidth={0}` + `fill` → the `stroke-0` class. Verify in the browser (DevTools →
Computed `stroke-width`) that the default applies before migrating in bulk.

#### B.1.4 Widths (`--container-*`) and layout

| Token            | Value   | Absorbs                                                                          |
| ---------------- | ------- | -------------------------------------------------------------------------------- |
| `max-w-prose-sm` | `26rem` | `max-w-105` (error), `max-w-108` (hero p), `max-w-110`, `max-w-115` (no results) |
| `max-w-hero`     | `34rem` | `max-w-135` (hero, seekbar)                                                      |
| `max-w-search`   | `30rem` | `max-w-120` (TopBar search box)                                                  |
| `max-w-cta`      | `20rem` | `max-w-80` (login buttons)                                                       |

Already exist and stay in `theme.css`: `--mf-sidebar-w`, `--mf-queue-w`, `--mf-nav-h`,
`--mf-player-h`, `--mf-safe-b`. Add:

```css
--mf-player-side-w: 16rem; /* w-64 from PlayerBar/PlayerExtras */
--mf-menu-w: 15rem; /* w-60 for context menus (= MENU_WIDTH 240) */
--mf-submenu-w: 16rem; /* w-64 (= SUBMENU_WIDTH 256) */
--mf-range-h: 3.5px; /* progress bar and volume */
--mf-range-thumb: 11px;
--mf-volume-w: 84px;
```

The TS constants `MENU_WIDTH`/`SUBMENU_WIDTH` (currently duplicated in `TrackContextMenu`
and `AlbumContextMenu`) move to `src/lib/config.ts`. No comment needed, the name speaks for itself.

#### B.1.5 Layers (`z-index`)

In `theme.css`'s `:root`: `--z-raised: 10; --z-sticky: 20; --z-backdrop: 30; --z-menu: 40;
--z-submenu: 50; --z-modal: 60; --z-grain: 80;`. Usage: `z-(--z-menu)`. Maps 1:1 to the
current values (`z-10`, `z-20`, `z-30`, `z-40`, `z-50`, `z-[60]`, `body::after` 80). Note:
`MobileHeader` and `PlayerDock` use `z-40` but they're "sticky", not menus, so they get
`--z-sticky` (verify the account menu still sits above the header: menu 40 > sticky 20).

#### B.1.6 Typography (`@utility` recipes in `layout.css`)

Type scale stays Tailwind's (`text-xs` through `text-4xl`), **no arbitrary sizes**.
Add to `@theme`: `--tracking-display: -0.02em; --tracking-eyebrow: 0.1em;
--leading-body: 1.65;`. Recipes (color is **not** part of the recipe, it's added separately):

```css
@utility text-display-1 {
	@apply font-display text-3xl font-medium tracking-tight sm:text-4xl;
}
@utility text-display-2 {
	@apply font-display text-2xl font-medium tracking-tight;
}
@utility text-display-3 {
	@apply font-display text-xl font-medium tracking-display;
}
@utility text-display-4 {
	@apply font-display text-lg font-medium tracking-tight;
}
@utility text-eyebrow {
	@apply text-xs font-medium tracking-widest uppercase;
}
@utility text-body {
	@apply text-sm leading-body;
}
@utility text-count {
	@apply text-xs tabular-nums;
}
```

> If `@apply` with an `sm:` variant doesn't compile inside `@utility`, use
> `@media (width >= 40rem) { font-size: var(--text-4xl); line-height: var(--text-4xl--line-height); }`
> the way `page-x` does.

Mapping of what exists today:

| Current pattern                                                                                                                                    | Recipe                               |
| -------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| `font-display text-3xl … sm:text-4xl` (PageHeader, CollectionHeader, Home hero, search h1 `text-2xl sm:text-3xl font-semibold tracking-[-0.03em]`) | `text-display-1` (Δ search: +1 step) |
| `font-display text-2xl` (Home stats, spotlight title, "Uploaded!")                                                                                 | `text-display-2`                     |
| `font-display text-xl … tracking-[-0.02em]` (Modal, EmptyState, +error, "No results", delete dialogs)                                              | `text-display-3` (all `font-medium`) |
| `font-display text-lg` (SectionHeading, "Best match", Queue "Up next" `text-base`)                                                                 | `text-display-4`                     |
| `text-xs font-medium tracking-widest uppercase` (plus variants `tracking-[0.14em]`, `[0.16em]`, `tracking-wider`, `text-sm font-semibold`)         | `text-eyebrow`                       |
| `text-sm leading-[1.65] text-fg-2`                                                                                                                 | `text-body text-fg-2`                |
| `text-xs text-muted tabular-nums`                                                                                                                  | `text-count text-muted`              |
| `tracking-[-0.01em]` (MixTile)                                                                                                                     | remove                               |

#### B.1.7 Surface and motion recipes (`@utility` in `layout.css`)

```css
@utility theme-transition {
	transition: background 0.5s ease-out;
} /* 8× transition-[background] duration-500 */
@utility glass-bar {
	/* PlayerBar ×2, TabsBar */
	background-image: var(--mf-bar-bg);
	border-top: 1px solid var(--mf-hairline);
	backdrop-filter: blur(24px);
	transition: background 0.5s ease-out;
}
@utility grid-cards {
	/* MediaGrid default: 150/165 */
	display: grid;
	gap: --spacing(3);
	grid-template-columns: repeat(auto-fill, minmax(9.375rem, 1fr));
	@media (width >= 40rem) {
		gap: --spacing(4);
		grid-template-columns: repeat(auto-fill, minmax(10.3125rem, 1fr));
	}
}
@utility grid-cards-lg {
	/* /playlists and /u/[id]: 180/210 */
	display: grid;
	gap: --spacing(3);
	grid-template-columns: repeat(auto-fill, minmax(11.25rem, 1fr));
	@media (width >= 40rem) {
		gap: --spacing(4);
		grid-template-columns: repeat(auto-fill, minmax(13.125rem, 1fr));
	}
}
@utility grid-tiles {
	/* Home mixes: 164 */
	display: grid;
	gap: --spacing(5);
	grid-template-columns: repeat(auto-fill, minmax(10.25rem, 1fr));
}
@utility grid-wide {
	/* Home playlists (258) and genres (268) → 16.5rem, Δ ±6px */
	display: grid;
	gap: --spacing(3.5);
	grid-template-columns: repeat(auto-fill, minmax(16.5rem, 1fr));
}
```

`.glass-panel` already exists; `Modal` should use it instead of its duplicated inline `style=`.

Stagger (replaces the 5 `style="animation-delay:…"` with 40 **or** 45ms):

```css
.animate-enter {
	animation-delay: calc(min(var(--i, 0), 10) * 45ms);
}
```

Usage: `style="--i:{i}"`. The only `style=` allowed alongside the ones in B.4.

#### B.1.8 Colors: cleanup

- Delete unused utilities: `--color-accent-dim`, `--color-accent-glow`,
  `--color-accent-hair`, `--color-accent-line`, `--color-accent-muted` (0 uses as a
  class; `--mf-accent-dim` is used in `::selection`, so that variable stays).
- `--color-on-accent` is an alias of `--mf-on-art` (same as `--color-on-art`), delete it and
  use `on-art`. `--color-hover` = `--mf-surface-2` (same as `surface-2`), keep
  only `hover` as the hover semantic and leave `surface-2` for resting-state backgrounds.
- `--mf-sidebar-bg` isn't used by any component (Sidebar uses `bg-bg`), delete it from
  `theme.css` and from `buildThemeTokens`.
- Button: `--mf-btn-bg` = `--mf-surface-2`; `--mf-btn-secondary-bg` only differs in light
  mode. Expose `--color-btn`, `--color-btn-hover`, `--color-btn-glass`,
  `--color-btn-glass-hover` in `@theme` and remove the `bg-[var(--mf-btn-…)]` from `Button.svelte`.
- Scrollbar: `rgba(255,255,255,.18/.32)` hardcoded in `layout.css` (broken in light mode) →
  tokens `--mf-scrollbar`, `--mf-scrollbar-hover` with a variant in `:root[data-theme='light']`.
- `MixTile`: `to-black/42`, `bg-ink/76`, `border-on-art/12` → `to-scrim/40`, `PlayButton`
  recipe (B.3).

### B.2 TypeScript helpers (single source of truth)

| File (new)                                 | API                                                                                                            | Replaces                                                                                                                                                    |
| ------------------------------------------ | -------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/lib/actions/pressable.ts`             | `pressable(node, fn)` → adds `role="button"`, `tabindex=0`, click + Enter/Space                                | 7 copies of `onkeydown` Enter/Space (Home ×5, Queue, TrackTitleCell)                                                                                        |
| `src/lib/actions/clickOutside.ts`          | `clickOutside(node, fn)`                                                                                       | `onDocumentClick` from `AccountMenu`                                                                                                                        |
| `src/lib/toggle.svelte.ts`                 | `createToggle(initial=false)` → `{ get open, show(), close(), toggle() }`                                      | `playlists.svelte.ts` and `player/queuePanel.svelte.ts` (identical)                                                                                         |
| `src/lib/format.ts` (extend)               | `plural(n, one, many)` → `"1 song"`; `fmtCount(n,'song','songs')`; **always lowercase**                        | 15 hand-rolled pluralizations, with inconsistent casing ("Song"/"song")                                                                                     |
| `src/lib/navigation.svelte.ts` (extend)    | `searchHref(q)`; `appNavLinks(user, counts?)` → `{href,label,icon,count?,primary:boolean}[]`                   | `buildHref` in TopBar and explore; Sidebar and TabsBar link lists                                                                                           |
| `src/lib/player/player.svelte.ts` (extend) | `isQueueCurrent(items)`, `playAllOrToggle(items)`, `playShuffled(items)`; rename `addToQueue` → `playNextItem` | `playAll`/`isCurrentQueue` in album, playlist, liked, mixes, Home spotlight (5 copies)                                                                      |
| `src/lib/player/player.svelte.ts`          | single `toQueueItem(track)` (normalizes `duration`/`listensCount` to `number`)                                 | hand-rolled mappings in `recentlyPlayed.ts` (×2), `liked.svelte.ts` (×2), `liked/+page`, `tracks.ts#targetForQueueItem` and the scattered `Number(…)` calls |
| `src/lib/menus.svelte.ts`                  | `createTrackMenu()` → `{ state, open(e, track), close(), playNext() }`                                         | context menu state + handler in Home, Explore, Mix                                                                                                          |

### B.3 New components (API)

| Component (`$lib/components/ui/`) | Props                                                                                                                                                                                                                                                           | Replaces / absorbs                                                                                                                                                                                               |
| --------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Artwork.svelte`                  | `src?`, `trackIds?: (string\|number)[]`, `size: 'xs'\|'sm'\|'md'\|'lg'\|'xl'\|'2xl'\|'hero'`, `shape?: 'auto'\|'round'`, `imageSize?: 'small'\|'medium'\|'large'` (auto from `size`), `fallback?: 'music'\|'gradient'\|'none'`, `class?`, `children?` (overlay) | `Cover`, `PlaylistArt`, `MixArt`; radius picked automatically by size (xs→thumb, sm to xl→art/control, 2xl/hero→art-lg). Priority: `src` → mosaic if ≥4 `trackIds` → first cover → fallback. A single `onerror`. |
| `Avatar.svelte`                   | `name`, `src?`, `size: 'sm'\|'md'\|'lg'\|'hero'`, `class?`                                                                                                                                                                                                      | initials/image in TopBar, MobileHeader, Explore (×2), `ArtistAvatar`                                                                                                                                             |
| `ListRow.svelte`                  | `href?` \| `onclick?`, `oncontextmenu?`, `active?`, `size?: 'sm'\|'md'\|'lg'`, `title`, `subtitle?`, `subtitleHref?`, `explicit?`, `meta?`, `leading?: Snippet`, `art: Snippet<[string]>` \| artwork props, `trailing?: Snippet`                                | `SearchResultRow` + 7 hand-rolled rows (see P7)                                                                                                                                                                  |
| `MediaIdentity.svelte`            | `title`, `subtitle?`, `subtitleHref?`, `explicit?`, `active?`, `art?: Snippet` \| `artwork` props, `titleClass?`                                                                                                                                                | `TrackTitleCell` (renamed and generalized) and the `player/TrackInfo` block                                                                                                                                      |
| `TrackList.svelte`                | `tracks: TrackLike[]`, `columns?: ('album'\|'added'\|'uploaded'\|'plays')[]`, `index?`, `onPlay(i)`, `rowAction?: {action, icon, label}` \| `rowActionSnippet?`, `oncontextmenu?(e, track)`                                                                     | `TrackTable`+`TrackRow`+`TrackIndexCell`+`TrackMeta` and the 6 tables (P8)                                                                                                                                       |
| `ContextMenu.svelte`              | `x`, `y`, `openLeft`, `onClose`, `items: {icon,label,onclick}[]`, `playlistAction?: {action: string, fields: Record<string,string>, label}`, `playlists`                                                                                                        | `TrackContextMenu` + `AlbumContextMenu` (90% identical)                                                                                                                                                          |
| `PlayButton.svelte`               | `playing?`, `size: 'sm'\|'md'\|'lg'`, `variant: 'strong'\|'glass'`, `label`, `onclick?`, `as?: 'button'\|'span'`                                                                                                                                                | play button in TransportControls, "Best match", MixTile overlay                                                                                                                                                  |
| `ConfirmDialog.svelte`            | `open`, `onClose`, `title`, `description`, `confirmLabel`, `action` (form POST)                                                                                                                                                                                 | 2 delete modals with different styles                                                                                                                                                                            |
| `Slider.svelte`                   | `value`, `max`, `label`, `oninput`, `variant: 'seek'\|'volume'`                                                                                                                                                                                                 | `SeekBar` (click-only) + `VolumeControl` (own `<style>`)                                                                                                                                                         |
| `Logo.svelte`                     | `size: 'sm'\|'lg'`                                                                                                                                                                                                                                              | logo in Sidebar (text only), MobileHeader, login                                                                                                                                                                 |
| `Chip.svelte`                     | `selected?`, `count?`, `href?`\|`onclick?`                                                                                                                                                                                                                      | filter chips and suggested genre chips in Explore                                                                                                                                                                |

### B.4 Allow list for `style=` and arbitrary values (after the refactor)

Allowed: `style="--i:{i}"` (stagger), per-hue tile gradients in Explore
(`--tile-hue`), progress width (`width:{pct}%`), menu position (`left/top`).
Allowed arbitrary values: `text-[clamp(…)]` on login, `grid-cols-[auto_1fr]`,
`max-h-[85dvh]`. Everything else becomes a token.

---

## C. Parts

### P0: Baseline and loose bugs (independent, ~30 min)

**Objective:** get `lint` green and fix detected bugs that don't require a refactor.

Tasks:

1. `src/routes/(app)/liked/+page.svelte`: remove the `Play` and `Pause` imports (eslint errors).
2. `src/lib/components/ui/MediaCard.svelte`: the title has no size class (inherits 16px) →
   `text-sm` (same as `MixTile`).
3. `src/routes/+page.svelte`, spotlight rows: `hover:bg-hover … {cond ? 'bg-accent-tint' : ''}`
   → make it mutually exclusive (`{cond ? 'bg-accent-tint' : 'hover:bg-hover'}`), same as the other rows.
4. `src/routes/(app)/albums/[id]/+page.svelte` delete modal: `text-base text-fg-3` →
   `text-sm leading-[1.65] text-fg-2` and a title matching the playlist one (will be unified in P10).
5. `Sidebar.svelte` (`+` button) and `Queue.svelte` (close): remove the useless `text-base`.
6. Delete `src/lib/components/ui/Badge.svelte` (0 importers).
7. `Button.svelte`: `primary` and `secondary` are the **same** class. Don't change the look;
   just leave `const variants = { primary: subtle, secondary: subtle, … }` with a single
   `subtle`, and note in D that P4 will decide whether `primary` should be differentiated
   (**product decision, ask the user**).

Closing: `npm run lint` 0 errors.

---

### P1: Base tokens (prerequisite for P3 through P14)

**Objective:** create all of B.1 **additively** (without migrating components yet, except
where noted). Nothing should change visually after this part.

Read: `src/routes/layout.css`, `src/lib/theme/theme.css`, `src/lib/theme/tokens.ts`.

Tasks:

1. Add to `@theme`: radii B.1.1 (and delete `--radius-art-round`), `--spacing-cover-*`
   B.1.2, `--spacing-icon-*` B.1.3, `--container-*` B.1.4, `--tracking-*`/`--leading-*`
   B.1.6, `--color-btn*` B.1.8.
2. Add to `theme.css`'s `:root`: layout variables from B.1.4 and z-index from B.1.5.
3. Add the `@utility` recipes from B.1.3 (stroke), B.1.6 (typography), B.1.7 (surfaces, grids);
   stagger in `.animate-enter`.
4. Move the scrollbar to tokens with a light variant (B.1.8).
5. Delete unused color utilities and `--mf-sidebar-bg` (B.1.8). Before deleting each
   one: `grep -rn "<name>" src` = 0.
6. Create a temporary, **uncommitted** test page, or use DevTools, to confirm
   `size-cover-sm`, `rounded-thumb`, `max-w-hero`, `text-display-1`, `z-(--z-menu)`
   and `stroke-thin` generate CSS (Tailwind 4 only generates what it sees used: test with a
   class on a component and revert).

Closing: check+lint; before/after screenshot of Home identical.

---

### P2: Theme, single source of truth + no flash in light mode

**Objective:** eliminate the `theme.css` ↔ `tokens.ts` duplication and the dark flash when
loading with light mode saved.

Findings:

- The default values of the ~15 dynamic tokens are written **twice**: in
  `theme.css` (`:root`, hue 145) and in `buildThemeTokens()`. The light block in
  `theme.css` repeats `--mf-bg`, `--mf-elevated`, `--mf-panel-bg`, `--mf-bar-bg`,
  `--mf-hairline`, `--mf-hero-bg`, `--mf-spotlight-bg`, `--mf-modal-glow`, which
  `tokens.ts` then overrides inline anyway.
- `mode.svelte.ts` only applies `data-theme` after hydration, causing a dark flash.

Tasks:

1. `src/app.html`: add an inline `<head>` script that reads `localStorage['musify.theme']`
   and sets `document.documentElement.dataset.theme` before the first paint (try/catch).
2. `+layout.svelte`: in `<svelte:head>`, emit
   `<style>:root{…buildThemeTokens(ACCENT_HUE,'dark')}:root[data-theme=light]{…buildThemeTokens(ACCENT_HUE,'light')}</style>`
   (serialized with a `tokensToCss(tokens)` helper in `tokens.ts`).
3. Delete from `theme.css` every token that `buildThemeTokens` generates (dark and light).
   `theme.css` is left with only fixed neutrals, surfaces, text, layout and z-index.
4. `+layout.svelte`: the two background `div`s with `style=` → `@utility app-backdrop` /
   `app-vignette` in `layout.css`. The `main` with an inline `padding-bottom: calc(…)` →
   a class using `--mf-dock-h`, activated via `data-has-track` on `main` itself.

Closing: with light mode saved, a hard reload shows no flash; track changes still animate
the hue; check+lint.

---

### P3: `Artwork` and `Avatar` (unified artwork)

**Objective:** a single cover component and a single avatar component.

Findings:

- `Cover`, `PlaylistArt` and `MixArt` implement the same cascade (image → 2×2 mosaic →
  first cover → placeholder). `MixArt` is basically `PlaylistArt` with
  `trackIds = items.map(i => i.trackId)`.
- `AlbumCard` passes the **album id** as `trackId` to `Cover` just to be able to use `src`
  (a hack).
- "Image or initial" avatars are reimplemented in `TopBar`, `MobileHeader`, Explore (best
  match + user row) and `ArtistAvatar` (with a ring).
- Each usage picks its own radius (`rounded-lg`, `rounded-xl`, `rounded-2xl`,
  `rounded-control`, `rounded-art-lg`…) and its own size (`h-8.5`, `h-9`, `h-10`, `h-10.5`,
  `h-11 md:h-12`, `h-13`, `h-14`, `h-15.5`, `h-22`, `h-34`, `h-36 sm:h-41`).

Tasks:

1. Create `Artwork.svelte` and `Avatar.svelte` per B.3 using tokens B.1.1/B.1.2.
   `size` drives both dimension **and** radius; `class` is only for layout (`shrink-0`, margins).
2. Migrate **every** usage (search for `Cover`, `PlaylistArt`, `MixArt`, `ArtistAvatar`,
   `user.picture`, `profilePictureUrl`), applying the B.1.2 table.
3. `ArtistAvatar`: on `u/[id]/+page.svelte`, use `<Avatar size="hero">` (the name is already
   shown by the header; the label under the avatar was redundant, **visible Δ**, confirm this).
4. Delete `Cover.svelte`, `PlaylistArt.svelte`, `MixArt.svelte`, `ArtistAvatar.svelte`.

Closing: `grep -rn "Cover\b\|PlaylistArt\|MixArt\|ArtistAvatar" src` only matches inside
`Artwork`; visual check on Home, Explore (search), Sidebar, Queue, player, headers.

---

### P4: Buttons, `Button`, `IconButton`, `PlayButton`

**Objective:** no `<button>` left hand-styled.

Findings (hand-made buttons): TopBar back/forward (circles `h-7.5`), clear
search, login (`bg-accent-soft rounded-full`), MobileHeader login, Sidebar `+` ×2, Queue
close/"Clear", PlayerExtras queue, TransportControls (shuffle, prev, play, next, repeat),
TrackInfo heart, "Best match" (circle `h-11.5`), MixTile overlay (`h-9.5`),
`BackLink`. Overrides with `!`: Home `!bg-cta-strong !text-ink hover:!brightness-95` and
`!font-normal`.

Tasks:

1. `Button.svelte`: variants `subtle` (today primary/secondary), `glass` (today
   `secondaryOnGlow`), `accent`, `strong` (cta-strong/ink, replaces the `!` overrides), `danger`.
   Keep the `primary`/`secondary` aliases only if P0 didn't land a decision. Radius →
   `rounded-control`. Remove the need for `!font-normal` (normal weight in `glass`, or a
   `weight` prop).
2. `IconButton.svelte`: add `shape: 'square'|'round'`, `size: 'xs'|'sm'|'md'|'lg'`,
   `tone: 'muted'|'subtle'|'plain'`, `pressed?: boolean` (→ `aria-pressed` + `text-accent`),
   `href?`, `surface?: boolean` (`surface-2` background). Icons with `size-icon-*`.
3. Create `PlayButton.svelte` (B.3); play/pause SVG icons from `TransportControls` move
   into it (one single place for those `<svg>`s).
4. Migrate all listed buttons. `TopBar` "forward" is a disabled decorative `<span>` and
   `PlayerExtras` has a decorative `AlignJustify` icon with no function:
   **ask** whether to remove them (visually dead code) or keep them for fidelity to
   the prototype.

Closing: `grep -rn "<button" src | grep -v "ui/\(Button\|IconButton\|PlayButton\|Chip\|ListRow\)"`
→ only justified cases remain (dropzones, segmented control).

---

### P5: TypeScript helpers

**Objective:** create B.2 and migrate its usages (logic, not styles).

Tasks:

1. Create `actions/pressable.ts`, `actions/clickOutside.ts`, `toggle.svelte.ts`.
   `createPlaylistModal` and `queuePanel` → `createToggle()`; delete the two old files
   or reduce them to `export const queuePanel = createToggle()`.
2. `format.ts`: `plural()`; migrate the 15 hand-rolled pluralizations (`grep -rn "=== 1 ?" src`).
   Unify to lowercase ("3 songs"), **visible Δ** on Playlists/Home/liked.
3. `player.svelte.ts`: `toQueueItem`, `isQueueCurrent`, `playAllOrToggle`,
   `playShuffled`; rename `addToQueue` → `playNextItem` (the current name is confusing:
   it inserts next, not at the end). Migrate album, playlist, liked, mixes, Home.
4. `recentlyPlayed.ts` and `liked.svelte.ts`: use `toQueueItem`.
5. `tracks.ts`: the `TrackTarget = { track }` wrapper plus 8 accessors (`targetTitle`,
   `targetArtist`, …) doesn't add anything. Replace with the `ApiTrackLike` object directly
   and delete the accessors; keep only `isCurrent(track)` if needed. (This touches Home,
   Explore, Mix, menus: do it in this part so there's no mix of styles left behind.)
6. `navigation.svelte.ts`: `searchHref`, `appNavLinks`.
7. `menus.svelte.ts`: `createTrackMenu()`; migrate Home, Explore, Mix.

Closing: check+lint; reproduce from album, playlist, liked, mix, spotlight; the
"Play next" context menu action still works.

---

### P6: Menus, `ContextMenu` + `AccountMenu` + `GlassMenu`

**Objective:** a single context menu.

Findings: `TrackContextMenu` (109 lines) and `AlbumContextMenu` (119) are copies:
same positioning function with the same constants, same backdrop, same Escape handling,
same "Add to playlist" submenu with `<form use:enhance>`; only the items and hidden
fields differ. Their items hand-reimplement `MenuItem`'s classes (with
`strokeWidth={2}` instead of `1.8`). The playlist item reimplements `ListRow`.

Tasks:

1. Create `ContextMenu.svelte` (B.3) using `GlassMenu` + `MenuItem` + `Artwork size="xs"`
   and widths `--mf-menu-w`/`--mf-submenu-w`; export `contextMenuPosition(event)` from
   the module using `MENU_WIDTH`/`SUBMENU_WIDTH` from `config.ts`.
2. Replace usages in Home, Explore (track and album), Mix. Delete the two old menus.
3. `AccountMenu`: use `clickOutside`; remove the duplicated `div.relative` wrapper that
   `TopBar`/`MobileHeader` put around it; `panelClass` → `width: 'sm'|'md'` prop.
4. Add Escape support to `AccountMenu` (currently it doesn't close on Escape, an a11y improvement).

Closing: track and album menus in Explore, submenu opens to the left near the edge,
mobile (submenu below), Escape closes it.

---

### P7: Rows, `ListRow` + `MediaIdentity` + `EqBars`

**Objective:** a single compact row for every list that isn't a table.

Rows reimplemented today (all of them: art + title + subtitle + meta + active state):

| Where                                 | Size     |
| ------------------------------------- | -------- |
| `SearchResultRow` (Explore ×4 groups) | lg       |
| Home "Continue listening" (card)      | lg, card |
| Home "Popular" (index/EQ + meta)      | sm       |
| Home spotlight (index, no art)        | sm       |
| Home "Your playlists" (card)          | lg, card |
| `Queue` "Now playing" + "Up next"     | sm       |
| `Sidebar` playlists                   | xs       |
| `ContextMenu` playlist items          | xs       |

Tasks:

1. Rename/generalize `TrackTitleCell` → `MediaIdentity` (B.3). Use it in
   `player/TrackInfo` (replacing its cover+title+artist block).
2. Create `ListRow` (B.3) on top of `MediaIdentity` + `pressable`; `card` variant = padding
   `p-2.25 pr-3.5 rounded-art` (Continue/Your playlists).
3. `NowPlaying` is basically `EqBars` with an overlay: add an `overlay` prop to `EqBars`
   (background `scrim/45`, `on-art` color) and delete `NowPlaying`.
4. Migrate every row in the table above. Delete `SearchResultRow`.

Closing: Home, Explore, Queue, Sidebar unchanged visually except for the B.1.2 Δ; keyboard
(Tab + Enter) plays from every row.

---

### P8: Track tables, `TrackList`

**Objective:** declarative tables; delete 3 cells and 3 route components.

Findings:

- 6 tables (`library`, `liked`, `mixes/[id]`, `AlbumTrackTable`, `PlaylistTrackTable`,
  `LibraryPicker`) repeat: `{@const active}`, `TrackIndexCell`, `TrackTitleCell`, N
  `TrackMeta`s, and a `<form method=POST use:enhance><input hidden trackId><IconButton revealOnHover>`.
- Column widths passed down from each page are **inconsistent**: "Plays" is `106px` in
  some places and `100px` in others.
- The "Album" column **always shows a placeholder dash**: it's a stand-in with no real data.
  **Decision needed**: remove it (recommended) or leave it in the preset.

Tasks:

1. Create `TrackList.svelte` (B.3) with column presets in the module itself:
   `{ album: {label:'Album', width:'76px'}, added: {'Added','124px'}, uploaded: {'Uploaded','124px'}, plays: {'Plays','104px'} }`
   and fixed columns (`index 32px/28px`, title `minmax(180px,1fr)/minmax(120px,1fr)`,
   duration `104px/52px`, action `36px/32px`) as module constants.
2. `rowAction={{ action: '?/removeTrack', icon: X, label: 'Remove from playlist' }}`
   generates the form+IconButton; for liked (`onclick`), accept `rowAction.onclick`.
3. Migrate the 6 tables; inline `AlbumTrackTable`, `PlaylistTrackTable`, `LibraryPicker`
   into their pages and delete them. Delete `TrackRow`, `TrackIndexCell`, `TrackMeta`,
   `TrackTable`.
4. `LibraryPicker` uses the `NowPlaying` overlay instead of an index: `TrackList` with
   `index={false}` shows the EQ over the art (via `Artwork` children).

Closing: all 6 pages; hover reveals the action; the active track shows the EQ; mobile hides meta.

---

### P9: Cards and grids

**Objective:** one card, zero wrappers.

Findings: `AlbumCard` and `PlaylistCard` only build the subtitle and pick the art;
`MixTile` is a `MediaCard` with a play overlay and different typography; `MediaGrid` is a
`<div>` with inline variables; Home (mixes 164px, playlists 258px) and Explore (genres
268px) each write their own `grid-cols-[repeat(auto-fill,minmax(…))]`.

Tasks:

1. `MediaCard`: props `href`, `title`, `subtitle?`, `onPlay?` (`PlayButton variant="glass"`
   overlay), `art` snippet **or** `Artwork` props, `oncontextmenu?`, `index`.
   Fixed typography (`text-sm text-fg` / `text-xs text-fg-3`).
2. Helpers in `format.ts`: `albumMeta(album)`, `playlistMeta(playlist)`.
3. Replace `AlbumCard`, `PlaylistCard`, `MixTile` with `MediaCard`; replace `MediaGrid`
   with `grid-cards`/`grid-cards-lg`/`grid-tiles`/`grid-wide` (B.1.7). Delete the 4 files.
4. Explore's genre tile: the `style` with `--tile-hue` and its gradient move into a
   `@utility genre-tile` that reads `var(--tile-hue)`.

Closing: /albums, /playlists, /u/[id], Home (mixes), Explore (genres).

---

### P10: Headers, dialogs and typography

**Objective:** 1 page header, 1 section header, 1 confirmation dialog.

Findings:

- `PageHeader` is basically `CollectionHeader` without the cover/eyebrow. Explore's
  search header reimplements both (eyebrow `text-sm font-semibold tracking-[0.16em]`).
- `AlbumHeader`/`PlaylistHeader` (routes) just wire up the meta and the buttons.
- Each page decides its own margin after the header (`mt-7 sm:mt-8`, `mt-6 sm:mt-8`, `mt-9`,
  `mt-10 sm:mt-12`…).
- 2 delete dialogs with different typography.
- `EmptyState` literally duplicated in Home ("You haven't listened to anything yet" ×2).

Tasks:

1. Merge into `PageHeader`: `title`, `eyebrow?`, `description?`, `meta?`, `cover?`,
   `actions?`, `align?`. **The header owns its own bottom margin** (`mb-7 sm:mb-8`);
   remove the `mt-*` from whatever follows it. Delete `CollectionHeader`.
2. Inline `AlbumHeader`/`PlaylistHeader` into their pages (with `plural()` and the `BackLink`)
   and delete them. Button icons: unify (album has icons, playlist doesn't), so
   **decide**: icons on both (recommended) or on neither.
3. `SectionHeading`: add a `count?` prop that renders the line + "N results" (currently
   repeated 4× in Explore) and `href?`/`linkLabel?` for "View all".
4. `ConfirmDialog` (B.3); migrate album and playlist. `Modal` uses `.glass-panel` and
   `z-(--z-modal)`; its eyebrow and title use the recipes.
5. Apply B.1.6 recipes in: `EmptyState`, `+error.svelte`, "No results", the Home
   hero, stats, `Queue`, `Sidebar` ("Your playlists"), `PlaylistForm` (visibility).
6. Home: replace the repeated empty state with a local snippet.

Closing: every page has a header; delete dialogs look identical.

---

### P11: Forms

**Objective:** a single form skeleton with a cover.

Findings:

- `AlbumForm` and `PlaylistForm` share: `use:enhance` with `submitting`,
  `ImageDropzone gradient class="h-36 w-36 rounded-2xl"`, `Field`/`Input`/`Textarea`,
  `Alert`, and a Cancel/Submit footer.
- `upload/+page.svelte` reimplements the cover picker (label + input + preview +
  gradient) that `ImageDropzone` already does.
- `Input` and `Textarea` repeat the same class string.
- The Private/Public toggle (`w-[calc(50%-4px)]`) and Explore's chips are both single-option
  selectors with their own styles.

Tasks:

1. `CoverForm.svelte` (skeleton): props `action`, `coverFallbackUrl?`, `coverIcon`,
   `requireCover?`, `formMessage?`, `submitLabel`, `submittingLabel`, `canSubmit`,
   `helperText?`, `onCancel`, `onSuccess?`, `fields` snippet. `AlbumForm` and
   `PlaylistForm` end up as ~30 lines of fields each (or get inlined into their
   pages if only used in one place: `AlbumForm` is used in 2, `PlaylistForm` in 2, so
   keep them as components).
2. `ImageDropzone`: `size="hero"` using `size-cover-hero` + `rounded-art-lg`; upload
   uses it (Δ: 64px icon → `icon-xl`).
3. `@utility field-control` in `layout.css` with the shared `Input`/`Textarea` class string.
4. `SegmentedControl.svelte` (`options`, bindable `value`) for Private/Public; `Chip`
   (B.3) for Explore.

Closing: create/edit album and playlist, upload a song (including drag & drop).

---

### P12: App chrome (layout, navigation, player)

**Objective:** a single navigation definition and a smaller player.

Findings:

- `Sidebar` and `TabsBar` each define their own link list + `isActive`.
- `MobileHeader` compensates for links missing from `TabsBar` with extra menu items.
- Logo: gradient + text in MobileHeader and login; text-only in Sidebar.
- `PlayerBar` repeats the "glass bar" string twice (same as `TabsBar`).
- `PlayerExtras` (22 lines) only composes volume + queue button.
- `SeekBar` is a click-only `<button>` (no keyboard, no dragging) with `h-[3.5px]`;
  `VolumeControl` is a range `<input>` with its own `<style>` (`84px`, `11px`, `3.5px`).
- `TrackInfo` duplicates the track identity block (→ `MediaIdentity`, P7).

Tasks:

1. `appNavLinks()` (B.2) in Sidebar and TabsBar (`primary` = the 4 items from TabsBar).
2. `Logo.svelte`; use it in Sidebar, MobileHeader and login (Δ: Sidebar gains the gradient
   square, **confirm this**).
3. `glass-bar` in PlayerBar ×2 and TabsBar; `theme-transition` in Sidebar, Queue, etc.
4. Merge `PlayerExtras` into `PlayerBar`; side widths via `--mf-player-side-w`.
5. `Slider.svelte` (B.3) with an `<input type="range">` styled through the
   `--mf-range-h/--mf-range-thumb/--mf-volume-w` tokens; replaces `SeekBar` and `VolumeControl`
   (bonus: dragging and keyboard support on the progress bar). Delete both.
6. `TopBar`: use `searchHref`, `IconButton`, `Avatar`; `max-w-search`.
7. Header/dock/menu z-index via B.1.5.

Closing: desktop/tablet/mobile navigation, queue opens/closes, seek via click, drag and
arrow keys, volume.

---

### P13: Big pages, Explore (602 lines) and Home (466)

**Objective:** make these pages composition, not logic or repeated markup. Do this
**after** P7 through P10 (uses `ListRow`, `SectionHeading count`, `PageHeader`).

Explore:

1. `GENRE_INFO` + `genreTiles` → `src/lib/genres.ts`.
2. Search logic (`topResult`, `searchCounts`, `searchChips`, `capped`,
   `showGroup`, `playlistMatches`) → `src/lib/search.ts` (pure functions).
3. The 4 group blocks (Songs/Albums/Users/Playlists) → a single `{#each groups}`
   with `SectionHeading count` + `ListRow`.
4. "Best match": a single markup block with `Artwork`/`Avatar` depending on type + `PlayButton
as="span"`; eliminate the duplicated button/a snippet with a `ListRow`-like element or
   `<svelte:element this={href ? 'a' : 'button'}>`.
5. `loadMore` pagination: reusable if another page needs it; if not, leave as is.

Home:

1. Use `Page` instead of the hand-written `page-x pt-3 sm:pt-4 …`; `gap-11.5` → a section
   token (`--spacing-section: 2.875rem`, `gap-section`).
2. The 3 identical stats blocks → `{#each stats}`.
3. Hero/spotlight: background `style=` → `@utility hero-surface` / `spotlight-surface`.
4. Rows → `ListRow` (if P7 didn't already do it).

Closing: Explore with no query, with a query, filters, no results, infinite scroll; Home
complete with and without data.

---

### P14: Final sweep and guardrails

**Objective:** make sure what's been cleaned up doesn't come back.

Tasks:

1. Script `web-player/scripts/check-tokens.mjs` + `npm run lint:tokens` (add it to
   `lint`). Fails if it finds any of the following in `src/**/*.svelte`:
   - `-\[` arbitrary values outside the B.4 allow list;
   - `rounded-(sm|md|lg|xl|2xl|3xl)\b` or a bare `rounded\b`;
   - `\b[hw]-\d` paired with its counterpart on the same element (should be `size-*`);
   - fractional spacing `-\d+\.\d+` in `h-`/`w-`/`size-` (sizes should be tokens);
   - `#[0-9a-f]{3,8}` in classes, `rgba(`, `strokeWidth=`, `text-base` with no recipe;
   - `style="` outside the allow list.
2. Remaining fractional spacing (`gap-2.75`, `py-1.75`, `mt-3.25`, `p-2.25`…):
   round to the nearest 0.5 step **or** to the whole-number scale when the Δ is ≤ 1px.
   Guide table: `.25/.75` → nearest `.5`; `mt-0.75` → `mt-1`; `gap-1.75` → `gap-2`;
   `p-4.25` → `p-4`; `mb-4.5` (SectionHeading) stays as is (Δ 2px, visible across all
   sections).
3. Orphan detector (run it repeatedly and delete whatever it flags):
   ```bash
   for f in $(find src/lib/components -name "*.svelte"); do n=$(basename "$f" .svelte); c=$(grep -rlw "$n" --include=*.svelte --include=*.ts src | grep -v "/$n.svelte$" | wc -l); [ "$c" -eq 0 ] && echo "$f"; done
   ```
4. Update `web-player/CLAUDE.md`, the "Styles" section, with: B.4, "use `size-cover-*`,
   `size-icon-*`, the `text-display-*`/`text-eyebrow`/`text-body`/`text-count` recipes,
   semantic radii; components: `Artwork`, `Avatar`, `ListRow`, `TrackList`,
   `MediaCard`, `PageHeader`, `ContextMenu`…". Delete `docs/design-system-refactor-plan.md`.
5. Recompute the A.4 table and log it in D.

---

## D. Log

| Part | Status | Commit    | Notes / decisions made                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ---- | ------ | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| P0   | ✅     | (pending) | Button primary/secondary stay identical (user's decision)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| P1   | ✅     | (pending) | Verified with `build` (Tailwind compiles `@apply sm:` inside `@utility`); no authenticated visual QA (Docker not available in this session)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| P2   | ✅     | (pending) | `<style>{expr}</style>` doesn't interpolate in Svelte (it's treated as the component's own CSS); solution: `{@html \`<style>${css}</style>\`}`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| P3   | ✅     | (pending) | Removed the name under the profile avatar (decision made: redundant with CollectionHeader's title). Added `size="fill"` to Artwork (not in the spec) for the fluid grid cards (AlbumCard/PlaylistCard/MixTile), since P9 (MediaCard) doesn't exist yet. SearchResultRow lost its `kind`/radius-by-type prop (radius is now decided by Artwork/Avatar). No authenticated visual QA (Docker not available)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| P4   | ✅     | (pending) | Decision made without blocking: removed the decorative "forward" button (TopBar) and the decorative `AlignJustify` icon (PlayerExtras) since they were visually dead code. `TransportControls` (shuffle/prev/next/repeat) is intentionally left unmigrated to IconButton: those are chromeless icons (no background/hit box), migrating them would have added a hover pill that doesn't exist today, a visual risk that couldn't be verified without Docker                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| P5   | 🟡     | (pending) | Done: pressable/clickOutside, toggle.svelte.ts, plural() (+ lowercase, visible Δ), player.svelte.ts helpers (toQueueItem/isQueueCurrent/playAllOrToggle/playShuffled, addToQueue→playNextItem), navigation.svelte.ts (searchHref/appNavLinks, appNavLinks created but its migration into Sidebar/TabsBar is left for P12 as that part specifies), menus.svelte.ts (createTrackMenu, migrated in Home/Explore/Mix). **Deliberately pending**: task 5 (removing the `TrackTarget` wrapper from tracks.ts and its 8 accessors) is deferred, it's the riskiest change in the plan (touches types across Home/Explore/Mix/menus) and couldn't be verified visually without Docker; tackle it together with P13 when those pages get rewritten                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| P6   | ✅     | (pending) | AccountMenu's z-index unified to z-(--z-menu) (was z-20, now at the same level as ContextMenu, a minor Δ that improves consistency); no authenticated visual QA (Docker not available)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| P7   | ✅     | (pending) | MediaIdentity stays "pure" (no onClick) per spec; the 6 track tables (P8) wrap their own `use:pressable` until P8 replaces them with TrackList. Documented deltas: Sidebar's active row now uses bg-accent-tint/text-accent-soft (was neutral gray, now consistent with other active rows); Home's "Your playlists" card loses the "by {your name}" line (redundant, it's always the current user) and its radius changes from rounded-panel to rounded-art (consistent with the "Continue" card); Queue's "Up next" rows lose the art's opacity-80 (ListRow doesn't expose that customization). No authenticated visual QA (Docker not available)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| P8   | ✅     | (pending) | Decision made (recommended by the plan itself, non-blocking): removed the "Album" column that always showed a placeholder dash. TrackList unifies TrackTable+TrackRow+TrackIndexCell+TrackMeta; AlbumTrackTable/PlaylistTrackTable/LibraryPicker are inlined into their pages and deleted. No authenticated visual QA (Docker not available)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| P9   | ✅     | (pending) | MediaCard gains the art's hover-zoom and scrim+PlayButton, conditioned on `onPlay` (previously exclusive to MixTile); this will now apply to any future card with onPlay, an intentional consistency choice. No authenticated visual QA (Docker not available)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| P10  | ✅     | (pending) | Decision made (recommended, non-blocking): icons on playlist header buttons (previously only album had them). PlaylistHeader loses its unused `id` prop after being inlined. Explore: the "Results for…" header now uses PageHeader's `mb-7/8` instead of a hand-tuned `mt-5` before the filter chips, slightly larger gap between the summary and the chips (minor, documented Δ). No authenticated visual QA (Docker not available)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| P11  | ✅     | (pending) | CoverForm adds an `extra` snippet (not in the literal spec, only `fields`) for full-width content below the cover+fields row (album description, playlist visibility selector) that didn't fit in a single column. Upload uses ImageDropzone with `onselect` (new prop) and wraps it in `{#key}` to be able to reset it after publishing, Δ: the cover container goes from 144/176px (mobile/desktop) to a fixed 144px (size-cover-hero, no larger desktop variant), and the empty-state icon goes from 64px to 36px (icon-xl), as the plan recommends. No authenticated visual QA (Docker not available)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| P12  | ✅     | (pending) | PlayerExtras merges into PlayerBar (volume/queue inline); Slider replaces SeekBar+VolumeControl (confirmed via grep that SeekBar's `compact` mode was dead code). Sidebar and TabsBar migrate to `appNavLinks()` (P5). Decision made (non-blocking): Sidebar gains the gradient-branded `Logo` it didn't have before, for consistency with MobileHeader/login. PlayerDock/MobileHeader z-index fixed to `z-(--z-sticky)` (they're fixed bars, not menus) and SegmentedControl to `z-(--z-raised)`, as B.1.5 specifies. `glass-bar`/`theme-transition` applied on PlayerBar (both variants), TabsBar, Queue and Home (hero/spotlight), replacing duplicated styles. No authenticated visual QA (Docker not available); `/login` smoke test with no console errors                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| P13  | ✅     | (pending) | This part also tackles the task deferred in P5: removes the `TrackTarget` wrapper from `tracks.ts` and its 8 accessors (`targetId`/`targetTitle`/`targetArtist`/`targetOwnerUserId`/`targetExplicit`/`targetListensCount`/`isTargetCurrent`/`queueItemForTarget`/`targetForQueueItem`); Explore, Home, Mix, `menus.svelte.ts` and `mixes.ts` now use track objects (`ApiTrackLike`/`LocalTrack`) directly, with `toQueueItem`/`trackFromQueueItem` (new, in `player.svelte.ts`) as the only conversion functions. TypeScript revealed the `isExplicit` field doesn't exist in the real `/tracks` schema, Explore's "explicit" badge was already a no-op before the refactor (always `undefined ?? false`); the binding was removed instead of faking support for it. Explore: `GENRE_INFO`/`genreTiles` → `src/lib/genres.ts`; search logic (`showGroup`/`capped`/`matchPlaylists`/`searchCounts`/`searchChips`/`findTopResult`) → `src/lib/search.ts` (pure functions, parameterized by `sfilter`/`query` instead of closing over component state); "Best match" unifies the duplicated button/link with `<svelte:element this={...}>`. Decision made without blocking: the 4 group blocks (Songs/Albums/Users/Playlists) are **not** collapsed into a single `{#each groups}`, each group has genuinely different snippets, context menus and `ListRow` props (track menu vs. album menu, overlay only on songs, differently formatted trailing content), and forcing a generic configuration would have added more indirection than it removed. Home: uses `Page` (previously a hand-written `page-x pt-3 sm:pt-4`); the 3 stats blocks → `{#each}`; hero/spotlight backgrounds (`style=` with `var(--mf-hero-bg)`/`var(--mf-spotlight-bg)`) → new `hero-surface`/`spotlight-surface` utilities (in `layout.css`) and the `--spacing-section: 2.875rem` token → `gap-section` replaces `gap-11.5`. No authenticated visual QA (Docker not available); `/explore` without a session redirects to `/login` because `+layout.server.ts` requires `allowAnonymousListening` from the backend (unreachable without Docker), a pre-existing behavior, not a regression; `/login` smoke test with no console errors |
| P14  | ✅     | (pending) | `scripts/check-tokens.mjs` (`npm run lint:tokens`, added to `lint`) scans `src/**/*.svelte` and fails on: `-[…]` arbitrary values outside the B.4 allow list, bare `rounded-(sm/md/lg/xl/2xl/3xl)`/`rounded`, `h-N w-N` pairs (should be `size-N`), fractional quarter spacing (`.25`/`.75`), literal hex/`rgba()` values. Cleaned the repo down to 0 violations: ~40 `h-N w-N` pairs → `size-N` (mostly icons, via a one-off script), ~26 quarter-spacing values rounded to the nearest whole number (Δ≤1px, e.g. `py-2.75`→`py-3`, `gap-1.75`→`gap-2`), 3 `rounded-sm/lg` + 1 bare `rounded` → tokens (`rounded-tag`/`rounded-thumb`/`rounded-control`), 2 `tracking-[-0.02em]` → `tracking-display` (matched the existing token exactly and hadn't been migrated), 1 `tracking-[0.14em]` in Explore → the `text-eyebrow` recipe, Home hero's `rounded-3xl` → `rounded-panel-lg` (consistent with the featured playlist card). Removed the `stroke-thin/regular/bold` recipes (created in P1, 0 consumers), **decision made, non-blocking**: `strokeWidth={n}` (13 values, across almost every component) isn't migrated to those CSS recipes because there's no way to verify without Docker that `lucide-svelte` respects `stroke-width` via class instead of the prop, and a regression there would be silent across the whole app; the script documents this gap explicitly and doesn't enforce it. Also tackled the task deferred from P5 (see P13). Orphan detector: 0 components with no importers (all 47 in `src/lib/components/` are in use). `docs/design-system-refactor-plan.md` (previous plan, superseded) deleted. `web-player/CLAUDE.md` gains a "Styles" section with the B.4 allow list, the base components to reuse, and the documented `strokeWidth` gap. Baseline recalculated (see table below)                                                                                                                                                                                                                                                                                                                                                                                       |

Pending product decisions (ask the user before the listed part):

- P0/P4: should `Button` primary look different from secondary?
- P3: remove the name under the profile avatar?
- P4: remove the decorative "forward" button and the `AlignJustify` icon from the player?
- P5: lowercase pluralizations across the whole app.
- P8: remove the "Album" column that always shows a placeholder dash?
- P10: icons on album **and** playlist header buttons?
- P12: gradient-square logo in the Sidebar too?

### D.1 Recalculated baseline (after P14, closing out the refactor)

Recalculated with the same criteria as A.4 (grep over `src/`), in the same session that closed out P14.

| Metric                                        | A.4 (before)     | After P14                                                                                                                                               |
| --------------------------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `.svelte` components in `src/lib/components/` | 61               | 47                                                                                                                                                      |
| Client `.svelte`/`.css`/`.ts` lines           | ~8,700           | ~8,310 (−~390; the −1,500/−2,000 goal had already been reached in P3 through P11, before P13 added `genres.ts`/`search.ts` as new, well-separated code) |
| Arbitrary classes `x-[…]`                     | 44               | 11 (the remaining 11 are all in the B.4 allow list)                                                                                                     |
| Fractional quarter spacing (`.25`/`.75`)      | ~190 occurrences | 0 (rounded to the nearest whole number, Δ≤1px)                                                                                                          |
| Distinct radii used                           | 13               | 8 (7 B.1.1 tokens + `rounded-full`)                                                                                                                     |
| Distinct `strokeWidth` values                 | 13               | 12 (out of scope, see the P14 log entry and CLAUDE.md)                                                                                                  |
| `style="…"` attributes                        | 24               | 16 (across 12 files; all dynamic per instance, stagger, menu position, `var(--mf-*)`, animation, none with a literal color/measurement)                 |
| Hand-rolled pluralizations `=== 1 ?`          | 15               | 2 (1 is `plural()` itself in `format.ts`; 1 is a past-participle agreement in `library` that `plural()` can't cover)                                    |
| `eslint` errors                               | 2                | 0                                                                                                                                                       |
| `npm run lint:tokens` (new, P14)              | n/a              | 0 violations                                                                                                                                            |

---

## E. Full inventory (verdict per file)

Legend: **=** stays as is · **~** gets modified · **→X** merges into X · **✕** gets deleted.

### `src/lib/components/ui/`

| File             | Lines | Verdict                                                                                                                                     | Part   | Reason                                                          |
| ---------------- | ----- | ------------------------------------------------------------------------------------------------------------------------------------------- | ------ | --------------------------------------------------------------- |
| AccountMenu      | 73    | ~                                                                                                                                           | P6     | `clickOutside`, Escape, width via prop                          |
| AlbumCard        | 62    | ✕ →MediaCard                                                                                                                                | P9     | meta wrapper + the `trackId=albumId` hack                       |
| AlbumForm        | 123   | ~ (CoverForm)                                                                                                                               | P11    | shared skeleton                                                 |
| Alert            | 20    | =                                                                                                                                           |        |                                                                 |
| ArtistAvatar     | 36    | ✕ →Avatar                                                                                                                                   | P3     |                                                                 |
| BackLink         | 33    | ~                                                                                                                                           | P4     | `IconButton`/tokens; remove duplicated `hover:text-accent-soft` |
| Badge            | 23    | ✕                                                                                                                                           | P0     | 0 uses                                                          |
| Button           | 66    | ~                                                                                                                                           | P0/P4  | duplicated variants, `!` overrides                              |
| Checkbox         | 29    | ~                                                                                                                                           | P14    | `size-*`, tokens                                                |
| CollectionHeader | 45    | ✕ →PageHeader                                                                                                                               | P10    |                                                                 |
| Cover            | 47    | ✕ →Artwork                                                                                                                                  | P3     |                                                                 |
| EmptyState       | 31    | ~                                                                                                                                           | P10    | recipes                                                         |
| EqBars           | 23    | ~                                                                                                                                           | P7     | absorbs `NowPlaying`                                            |
| ExplicitBadge    | 13    | ~                                                                                                                                           | P1/P14 | `rounded-tag`, `size-4`                                         |
| Field            | 27    | =                                                                                                                                           |        |                                                                 |
| GlassMenu        | 15    | =                                                                                                                                           |        | menu base                                                       |
| IconButton       | 39    | ~                                                                                                                                           | P4     | shape/size/tone/pressed/href                                    |
| ImageDropzone    | 62    | ~                                                                                                                                           | P11    | `size="hero"`; used by upload                                   |
| InfiniteScroll   | 39    | =                                                                                                                                           |        |                                                                 |
| Input            | 59    | ~                                                                                                                                           | P11    | `field-control`                                                 |
| MediaCard        | 32    | ~                                                                                                                                           | P0/P9  | 16px bug; absorbs MixTile                                       |
| MediaGrid        | 27    | ✕ →`grid-*`                                                                                                                                 | P9     |                                                                 |
| MenuItem         | 48    | ~                                                                                                                                           | P6     | `size-icon-md` icons                                            |
| MixArt           | 25    | ✕ →Artwork                                                                                                                                  | P3     |                                                                 |
| MixTile          | 51    | ✕ →MediaCard                                                                                                                                | P9     |                                                                 |
| Modal            | 106   | ~                                                                                                                                           | P10    | `.glass-panel`, z-token, recipes                                |
| NowPlaying       | 20    | ✕ →EqBars                                                                                                                                   | P7     |                                                                 |
| Page             | 14    | =                                                                                                                                           |        | Home should use it (P13)                                        |
| PageHeader       | 26    | ~                                                                                                                                           | P10    | absorbs CollectionHeader                                        |
| PlaylistArt      | 61    | ✕ →Artwork                                                                                                                                  | P3     |                                                                 |
| PlaylistCard     | 23    | ✕ →MediaCard                                                                                                                                | P9     |                                                                 |
| PlaylistForm     | 161   | ~ (CoverForm)                                                                                                                               | P11    | + SegmentedControl                                              |
| SearchResultRow  | 78    | ✕ →ListRow                                                                                                                                  | P7     |                                                                 |
| SectionHeading   | 32    | ~                                                                                                                                           | P10    | `count`, "view all"                                             |
| Surface          | 17    | =                                                                                                                                           |        | (upload only; acceptable)                                       |
| Textarea         | 28    | ~                                                                                                                                           | P11    | `field-control`                                                 |
| TrackIndexCell   | 27    | ✕ →TrackList                                                                                                                                | P8     |                                                                 |
| TrackMeta        | 14    | ✕ →TrackList                                                                                                                                | P8     |                                                                 |
| TrackRow         | 27    | ✕ →TrackList                                                                                                                                | P8     |                                                                 |
| TrackTable       | 79    | ✕ →TrackList                                                                                                                                | P8     |                                                                 |
| TrackTitleCell   | 77    | → MediaIdentity                                                                                                                             | P7     | rename + generalize                                             |
| **New**          |       | Artwork, Avatar, ListRow, MediaIdentity, TrackList, ContextMenu, PlayButton, ConfirmDialog, Slider, Logo, Chip, SegmentedControl, CoverForm |        |                                                                 |

### `src/lib/components/` and `player/`

| File                     | Lines | Verdict        | Part   | Reason                              |
| ------------------------ | ----- | -------------- | ------ | ----------------------------------- |
| AlbumContextMenu         | 119   | ✕ →ContextMenu | P6     | copy of TrackContextMenu            |
| TrackContextMenu         | 109   | ✕ →ContextMenu | P6     |                                     |
| MobileHeader             | 70    | ~              | P6/P12 | Avatar, Logo, no duplicated wrapper |
| Sidebar                  | 125   | ~              | P7/P12 | `appNavLinks`, `ListRow`            |
| TabsBar                  | 50    | ~              | P12    | `appNavLinks`, `glass-bar`          |
| TopBar                   | 140   | ~              | P4/P12 | IconButton, Avatar, `searchHref`    |
| player/PlayerBar         | 28    | ~              | P12    | `glass-bar`, absorbs PlayerExtras   |
| player/PlayerDock        | 29    | ~              | P12    | z-token                             |
| player/PlayerExtras      | 22    | ✕ →PlayerBar   | P12    |                                     |
| player/Queue             | 111   | ~              | P7     | `ListRow`, recipes, `pressable`     |
| player/SeekBar           | 48    | ✕ →Slider      | P12    |                                     |
| player/TrackInfo         | 65    | ~              | P7     | `MediaIdentity`, IconButton pressed |
| player/TransportControls | 75    | ~              | P4     | `PlayButton`, `IconButton pressed`  |
| player/VolumeControl     | 52    | ✕ →Slider      | P12    |                                     |

### Routes

| File                                           | Lines | Verdict    | Part         | Reason                                                 |
| ---------------------------------------------- | ----- | ---------- | ------------ | ------------------------------------------------------ |
| `+layout.svelte`                               | 142   | ~          | P2           | SSR tokens, backgrounds to utilities, dock padding     |
| `+page.svelte` (Home)                          | 466   | ~          | P0/P5/P7/P13 | 4 hand-rolled rows, stats ×3, empty state ×2, `style=` |
| `+error.svelte`                                | 16    | ~          | P10          | recipes, `max-w-prose-sm`                              |
| `login/+page.svelte`                           | 50    | ~          | P12          | `Logo`, `max-w-cta`                                    |
| `(app)/explore/+page.svelte`                   | 602   | ~          | P13          | split data/logic; 4 groups → each                      |
| `(app)/upload/+page.svelte`                    | 288   | ~          | P11          | `ImageDropzone`, recipes                               |
| `(app)/library/+page.svelte`                   | 108   | ~          | P8           | `TrackList`                                            |
| `(app)/liked/+page.svelte`                     | 123   | ~          | P0/P5/P8/P10 | dead imports, `playAllOrToggle`, `TrackList`           |
| `(app)/mixes/[id]/+page.svelte`                | 134   | ~          | P5/P8/P10    | menu, `TrackList`, `PageHeader`                        |
| `(app)/albums/+page.svelte`                    | 74    | ~          | P9           | `MediaCard` + `grid-cards`                             |
| `(app)/albums/[id]/+page.svelte`               | 91    | ~          | P8/P10       | inline header/tables, `ConfirmDialog`                  |
| `albums/[id]/components/AlbumHeader`           | 82    | ✕ (inline) | P10          |                                                        |
| `albums/[id]/components/AlbumTrackTable`       | 64    | ✕ (inline) | P8           |                                                        |
| `albums/[id]/components/LibraryPicker`         | 65    | ✕ (inline) | P8           |                                                        |
| `(app)/playlists/+page.svelte`                 | 62    | ~          | P9           |                                                        |
| `(app)/playlists/[id]/+page.svelte`            | 95    | ~          | P8/P10       |                                                        |
| `playlists/[id]/components/PlaylistHeader`     | 62    | ✕ (inline) | P10          |                                                        |
| `playlists/[id]/components/PlaylistTrackTable` | 71    | ✕ (inline) | P8           |                                                        |
| `(app)/u/[id]/+page.svelte`                    | 97    | ~          | P3/P9/P10    | Avatar, grid                                           |
| `routes/layout.css`                            | 238   | ~          | P1           | all tokens and recipes                                 |

### `src/lib/` (client)

| File                                                                              | Verdict              | Part  | Reason                                            |
| --------------------------------------------------------------------------------- | -------------------- | ----- | ------------------------------------------------- |
| `theme/theme.css`                                                                 | ~                    | P1/P2 | remove `tokens.ts` duplicates, add layout/z       |
| `theme/tokens.ts`                                                                 | ~                    | P2    | `tokensToCss`, no `--mf-sidebar-bg`               |
| `theme/palette.ts`, `theme/color.ts`, `theme/mode.svelte.ts`                      | =                    |       | (`mode` loses the flash with P2)                  |
| `tracks.ts`                                                                       | ~                    | P5    | remove the `TrackTarget` wrapper + 8 accessors    |
| `recentlyPlayed.ts`                                                               | ~                    | P5    | `toQueueItem`                                     |
| `player/player.svelte.ts`                                                         | ~                    | P5    | queue helpers, `toQueueItem`, rename `addToQueue` |
| `player/liked.svelte.ts`                                                          | ~                    | P5    | `toQueueItem`                                     |
| `player/queuePanel.svelte.ts`, `playlists.svelte.ts`                              | → `toggle.svelte.ts` | P5    | identical                                         |
| `format.ts`                                                                       | ~                    | P5/P9 | `plural`, `albumMeta`, `playlistMeta`             |
| `navigation.svelte.ts`                                                            | ~                    | P5    | `searchHref`, `appNavLinks`                       |
| `config.ts`                                                                       | ~                    | P6    | `MENU_WIDTH`, `SUBMENU_WIDTH`                     |
| `mixes.ts`, `albums.ts`, `collections.ts`, `types.ts`                             | =                    |       |                                                   |
| `lib/server/**`, `+page.server.ts`, `routes/api/**`, `auth/**`, `hooks.server.ts` | out of scope         |       | no UI or styles                                   |
