# Storage (SeaweedFS / S3)

## What it is

**SeaweedFS** is the object store, S3-API compatible. It holds the uploaded originals and their derivatives (`.m4a` audio, thumbnails). Accessed via `AWSSDK.S3` from `StorageService` (`IStorageService`).

It exposes:
- **S3 API** (`:8333`): S3 operations (presigned URLs, put/get/list…).
- **filer HTTP** (`:8888`): serves files by path, used by the **StreamingGateway**.
- master (`:9333`) and volume server, internal only.

App bucket: **`webapi-storage`** (`ApplicationStorage:Bucket`). Important: the API and the Worker use **the same bucket** (the worker processes whatever the API uploads).

## Key layout

The layout is configured per owner (the `Track`, `PlayList` and `Album` sections, `Routes`); `ParentFolder` is `Tracks`, `PlayLists` or `Albums`.

- `{TempRootPrefix}/{userId}/{ParentFolder}/{guid}.{ext}`: temporary uploads, the target of presigned PUTs, before the intent is consumed (`UploadIntent:TempRootPrefix`, default `temp`; objects under the old `temporal/` prefix from earlier versions may need a one-off clean-up).
- `uploads/{userId}/{ParentFolder}/OriginalPictures|OriginalAudios/...`: originals that have already been consumed.
- `{ParentFolder}/SmallPictures|MediumPictures|LargePictures/...`: generated thumbnails.
- `Tracks/ProcessedAudios/{AudioFolderName}/`: transcoded audio (`audio.m4a`, AAC with faststart).

Keys are always built with the `StorageKey.Combine(...)` helper (in `Application/Shared`), which joins segments with `/` and trims stray spaces and slashes. It's centralized to keep the logic consistent and avoid repeating it.

> In the filer, S3 objects live under `/buckets/{bucket}/{key}`, which is why the gateway rewrites `/media/{key}` → `/buckets/{bucket}/{key}` (the bucket comes from the compose override `S3_BUCKET`; the default in the gateway's `appsettings.json` is the placeholder `CHANGE_ME`).

## Presigned URLs

To **avoid** routing large files through the API, the client talks to S3 directly using short-lived signed URLs:

- **Uploads (PUT)**: `RequestTrackUploadUrls` / `RequestAlbumPictureUpload` / `RequestPlayListPictureUpload` (issued by `UploadIntentIssuer`) generate presigned PUTs (with signed `Content-Type` and `If-None-Match: *`, ~120s expiry). The client uploads straight to S3.
- **Images (GET)**: thumbnails are served via presigned GETs (one object = one URL).

**Why presigned instead of proxying**: routing a ~100 MB audio file through the API would burn memory and bandwidth on the application servers and wouldn't scale well. A direct presigned PUT to S3 is the standard, correct pattern here.

### Upload intents and quota
Every `upload-urls` call creates **UploadIntents** (reservations in the `Issued` state) and returns the URLs. They're **consumed** when the track/playlist is created. There's a per-user quota (`MaxActiveUploadIntentsPerUser`, byte limits…) to prevent abuse. The `UploadIntentExpirationJob` (a Hangfire job on the Worker) marks expired intents and frees up quota, and `TempUploadsCleanupJob` deletes abandoned temporary objects. If the Worker isn't running, stale intents pile up and block new uploads. An intent is bound to its purpose (track picture/audio, album picture, playlist picture) and the same intent can't be used for two files.

## What's public and what's private (deployment)

| Expose to the internet | Keep private |
|---|---|
| S3 API `:8333` (behind TLS), needed for the client's presigned PUT/GET | filer `:8888` (internal network, gateway only) |
| StreamingGateway `:8081` | master `:9333`, volume server, admin UI |

- **Audio** is served through the gateway, so SeaweedFS can stay private for that flow.
- **Uploads and images** use presigned URLs, so the client talks to the S3 endpoint directly and `:8333` has to be reachable (though useless without a valid signature, this is the standard model for any S3/MinIO bucket).
- In **production**: `InfrastructureStorage:Address` must be the **public S3 endpoint** (the signature is computed for that host). If the servers can't reach it internally, use split config: public host for signing, internal host for server-side operations.

**Rule of thumb**: the gateway, and at most the signed `:8333`, are the only public surface. Master, volume, filer and admin are **never** exposed.
