# Media processing (Worker + MassTransit)

Heavy processing (audio transcoding, thumbnails, bucket transfers) happens **outside the HTTP request**, in the **Worker**, orchestrated with **MassTransit** on top of RabbitMQ.

## Why async, and why routing slips

- Transcoding audio and resizing images is CPU/IO-intensive: it shouldn't block an API call.
- It's a **multi-step process with side effects** (download from S3 → transcode → upload results → update the DB). If a step fails, the previous ones need to be **undone**. That's what **routing slips** are for (MassTransit's Saga pattern): each step is an *activity* with its own **compensation**.
- **Transactional outbox** (EF + MassTransit): the event is published consistently with the DB change (never lost, never duplicated).

## Trigger

When a track, album or playlist is created, updated or deleted, the handler publishes an event (e.g. `CreateTrackResourcesEvent`, `UpdateAlbumPictureSourceEvent`, `DeleteAlbumEvent`). A **consumer** (`CreateTrackConsumer`, `UpdateTrackPictureConsumer`, `DeleteAlbumConsumer`, …) builds a **routing slip** with the sequence of activities and executes it. Every slip starts from `RoutingSlips.Create(correlationId)` and adds steps with `AddStep(...)`; `TrackFaults(subjectId, processKind)` makes a failed slip publish a failure event for its subject (`ProcessingSlipFaultConsumer`, one process kind per workflow: `TrackPicture`, `TrackAudio`, `PlayListPicture`, `AlbumPicture` and the three `*Creation` kinds).

## Routing slip builders

- `CreateTrackRoutingSlipBuilder`: creating a track (consumes intents, moves files, kicks off processing).
- `PictureWorkflowRoutingSlipBuilder`: image pipeline (download → resize → upload → update).
- `AudioWorkflowRoutingSlipBuilder`: audio pipeline (download → transcode to `.m4a` → upload folder → update).
- `DeleteRoutingSlipBuilder`: marks a track, album or playlist as `Removing`, removes its files from the bucket and finally deletes the row.
- `PictureSourceRoutingSlipBuilder`: album and playlist pictures (copy the uploaded file to its final key, consume the intent, publish the event that starts the picture workflow), for both creation and update.

## Activities (steps)

Grouped by area:
- **Files**: `DownloadFileFromBucketActivity`, `UploadFileToBucketActivity`, `TransferFilesToBucketActivity`, `CopyFileInBucketActivity`, `RemoveFileFromBucketActivity`.
- **Audio**: `GenerateAudioWorkflowPathsActivity`, `TranscodeAudioActivity` (ffmpeg → `.m4a`), `UpdateTrackAudioActivity` (saves `AudioFolderName` and marks it `Completed`).
- **Pictures**: `GeneratePictureWorkflowPathsActivity`, `ResizePictureActivity` (ImageSharp), `UpdateTrackPictureActivity`, `UpdatePlayListPictureActivity`, `UpdateAlbumPictureActivity` (a shared `UpdatePicturesActivity<T>` base; compensation restores the previous names).
- **Life cycle**: `Mark{Track,PlayList,Album}LifeCycleActivity` and `Delete{Track,PlayList,Album}Activity` (generic over the entity; a missing row is skipped).
- **Events**: `PublishTrackProcessingEventsActivity`, `PublishPlayListPictureProcessingEventActivity`, `PublishAlbumPictureProcessingEventActivity`.
- **UploadIntents**: `ConsumeUploadIntentsActivity`.

`RoutingSlipCleanUpConsumer` reacts to `Completed`/`Faulted` to clean up (e.g. deleting the temporary working directory).

## Audio transcoding

`AudioTranscoderService` calls **ffmpeg**, reading the original from stdin and producing a single `audio.m4a` in a working directory: AAC with a configurable codec/bitrate/sample-rate/profile, and `-movflags +faststart` (moov at the start, so range-based seeking works). `TransferFilesToBucketActivity` then uploads that folder to `Tracks/ProcessedAudios/{folder}/`, and `UpdateTrackAudioActivity` saves the `AudioFolderName` (the **destination folder in S3**, not the local one) and sets the status to `Completed`.

## What the Worker needs configured

The Worker calls `AddApplication` (every Application handler is registered, so its container is validated on build), plus observability, the DB, storage (S3), the transcoder (ffmpeg), pictures, stream tickets, the MassTransit consumers and the Hangfire jobs. Its `appsettings.json` mirrors the API for the shared sections: `ApplicationStorage` (the same bucket as the API), `PlayList`, `Album`, `Track`, `Mix`, `UploadIntent`, `StreamGateway`, `StreamTicket` and `Playback` (the handlers need them to be constructed; the Worker never issues tickets, so the private key is never read), and adds `MassTransit`, `AudioTranscoder`, `Workers` (temp directory), `InfrastructureStorage`, `Database` and `OpenTelemetry`.

## Background jobs

Recurring **Hangfire** jobs (Postgres storage), registered by `RecurringJobsRegistrar` when the Worker starts:

- `DailyMixGenerationJob` (`daily-mix-generation`): regenerates the mixes every day.
- `ListeningHistoryCleanupJob` (`listening-history-cleanup`): prunes old listens every day at 03:00.
- `UploadIntentExpirationJob` (`upload-intent-expiration`): marks expired intents as such (freeing quota) every `ExpirationJobIntervalSeconds`.
- `TempUploadsCleanupJob` (`temp-uploads-cleanup`): cleans up old temporary uploads under `TempRootPrefix` (`TempUploadsRetentionDays`) every `TempCleanupJobIntervalSeconds`.

Hangfire cron has a one-minute resolution, so intervals in seconds are rounded up to whole minutes (`RecurringJobsRegistrar.EveryCron`). If the Worker isn't running, stale intents never get cleaned up on their own.
