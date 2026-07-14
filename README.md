# Handoff: Musify (App prototype)

## Overview
Musify is a music streaming app: no ads, upload your own files, import from other platforms, adaptive-bitrate HLS streaming, offline downloads, and user playlists. This bundle covers the **app prototype** only (`Musify.dc.html`) — the in-app experience (sidebar nav, home, search, library, upload, import, downloads, playlists, and a persistent bottom player). The marketing landing page is a separate deliverable and is not included here.

## About the Design File
`Musify.dc.html` is a **design reference built in HTML** — an interactive prototype showing the intended look, layout, and behavior. It is **not production code to copy directly**. The task is to **recreate this design in your real codebase** using your framework's component model and state management (stores, hooks, etc.) and your project's existing styling approach — not to embed the HTML as-is.

## Fidelity
**High-fidelity (hifi)**: colors, typography, spacing, and interactions are final-intent. Recreate pixel-close; adapt only where your framework's conventions differ (e.g. CSS scoping, component boundaries, routing).

## Design System
- **Background**: near-black `#08080a` throughout (not pure gray — avoid `#111`/`#222` grays; use this near-black plus translucent white overlays for elevation, e.g. `rgba(255,255,255,0.035)`–`rgba(255,255,255,0.09)`).
- **Accent color**: **dynamic** — derived from the currently-playing track's "hue" (`oklch(72% 0.17 <hue>)`), so the whole UI (nav highlight, progress bar, buttons, profile avatar) subtly recolors per song. There is no fixed brand accent in the app; drive this from real track metadata/album art in production (e.g. extracted dominant color), or fall back to a fixed brand purple if per-track theming isn't desired.
- **Typography**: headings in **Sora** (600–900 weight), body/UI text in **Manrope** (400–700 weight). Both loaded from Google Fonts.
- **Text color**: primary text `#f5f4f2`; secondary/subtitle text `rgba(245,244,242,0.7–0.85)` (kept fairly bright, not dimmed gray); tertiary/meta text down to `~0.4–0.55`.
- **Corners**: generous radii — 6–14px on small controls/rows, 100px (pill) on avatars/buttons/badges.
- **Cover art / track thumbnails**: gradient placeholders (`linear-gradient(135deg, oklch(70% 0.15 hue), oklch(36% 0.13 hue+28))`) with a diagonal repeating-stripe overlay — stand-ins for real album art (network image).
- **Icons**: all hand-built from divs (no icon font/SVG library) — simple geometric shapes (house, magnifying glass, bars, triangles, circles). Recreate as inline SVG or an icon library for maintainability.
- **Layout philosophy**: content areas fill the available width (search results, library list, playlist grids, "acceso rápido" grid all stretch to 100%/auto-fill rather than being capped at a narrow max-width) — avoid re-introducing narrow `max-width` caps when porting.

## Layout
CSS grid, `grid-template-columns: 264px 1fr`, `grid-template-rows: 1fr 92px`, `grid-template-areas: 'sidebar main' 'player player'`, fixed to viewport (`position:fixed; inset:0`).

### Sidebar (264px, left)
- Wordmark "Musify" (Sora 800, 20px) top-left — no icon/logo mark, text only.
- Nav list: Inicio (house icon), Buscar (magnifying-glass icon), Tu biblioteca (bars icon), Playlists (list icon); then a divider; then Subir música, Importar, Descargas. Active item: `rgba(255,255,255,0.08)` background pill, icon/text tinted with the current accent color; inactive: `rgba(245,244,242,0.5–0.62)`.
- Footer: profile row — circular avatar (gradient, offset hue from current track) + name "Alex Rivera" + "@alexrivera" handle, in a subtle rounded card, clickable (wire to a real account menu).

### Main content (right of sidebar, scrollable)
Seven views, toggled by sidebar nav (single active view at a time — replace with real routing):

1. **Inicio (Home)** — blurred color-wash hero (current track's gradient), greeting + headline "Tu música. Sin límites."; "Acceso rápido" grid (auto-fill, min 260px per card) of 6 track rows; "Recomendado para ti" horizontal scroll rail of 168×168 cards with a hidden-but-functional scrollbar.
2. **Buscar (Search)** — full-width search input filtering a track list live by title/artist; empty state message when no match. List and input both stretch to 100% width.
3. **Tu biblioteca (Library)** — full-width flat list of all tracks (built-in + uploaded), "Subida"/"Importada" source badges where applicable.
4. **Subir música (Upload)** — dashed drop-zone (click simulates an upload adding a fake track), list of uploaded tracks below.
5. **Importar (Import)** — 3 cards for external sources (cloud storage, external streaming account, other device), each with a "Conectar"/"Conectado" toggle pill.
6. **Descargas (Downloads)** — storage usage bar (used/2GB), list of tracks marked offline with per-track download progress (0–100%, simulated) and a circular toggle button.
7. **Playlists** — grid of playlist cards (2×2 color-mosaic thumbnail from track hues + name + track count) plus a "create playlist" input+button. Clicking a card opens **Playlist Detail**: editable title (inline text input), 2×2 mosaic art, track list with per-track remove (×), and an "Añadir canciones" list of tracks not yet in the playlist (click to add).

### Player bar (bottom, full width, 92px)
Same background treatment as sidebar. Fluid three-column layout (`minmax(200px,1fr) minmax(0,3fr) minmax(200px,1fr)`) so the transport/seek area grows on wide screens:
- Left: current track art (52px) + title/artist.
- Center: transport controls (prev/play-pause/next, hand-drawn triangles) and a seek bar — click-to-seek with a smooth animated fill (`transition: width 0.6s cubic-bezier(0.22,0.61,0.36,1)`), current/total time labels, now stretching up to 900px wide.
- Right: a thin custom volume slider styled to match the seek bar (not the default OS slider) — track is a gradient split at the volume %, small circular thumb via `::-webkit-slider-thumb`/`::-moz-range-thumb`.

## Interactions & Behavior
- **View switching**: clicking a sidebar item sets `state.view` and conditionally renders one of the 7 main-content sections; no real routing — replace with SvelteKit routes / React Router / etc.
- **Playback simulation**: a 300ms interval advances `progress` when `playing`; on reaching track duration it auto-advances to the next track and loops. Clicking a track toggles play/pause if it's already current, else switches to it and starts playing. Replace with a real `<audio>`/HLS.js player.
- **Seek**: click position on the progress bar computes a fraction of its width and sets `progress` to that fraction × duration; the fill animates smoothly rather than jumping.
- **Volume**: native `<input type="range">`, custom-styled; value drives a linear-gradient split on the track itself.
- **Offline downloads**: clicking the toggle starts a simulated download (progress increments ~14% every 300ms until 100%/"done"); clicking again while done removes it. Storage-used bar is derived from count of "done" downloads × ~0.18GB (placeholder math — replace with real file sizes).
- **Upload**: clicking the drop-zone appends a fake track to `state.uploads` (no real file picker/parsing) — wire up a real file input + upload pipeline.
- **Import**: toggling a service just flips a boolean per source — no real OAuth/integration.
- **Playlists**: create (name input + button) → adds a playlist and opens it; rename via inline text input on the detail header; delete button removes it and returns to the list; add/remove track buttons mutate `trackIds` on the playlist.

## State Management (what to model)
- `currentTrackId`, `playing`, `progress` (seconds), `volume` (0–100), `network` (currently cycled manually — in production drive from actual measured/adaptive HLS bitrate).
- `view` (active section) — replace with real routing.
- `tracks` (built-in + uploaded, merged) — replace with real catalog/uploaded-file data from your backend.
- `offline: Record<trackId, {status, percent}>` — replace simulated progress with real download-manager state.
- `playlists: {id, name, trackIds}[]`, `activePlaylistId`.

## Design Tokens
- **Colors**: background `#08080a`; text `#f5f4f2`; text-secondary `rgba(245,244,242,0.7–0.85)`; text-tertiary `rgba(245,244,242,0.4–0.55)`; surfaces `rgba(255,255,255,0.035–0.09)`. Accent computed at runtime: `oklch(72% 0.17 <trackHue>)`.
- **Typography**: Sora 600/700/800/900 for headings; Manrope 400/500/600/700 for body/UI.
- **Radii**: 6–14px (small UI), 100px (pills/avatars).
- **Spacing**: main content padding `44px 48px`; sidebar padding `26px 18px`.

## Assets
No external images — all art (track covers, avatar) is CSS-gradient placeholders. Replace with real album art / user avatar images before shipping. Icons are hand-built divs; consider swapping for an SVG icon set in your production build.

## Files
- `Musify.dc.html` — app prototype (self-contained; open directly in a browser).
- `support.js` — runtime helper used only by the HTML prototype format; **not needed** in your production port, it has no equivalent purpose in a real app.
