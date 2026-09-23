# Musify, page guide

Living document describing what each page of the web frontend should contain. It works as a map for prioritizing features. The design system (tokens, recipes, components) lives in the `musify-web` skill; here we describe **content and structure**, not styling.

Status legend: ✅ done · 🟡 partial / mockup · ⬜ pending.

> **Note (app redesign):** the `Musify.dc.html` prototype is being ported over (an app with a sidebar, views, and a persistent player with a dynamic accent per track). The marketing landing page was removed. The sections below still describe the older design and are pending an update to match the app views (Home, Search, Library, Upload, Import, Downloads, Playlists). The **player** is already built as components in `$lib/components/player/`, with its theme in `$lib/theme/` (`theme.css` for the palette, `color.ts` for the accent and gradient).

## Home (app) `/` 🟡

- **Hero**: a greeting plus "Your music. No limits." with a color wash taken from the current track's hue.
- **Quick access, meaning recently played** (`GET /users/{id}/listening-history` merged with `player.recentlyPlayed` via `mergeRecentlyPlayed`). Empty state with an icon and an "Explore" CTA.
- **Your playlists**: mosaic cards for the user's playlists (sample data for now). Has an empty state.
- **Resume on load**: the root layout fetches `GET /users/{id}/last-listened-track` and hydrates the player (`player.hydrate`) with it, paused, so the player bar isn't empty on a fresh session. Only applies when nothing is already loaded (won't override an in-progress session).

### Pending backend work (ASP.NET) for Home

- **Playlists by last listen**: add `lastPlayedAt` to the playlist (the schema currently only has `createdAt`/`updatedAt`) and allow sorting `GET /playlists/users/{userId}` by that field (for example `?sort=lastPlayed`), to show "your most recently played" playlists.
- Tracks will need an **artist** and a **color/hue** (or a real cover to extract one from) for the artwork and the dynamic accent. Right now `TrackApplicationResponse` carries neither artist nor color.

## Route structure

Routes are grouped to separate the public experience from the authenticated one (the groups don't change the URL):

```
src/routes/
  (marketing)/        Public, no session required
    +page.svelte        /            Landing            ✅
  (app)/              The app experience
    explore/            /explore     Explore / search    ✅
    upload/             /upload      Upload music         🟡 mockup
    library/             /library     Your library          ⬜
    playlists/            /playlists   Your playlists         ⬜
      [id]/                /playlists/:id  Playlist detail    ⬜
  auth  auth/login  auth/logout  auth/callback  Auth (page + endpoints)   ✅
  api/tracks/[id]/stream             Stream proxy         ✅
```

Global components (present on every page): **Navbar**, **Footer**, and **PlayerBar** (the fixed playback bar, shown while something is playing).

---

## 1. Landing `/` ✅ (marketing)

The entry page for logged out visitors. Its job is to sell the product and drive people to sign up or explore.

- **Hero**: a status badge (beta), a title, a subtitle, a primary CTA (Start for free, leading to sign up) and a secondary one (Explore songs).
- **Features**: three cards covering listening, playlists, and uploading.
- **Suggested next steps**: a screenshots or demo section, social proof, a short FAQ.

## 2. Login / Auth ✅ (endpoints)

`/auth` is Musify's login screen; its buttons lead to `/auth/login`, which kicks off the Zitadel OIDC flow and redirects. `?mode=register` starts on the sign up variant, and `?returnTo=` sends the user back to the route they came from once they're in. `/auth/logout` ends the session. `/auth/callback` exchanges the code and creates the encrypted `mf_session` cookie. The credentials UI itself is served by Zitadel, not by Musify.

## 3. Explore / Search `/explore` ✅ (app)

Discovery and search over the catalog.

- **Search box**: a text input (`?q=`) that filters by name on the backend.
- **Results count** and a **track grid** (2 to 4 columns): cover, title, date, and a play/pause button on hover that queues and plays through the global player.
- **Pagination**: previous/next (`?page=`, 24 per page).
- **Empty state**: no results, or an empty catalog.
- **Suggested next steps**: filters (most recent, by user), loading skeletons, a real track cover (the listing API doesn't currently return the image key).

## 4. Upload music `/upload` 🟡 mockup (app)

Adding a new song. The real backend flow, for when it gets wired up:

1. `POST /tracks/upload-urls` returns presigned URLs for cover and audio, plus `intentId`s.
2. `PUT` each file to its presigned URL (a direct upload to storage).
3. `POST /tracks` with `title`, `pictureIntentId`, `audioIntentId`.
4. The backend processes the audio asynchronously (transcoding it to DASH).

What the page needs:

- **Audio zone**: drag and drop or a file picker, showing the file's name and size, with type (audio) and size validation.
- **Cover**: an image picker with a square preview.
- **Metadata**: title (required), with room for future fields like a description.
- **Visible pipeline**: the Upload, Process, Publish steps and their status.
- **Progress bar** for the upload and the processing state.
- **States**: empty, file selected, uploading, processing, published, error.
- **Requires a session** (once wired up: redirect to `/auth?returnTo=/upload`).

## 5. Your library `/library` ⬜ (app)

The authenticated user's personal panel, everything of theirs in one place.

- **Tabs / sections**: "Your songs" (`GET /tracks/users/{userId}`) and "Your playlists" (`GET /playlists/users/{userId}`).
- **Your songs**: a list with play, plus management actions (delete via `DELETE /tracks/{id}`, add to a playlist).
- **Your playlists**: a grid of covers with a "Create playlist" button.
- **Empty state** with a CTA to upload or create.
- **Requires a session.**

## 6. Playlists `/playlists` ⬜ (app)

A listing of the user's playlists.

- **Grid** of playlists (cover, name, track count), linking to the detail page.
- **Create playlist**: a modal or page with a name, description, and cover (`POST /playlists`, cover via `POST /playlists/upload-picture`).
- **Empty state** with a CTA.
- **Requires a session.**

## 7. Playlist detail `/playlists/[id]` ⬜ (app)

- **Header**: a large cover, name, description, track count, and a "Play all" button.
- **Track list** (`GET /playlists/{id}/tracks`, paginated): index, title, date, play, and remove from the playlist (`DELETE /playlists/{id}/tracks/{trackId}`).
- **Owner actions**: edit (`PUT`), delete (`DELETE`) the playlist, reorder (a future addition).
- **Add tracks**: from explore or the library (`POST /playlists/{id}/tracks/{trackId}`).

## 8. User profile `/user/[id]` ⬜ (future)

A public view of a user: avatar, name, their public songs and playlists (`GET /users/{id}`, `GET /tracks/users/{id}`, `GET /playlists/users/{id}`).

---

## Global player (PlayerBar) ✅

A fixed bottom bar, visible while something is playing. Shows track info, transport controls (previous, play/pause, next), a progress bar, and volume control. Queues lists from any grid (`player.playQueue`). Plays DASH through the `/api/tracks/:id/stream` proxy, which requests a manifest and a signed ticket from the backend.

## Available backend (summary)

| Resource  | Endpoints                                                         |
| --------- | ----------------------------------------------------------------- |
| Tracks    | list/search, detail, by user, stream, create, delete, upload-urls |
| Playlists | CRUD, playlist tracks, add/remove track, upload-picture           |
| Users     | list/search, detail, create                                       |

Typed client in `$lib/server/api.ts` (`openapi-fetch`, ErrorOr style). Use it inside `load`/server actions, never from the client. Types live in `src/lib/api/schema.d.ts`.
