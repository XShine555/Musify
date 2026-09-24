# Musify, page guide

Living document describing what each page of the web frontend contains. The design system (tokens, recipes, components) lives in the `musify-web` skill and in `CLAUDE.md`; here we describe **content and structure**, not styling.

## Route structure

Everything the user sees after signing in lives under the `(app)` group (the group doesn't change the URL). `auth/` and `api/` sit outside it.

```
src/routes/
  (app)/
    +page.svelte                  /                      Home
    explore/                      /explore               Search and genre discovery
    liked/                        /liked                 Liked songs
    library/                      /library               Your uploaded songs
    upload/                       /upload                Upload a song
    albums/                       /albums                Your albums
      [id]/                       /albums/:id            Album detail
    playlists/                    /playlists             Your playlists
      [id]/                       /playlists/:id         Playlist detail
    mixes/[id]/                   /mixes/:id             Generated mix
    user/[id]/                    /user/:id              Public profile
      [list=followList]/          /user/:id/followers|following
  auth/  auth/login  auth/logout  auth/callback         Login screen and OIDC endpoints
  api/                                                   Same origin JSON endpoints used by the client
```

Global chrome on every app page: sidebar (desktop) or tab bar (mobile), top bar with search and account menu, the persistent player dock, and the queue panel.

## Pages

- **Home `/`**: a greeting, weekly listening stats, "continue listening" (recently played merged with the in-session history), the featured playlist with its first tracks (play and shuffle load the full list on demand), the user's mixes and playlists. Anonymous visitors are redirected to `/explore`.
- **Explore `/explore`**: genre tiles when there is no query, otherwise search results grouped by songs, albums, users and playlists with filter chips and counts taken from the API totals. Songs paginate with infinite scroll. Track and album context menus offer play next, add to queue and add to playlist.
- **Liked `/liked`**: the songs the user liked, newest first, with unlike per row.
- **Library `/library`**: the songs the user uploaded, with a confirmed delete.
- **Upload `/upload`**: audio, cover, title, genres (incompatible genres are blocked) and explicit flag. The server reserves presigned URLs, uploads both files and creates the track; processing happens asynchronously in the backend.
- **Albums `/albums`, `/albums/:id`**: list and create albums. The owner can edit, delete and add or remove tracks from their library.
- **Playlists `/playlists`, `/playlists/:id`**: list and create playlists. The owner can edit, delete and remove tracks; everyone else gets a read only view.
- **Mix `/mixes/:id`**: a generated mix with play, shuffle and a track menu.
- **Profile `/user/:id`**: public playlists, follow button and links to the followers and following lists, which respect the profile's privacy.
- **Auth `/auth`**: the login screen. Its buttons lead to `/auth/login`, which starts the Zitadel OIDC flow; `?mode=register` opens the sign up variant and `?returnTo=` (local paths only) sends the user back afterwards. `/auth/logout` ends the session and `/auth/callback` creates the encrypted `mf_session` cookie.

## Data flow

Each page loads its data in `+page.server.ts` through the typed API client, and normalizes it with the mappers in `$lib/server/mappers.ts` so the client only ever sees the domain types in `$lib/types.ts`. Mutations are form actions built with `authedAction`. Client side pagination goes through the `/api/*` endpoints.

## Pending backend work

- Sorting playlists by last listen (`lastPlayedAt`) for a "recently played playlists" shelf.
- A bulk endpoint to add several tracks to a playlist at once (adding an album currently posts one track at a time).
- An endpoint returning the most listened tracks, to bring back a "popular" section on Home.
