# Musify: web-player

Musify's public web app (`web-player/` folder). For design and UI recipes, use the **`musify-web`** skill.

## Stack

SvelteKit 2 (`adapter-node`), Svelte 5 **runes**, Tailwind 4, TypeScript strict, Zitadel OIDC auth (`openid-client` + `jose`).

There's no `svelte.config.js` (config is inline in `vite.config.ts`) and no `tailwind.config.js` (config lives in `src/routes/layout.css`).

## Commands

```sh
npm run dev          # dev server (port 5173)
npm run build
npm run check         # svelte-check, must exit 0 errors before closing a change
npm run format         # prettier --write .
npm run lint            # prettier --check . && eslint . && npm run lint:tokens
npm run lint:tokens      # scripts/check-tokens.mjs, the design system guardrail (see below)
```

## Rules

- **No comments** in code unless essential.
- `npm run lint` and `npm run check` must exit clean before closing a change.
- **Always Svelte 5 runes:** `$props()` with `interface Props`, `$state`, `$derived`, `$effect`. No `export let` or `$:`. `{#each}` always keyed.
- **Server only code goes under `$lib/server/`**, and it's never imported from client code.
- SSR data comes through `load` in `+page.server.ts`/`+layout.server.ts` and lands in the `data` prop.
- Links to `+server.ts` endpoints (`/auth/login`, `/auth/logout`) carry `data-sveltekit-reload`.
- Reuse design system tokens and recipes before writing new CSS. If something repeats, extract it to `$lib/components/ui/`.

## Folder structure

- `src/lib/state/` holds reactive rune based state (`*.svelte.ts` files that don't have a more specific home). Player state stays in `$lib/player/`, theme mode in `$lib/theme/`.
- `src/lib/data/` holds client side domain helpers: albums, collections, genres, mixes, recentlyPlayed, search.
- `src/lib/utils/` holds generic utilities with no domain knowledge, like `format.ts`.
- `src/lib/components/layout/` holds the app chrome: Sidebar, TopBar, TabsBar, MobileHeader.
- `src/lib/components/player/` holds the player UI: PlayerBar, PlayerDock, Queue, TrackInfo, TransportControls.
- `src/lib/components/ui/` holds the design system components, split into five categories:
  - `primitives/` for generic controls with no music domain knowledge (Button, Input, Chip, Slider, and so on).
  - `overlay/` for anything that floats above the content (Modal, ContextMenu, AccountMenu, and so on).
  - `media/` for music domain display components (Artwork, TrackList, MediaCard, PlayButton, and so on).
  - `forms/` for composite entity forms (AlbumForm, PlaylistForm, CoverForm, ImageDropzone).
  - `layout/` for page level scaffolding (Page, PageHeader, SectionHeading). This is different from `components/layout/` above, which is the global app chrome.
- Auth routes live under `src/routes/auth/`: `+page.svelte` is the login screen, and `login/`, `logout/`, `callback/` are the OIDC endpoints. The public user profile route is `src/routes/(app)/user/[id]/`.

## Styles

The design system lives in `src/routes/layout.css` (`@theme` tokens plus `@utility` recipes) and in `src/lib/theme/` (`theme.css` for the static neutrals, `tokens.ts` for the dynamic tokens that depend on the user's hue). `scripts/check-tokens.mjs` (run as `npm run lint:tokens`, part of `lint`) fails on a stock radius, an `h-N w-N` pair, fractional quarter spacing, a literal hex/`rgba()`, or an arbitrary `-[…]` value outside its whitelist, so check that script before adding one.

- **Radii:** always `rounded-tag/thumb/control/art/art-lg/panel/panel-lg` (or `rounded-full` for pills and circles). Never `rounded-sm/md/lg/xl/2xl/3xl` or a bare `rounded`.
- **Square sizes:** use `size-*` instead of repeating `h-N w-N` (icons: `size-icon-xs/sm/md/lg/xl`; covers: `size-cover-xs` through `size-cover-hero`).
- **Typography:** use the recipes `text-display-1/2/3/4`, `text-eyebrow`, `text-body`, `text-count`, with tracking through `tracking-display`/`tracking-eyebrow` rather than a loose `tracking-[…]`.
- **Spacing:** stick to the Tailwind scale in `.5` steps and avoid quarters like `.25`/`.75`, unless the semantic token calls for it (for example `mb-4.5` in `SectionHeading`).
- **Colors:** always use `--mf-*`/`text-*`/`bg-*`/`border-*` from `theme.css`/`tokens.ts`. Never a literal hex or `rgba()` inside a `.svelte` file; those literals only belong in `theme.css`/`tokens.ts` as the source of truth.
- **Inline `style="…"` and arbitrary `-[…]` values:** only the ones whitelisted in `scripts/check-tokens.mjs` (the stagger variable `--i`, tile hue, menu position, progress width, `text-[clamp(…)]`, `leading-[…]` on login's display type, `grid-cols-[auto_1fr]`, `max-h-[85dvh]`, `my-`/`py-[Ndvh]` for vertical centering, `transition-[filter]` on genre tiles, and `bg-[image:var(--mf-*)]`). Everything else should become a token.
- Reach for these base components before creating a new one: `Artwork`, `Avatar`, `ListRow`, `MediaIdentity`, `TrackList`, `MediaCard`, `PageHeader`, `ContextMenu`, `ConfirmDialog`, `PlayButton`, `Slider`, `Logo`, `Chip`, `SegmentedControl`, `CoverForm`.
- Known and accepted gap (documented in `scripts/check-tokens.mjs`): icon `strokeWidth={n}` isn't unified yet. There are 13 different values spread across almost every component, and migrating them to `stroke-thin/regular/bold` recipes safely would require visually confirming that `lucide-svelte` actually respects `stroke-width` through CSS, so that was left out of the last pass.

## Auth

`hooks.server.ts` validates the encrypted session cookie (`mf_session`), refreshes the `access_token`, and fills `locals.user` and `locals.accessToken`. To protect a route, check `locals.user` in its `load` and redirect to `/auth?returnTo=…`. When talking to the backend, send `locals.accessToken` as a Bearer token to `API_BASE_URL`.

Environment variables live in `.env` (see `.env.example`): `ZITADEL_*`, `AUTH_REDIRECT_URI`, `AUTH_POST_LOGOUT_URI`, `SESSION_SECRET`, `API_BASE_URL`.

## Backend API

Typed client via `openapi-fetch` over the `Musify.Api` OpenAPI spec. Inside a server `load` or action:

```ts
import { createApiClient } from '$lib/server/api';
const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
const { data, error: err } = await api.GET('/tracks');
```

It returns `{ data, error }` (ErrorOr style) and never throws. The types live in `src/lib/api/schema.d.ts` and get regenerated with `npm run gen:api` (this needs the dev API running at `API_BASE_URL`, which only exposes `/openapi/v1.json` in Development).
