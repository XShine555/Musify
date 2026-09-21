# Audio streaming

## Goal

Play audio **without the bytes passing through the API**, while still controlling access. The API only issues a short-lived permission; the reverse proxy serves the bytes from SeaweedFS.

## Format: a single AAC file (.m4a), served via HTTP range

Audio is transcoded into **one `audio.m4a`** (AAC, MP4 container with `+faststart` so the `moov` atom sits at the start and range-based seeking works), stored under `Tracks/ProcessedAudios/{AudioFolderName}/`. There's no manifest or segments: the player downloads the file via **HTTP range requests** (the same model Spotify uses for on-demand music).

Multi-bitrate isn't implemented yet; the path to add it is transcoding several qualities (`audio_96.m4a`, …) and letting the client pick one at playback start (no seamless mid-track ABR, same as Spotify).

## Access control: a prefix-scoped stream ticket

Access is authorized for **the track's folder prefix** via a short-lived *stream ticket*, validated by the proxy on every request. Even though there's only one object today, the ticket is still scoped to the prefix `Tracks/ProcessedAudios/<folder>/` (covering the current file and any future quality variants).

## Flow

1. `GET /tracks/{id}/stream` (authenticated) → the API authorizes the request and returns:
   ```json
   { "manifestUrl": "http://<gateway>/media/Tracks/ProcessedAudios/<folder>/audio.m4a",
     "ticket": "<JWT RS256>", "expiresInSeconds": 3600 }
   ```
   The ticket carries the claim `prefix = "Tracks/ProcessedAudios/<folder>/"`.
2. The player opens the audio URL with `?t=<ticket>` appended (or the `X-Stream-Ticket` header).
3. The **StreamingGateway** (YARP) validates the ticket and forwards the request to the filer; the bytes flow SeaweedFS → client (with range/seek support).

## The ticket (RS256)

- Signed by the API with a **private** key (`StreamTicketService`); validated by the gateway with the **public** one (`TicketValidator`). That way the gateway **can't issue** tickets, only verify them.
- Claims: `sub`, `prefix`, `aud = media-gateway`, `iss = musify-webapi`, `exp` (~1h TTL).

**Why RS256 (asymmetric)**: it separates responsibilities. The issuer (API) and the verifier (gateway) don't share a secret, so even if the gateway is compromised, it can't mint permissions.

## The gateway (validation + proxy)

`TicketValidationMiddleware`, on every request to `/media/**`:
1. Reads `?t=`, validates its signature/aud/iss/exp.
2. Checks that the requested key **starts with** the ticket's `prefix`. If not → 403.
3. Strips the `?t=` and passes the request on to YARP, which rewrites `/media/{key}` → `/buckets/webapi-storage/{key}` for the filer.

- No ticket → **401**; outside the prefix → **403**; ok → **200** (and **206** for range requests, so seeking works).
- Passes through `Range`/`Accept-Ranges` and exposes those headers via CORS.

## One ticket per track?

Yes. Each ticket is scoped to **one** folder (one track). Playing a different one means requesting a new ticket. It's cheap (the API just signs a JWT) and it's the safest option: every permission only unlocks what you're about to play.

- If that ever becomes a problem (very frequent track switching), the same `prefix` claim mechanism can widen the scope to a whole playlist or the whole library (`Tracks/ProcessedAudios/`). It's a security/convenience trade-off. The default is **per track**.

## Open items / security notes

- Today, the policy for who can request a ticket is "any authenticated user" (TODO: restrict it with `UserHasTrack`/visibility).
- The prefix check is a literal `StartsWith`; it would be worth hardening it against `..` path traversal, even though browsers and ASP.NET normalize `..`.
- The ticket travels in the query string (`?t=`); the gateway strips it before forwarding, so it never reaches the filer's logs.
