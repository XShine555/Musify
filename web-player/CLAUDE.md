# Musify — web-player

Musify's public web app (`web-player/` folder). For design and UI recipes, use the **`musify-web`** skill.

## Stack

SvelteKit 2 (`adapter-node`) · Svelte 5 **runes** · Tailwind 4 · TypeScript strict · Zitadel OIDC auth (`openid-client` + `jose`).

There is no `svelte.config.js` (config is inline in `vite.config.ts`) nor `tailwind.config.js` (config lives in `src/routes/layout.css`).

## Commands

```sh
npm run dev         # dev server (port 5173)
npm run build
npm run check        # svelte-check — must exit 0 errors before closing a change
npm run format        # prettier --write .
npm run lint          # prettier --check . && eslint . && npm run lint:tokens
npm run lint:tokens   # scripts/check-tokens.mjs — design-system guardrail (see below)
```

## Rules

- **No comments** in code unless essential.
- `npm run lint` and `npm run check` must exit clean before closing a change.
- **Always Svelte 5 runes:** `$props()` with `interface Props`, `$state`, `$derived`, `$effect`. No `export let` or `$:`. `{#each}` always keyed.
- **Server-only code under `$lib/server/`**; never imported from client code.
- SSR data via `load` in `+page.server.ts`/`+layout.server.ts` → `data` prop.
- Links to `+server.ts` endpoints (`/auth/login`, `/auth/logout`) carry `data-sveltekit-reload`.
- Reuse design-system tokens/recipes; extract to `$lib/components/ui/` whatever repeats.

## Folder structure

Full rationale and history in `docs/folder-structure-plan.md`.

- `src/lib/state/` — reactive rune-based state (`*.svelte.ts` with no other home; player state stays in `$lib/player/`, theme mode in `$lib/theme/`).
- `src/lib/data/` — client-side domain/data helpers (albums, collections, genres, mixes, recentlyPlayed, search).
- `src/lib/utils/` — generic, domain-agnostic utilities (e.g. `format.ts`).
- `src/lib/components/layout/` — app chrome (Sidebar, TopBar, TabsBar, MobileHeader).
- `src/lib/components/player/` — player UI (PlayerBar, PlayerDock, Queue, TrackInfo, TransportControls).
- `src/lib/components/ui/` — design-system components, split by category:
  - `primitives/` — generic controls with no music-domain knowledge (Button, Input, Chip, Slider, …).
  - `overlay/` — anything that floats above content (Modal, ContextMenu, AccountMenu, …).
  - `media/` — music-domain display components (Artwork, TrackList, MediaCard, PlayButton, …).
  - `forms/` — composite entity forms (AlbumForm, PlaylistForm, CoverForm, ImageDropzone).
  - `layout/` — page-level scaffolding (Page, PageHeader, SectionHeading) — distinct from `components/layout/`, which is the global app chrome.
- Auth routes live under `src/routes/auth/` (`+page.svelte` = login screen, `login/`, `logout/`, `callback/` = OIDC endpoints). Public user profile is `src/routes/(app)/user/[id]/`.

## Styles

The design system lives in `src/routes/layout.css` (`@theme` tokens + `@utility` recipes) and `src/lib/theme/` (`theme.css` for static neutrals, `tokens.ts` for user-hue-dependent dynamic tokens). `scripts/check-tokens.mjs` (`npm run lint:tokens`, part of `lint`) fails if `src/**/*.svelte` reintroduces what the `docs/frontend-refactor-plan.md` (P0–P14) refactor removed — read it before adding an arbitrary value or a "stock" radius/size.

- **Radii:** always `rounded-tag/thumb/control/art/art-lg/panel/panel-lg` (or `rounded-full` for pills/circles). Never `rounded-sm/md/lg/xl/2xl/3xl` or bare `rounded`.
- **Square sizes:** `size-*` instead of repeated `h-N w-N` (icons: `size-icon-xs/sm/md/lg/xl`; covers: `size-cover-xs…hero`).
- **Typography:** recipes `text-display-1/2/3/4`, `text-eyebrow`, `text-body`, `text-count`; tracking via `tracking-display`/`tracking-eyebrow`, not a loose `tracking-[…]`.
- **Spacing:** Tailwind scale in `.5` steps (avoid quarters like `.25`/`.75`; only acceptable if the semantic token calls for it, e.g. `mb-4.5` in `SectionHeading`).
- **Colors:** always `--mf-*`/`text-*`/`bg-*`/`border-*` from `theme.css`/`tokens.ts`. Never literal hex or `rgba()` in a `.svelte` file (those literals only exist as the source of truth inside `theme.css`/`tokens.ts`).
- **`style="…"` and arbitrary `-[…]` values:** only the whitelist in section B.4 of `docs/frontend-refactor-plan.md` (stagger `--i`, tile hue, menu position, dynamic widths/heights via `var(--mf-*)`, `text-[clamp(…)]`, `grid-cols-[auto_1fr]`, `max-h-[85dvh]`, `vh` paddings for vertical centering). Everything else goes to a token.
- Base components to reuse before creating a new one: `Artwork`, `Avatar`, `ListRow`, `MediaIdentity`, `TrackList`, `MediaCard`, `PageHeader`, `ContextMenu`, `ConfirmDialog`, `PlayButton`, `Slider`, `Logo`, `Chip`, `SegmentedControl`, `CoverForm`.
- Known, accepted gaps (documented in `scripts/check-tokens.mjs`): `strokeWidth={n}` on icons isn't unified (13 distinct values spread across almost every component; safely migrating it to `stroke-thin/regular/bold` recipes would require visually verifying that `lucide-svelte` respects `stroke-width` via CSS, so it was left out of that pass).

## Auth

`hooks.server.ts` validates the encrypted session cookie (`mf_session`), refreshes the `access_token`, and populates `locals.user` / `locals.accessToken`. To protect a route, check `locals.user` in its `load` and redirect to `/auth?returnTo=…`. For the backend, send `locals.accessToken` as a Bearer token to `API_BASE_URL`.

Environment variables in `.env` (see `.env.example`): `ZITADEL_*`, `AUTH_REDIRECT_URI`, `AUTH_POST_LOGOUT_URI`, `SESSION_SECRET`, `API_BASE_URL`.

## Backend API

Typed client via `openapi-fetch` over `Musify.Api`'s OpenAPI spec. In a server `load`/action:

```ts
import { createApiClient } from '$lib/server/api';
const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
const { data, error: err } = await api.GET('/tracks');
```

Returns `{ data, error }` (ErrorOr-style), never throws. Types live in `src/lib/api/schema.d.ts` and are regenerated with `npm run gen:api` (requires the dev API running at `API_BASE_URL`, which exposes `/openapi/v1.json` only in Development).
