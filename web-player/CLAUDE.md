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
npm run test            # vitest run (pure functions, mappers, form parsers)
npm run lint            # prettier --check . && eslint . && npm run lint:tokens && npm run test
npm run lint:tokens      # scripts/check-tokens.mjs, the design system guardrail (see below)
```

## Rules

- **No comments** in code unless essential.
- `npm run lint` and `npm run check` must exit clean before closing a change.
- **Imports:** always `$lib/...`, never `../` (ESLint enforces it). Non-null assertions are forbidden too.
- **Always Svelte 5 runes:** `$props()` with `interface Props`, `$state`, `$derived`, `$effect`. No `export let` or `$:`. `{#each}` always keyed.
- **Server only code goes under `$lib/server/`**, and it's never imported from client code.
- SSR data comes through `load` in `+page.server.ts`/`+layout.server.ts` and lands in the `data` prop.
- Links to `+server.ts` endpoints (`/auth/login`, `/auth/logout`) carry `data-sveltekit-reload`.
- Reuse design system tokens and recipes before writing new CSS. If something repeats, extract it to `$lib/components/ui/`.

## Folder structure

- `src/lib/types.ts` holds the domain types (`Track`, `Album`, `Playlist`, `Mix`, `Paged<T>`). Pages and the player only ever see these, never raw API DTOs.
- `src/lib/player/` is the player: `player.svelte.ts` (state and orchestration), `queue.ts` (pure queue operations), `stream.ts`, `mediaSession.ts`, `progressClock.ts`, `listenTracker.ts`, `liked.svelte.ts`, and `actions.ts` (`playAllOrToggle`, `playShuffled`, `isQueueCurrent`).
- `src/lib/state/` holds reactive rune state: `panels.svelte.ts` (queue panel, create playlist modal), `history.svelte.ts` (previous page for `BackLink`), `menu.svelte.ts` (`createMenu<T>()`), `pagedList.svelte.ts` (`createPagedList`), `dialog.svelte.ts`, and `scroll.ts`. Nothing in `state/` imports from `components/`.
- `src/lib/theme/` holds the theme: `accent.svelte.ts` (accent hue per track and the hue fade), `mode.svelte.ts`, `palette.ts`, `tokens.ts`.
- `src/lib/data/` holds client side domain helpers: albums, genres, recentlyPlayed, search.
- `src/lib/utils/` holds generic utilities: `format.ts`, `collections.ts`, `hrefs.ts` (search, genre and cover URLs), `menuPosition.ts`, `storage.ts`, `transitions.ts`.
- `src/lib/validation.ts` holds the form limits (`LIMITS`) shared by client forms and server parsers.
- `src/lib/server/` is server only code (see below).
- `src/lib/components/layout/` holds the app chrome: Sidebar, TopBar, TabsBar, MobileHeader, and `navLinks.ts`.
- `src/lib/components/player/` holds the player UI: PlayerBar, PlayerDock, Queue, TrackInfo, TransportControls.
- `src/lib/components/ui/` holds the design system components, split into five categories:
  - `primitives/` for generic controls with no music domain knowledge (Button, Input, Chip, Slider, and so on).
  - `overlay/` for anything that floats above the content (Modal, ContextMenu, TrackContextMenu, AlbumContextMenu, AccountMenu, and so on).
  - `media/` for music domain display components (Artwork, TrackList, MediaCard, PlayButton, PlayAllButton, and so on).
  - `forms/` for composite entity forms (AlbumForm, PlaylistForm, CoverForm, ImageDropzone).
  - `layout/` for page level scaffolding (Page, PageHeader, SectionHeading). This is different from `components/layout/` above, which is the global app chrome.
- Everything the user sees after signing in lives under `src/routes/(app)/`, including Home (`(app)/+page.svelte`). Auth routes live under `src/routes/auth/`: `+page.svelte` is the login screen, and `login/`, `logout/`, `callback/` are the OIDC endpoints. The public user profile route is `src/routes/(app)/user/[id]/`, and its followers/following lists share one route, `user/[id]/[list=followList]/` (matcher in `src/params/followList.ts`).

## Naming conventions and glossary

- Variant maps are module level constants in upper case (`SIZE`, `VARIANT`).
- Errors: destructure as `const { data, error } = ...` and name the caught value `err` in `catch (err)`.
- Play handlers: `playFrom(index)` and `playAll()`. Form actions: `create`, `edit`, `delete`, `addTrack`, `removeTrack`.
- UI copy: "Playlist" (never "Lista"), "Mezcla" (never "Mix"), "Modo claro". Total durations always go through `fmtDurationLong`.

## Styles

The design system lives in `src/routes/layout.css` (`@theme` tokens plus `@utility` recipes) and in `src/lib/theme/` (`theme.css` for the static neutrals, `tokens.ts` for the dynamic tokens that depend on the user's hue). `scripts/check-tokens.mjs` (run as `npm run lint:tokens`, part of `lint`) fails on a stock radius, an `h-N w-N` pair, fractional quarter spacing, a literal hex/`rgba()`, or an arbitrary `-[…]` value outside its whitelist, so check that script before adding one.

- **Radii:** always `rounded-tag/thumb/control/art/art-lg/panel/panel-lg` (or `rounded-full` for pills and circles). Never `rounded-sm/md/lg/xl/2xl/3xl` or a bare `rounded`.
- **Square sizes:** use `size-*` instead of repeating `h-N w-N` (icons: `size-icon-xs/sm/md/lg/xl`; covers: `size-cover-xs` through `size-cover-hero`).
- **Typography:** use the recipes `text-display-1/2/3/4`, `text-eyebrow`, `text-body`, with tracking through `tracking-display`/`tracking-eyebrow` rather than a loose `tracking-[…]`.
- **Spacing:** stick to the Tailwind scale in `.5` steps and avoid quarters like `.25`/`.75`, unless the semantic token calls for it (for example `mb-4.5` in `SectionHeading`).
- **Colors:** always use `--mf-*`/`text-*`/`bg-*`/`border-*` from `theme.css`/`tokens.ts`. Never a literal hex or `rgba()` inside a `.svelte` file; those literals only belong in `theme.css`/`tokens.ts` as the source of truth.
- **Inline `style="…"` and arbitrary `-[…]` values:** only the ones whitelisted in `scripts/check-tokens.mjs` (the stagger variable `--i`, tile hue, menu position, progress width, `text-[clamp(…)]`, `leading-[…]` on login's display type, `grid-cols-[auto_1fr]`, `max-h-[85dvh]`, `my-`/`py-[Ndvh]` for vertical centering, `transition-[filter]` on genre tiles, and `bg-[image:var(--mf-*)]`). Everything else should become a token.
- Reach for these base components before creating a new one: `Artwork`, `Avatar`, `ListRow`, `MediaIdentity`, `TrackList`, `MediaCard`, `PageHeader`, `ContextMenu`, `ConfirmDialog`, `PlayButton`, `Slider`, `Logo`, `Chip`, `SegmentedControl`, `CoverForm`.
- **Icon stroke:** one global rule (`svg.lucide { stroke-width: 1.5 }` in `layout.css`) sets the stroke for every icon. Never pass `strokeWidth` to an icon, since any CSS rule overrides the SVG attribute and it would do nothing. For a different stroke on one icon, use a Tailwind `stroke-*` utility.

## App shell

The shell in `src/routes/+layout.svelte` is a fixed `h-dvh` frame: Sidebar | (content column over PlayerDock) | Queue. Only the content column (`app-scroll`) scrolls, so nothing ever passes under the dock and the chrome can stay transparent over `app-backdrop`. Because the window never scrolls, scroll reset/restore on navigation is handled by `restoreScroll()` in `$lib/state/navigation.svelte.ts`; don't use `window.scrollTo`/`scrollY`.

## Motion

Every animation follows one scale of three durations and one easing curve, all defined in `theme.css`. Fast (`--mf-motion-fast`, 150ms) is for hover, focus and press feedback: color, opacity and small scale changes. Plain `transition` or `transition-colors` already uses it because it is the Tailwind default, so never write `duration-150`. Base (`--mf-motion-base`, 200ms) is for things that appear on top of the page or move inside a control: modals, menus, the queue panel, `animate-pop`, `animate-fade` and the segmented control thumb. Slow (`--mf-motion-slow`, 300ms) is for content entering the page and larger movement: `animate-enter`, cover zoom on hover.

Entrances and movement always use `--mf-ease` (`ease-snappy` in Tailwind, `expoOut` in Svelte transitions), which feels immediate because most of the motion happens at the start. Color transitions keep the Tailwind default curve. Overlays must feel instant: the modal backdrop uses `transition:fade={{ duration: 200 }}` and the panel uses a local `pop` transition (opacity + scale + translateY, `expoOut`, 200ms) mirroring `animate-pop`'s keyframes — both directives so the modal fades and pops on close too, not just on open, and the whole thing settles in 200ms. Staggered lists use `--i` with 40ms steps capped at ten items.

The only exceptions are looping indicators (equalizer bars, pulse, spin, sheen) and the accent hue crossfade in `+layout.svelte` (a random 2 to 4 seconds per song change), which is ambient and meant to be slow. Do not add new durations or curves; if something needs a different feel, pick the closest step of the scale. `prefers-reduced-motion` disables everything globally in `layout.css`.

## Auth

`hooks.server.ts` validates the encrypted session cookie (`mf_session`), refreshes the `access_token`, and fills `locals.user` and `locals.accessToken`. To protect a route, check `locals.user` in its `load` and redirect to `/auth?returnTo=…`. When talking to the backend, send `locals.accessToken` as a Bearer token to `API_BASE_URL`.

Environment variables live in `.env` (see `.env.example`): `ZITADEL_*`, `AUTH_REDIRECT_URI`, `AUTH_POST_LOGOUT_URI`, `SESSION_SECRET`, `API_BASE_URL`.

## Backend API

Typed client via `openapi-fetch` over the `Musify.Api` OpenAPI spec. Inside a server `load` or action use the helpers in `$lib/server/api.ts`:

```ts
const api = apiFor(event); // or apiFor({ fetch, locals }); adds the bearer token when there is a session
const { data, error } = await api.GET('/tracks');
```

It returns `{ data, error }` (ErrorOr style) and never throws except on network errors. The types live in `src/lib/api/schema.d.ts` and get regenerated with `npm run gen:api` (this needs the dev API running at `API_BASE_URL`, which only exposes `/openapi/v1.json` in Development).

Server helpers:

- `mappers.ts` (`toTrack`, `toAlbum`, `toPlaylist`, `toMix`, `toPage`, ...) turn DTOs into the domain types. Every load and `/api` endpoint that returns tracks, albums or playlists goes through them, so `Number()` conversions never reach the client.
- `authedAction(handler)` wraps a form action: it returns 401 without a token, reads the form and hands `{ api, form, params, locals, url }` to the handler. Use `formString`/`formFile` to read fields, `failOnError(result, message)` to turn an API error into a `fail(...)`, and `requireData(result, message)` when you also need the response data narrowed.
- `forms/` holds the form parsers (`parseAlbumForm`, `parsePlaylistForm`, `parseTrackUploadForm`); they validate with `LIMITS` from `$lib/validation`. `uploadOptionalCover` handles the optional cover upload.
- `genres.ts` caches the genre list; `playbackConfig.ts` caches the anonymous listening setting.
- Page sizes come from `$lib/config`; do not hardcode them.
