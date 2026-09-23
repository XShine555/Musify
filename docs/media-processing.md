# Media processing (Worker + MassTransit)

Heavy processing (audio transcoding, thumbnails, bucket transfers) happens **outside the HTTP request**, in the **Worker**, orchestrated with **MassTransit** on top of RabbitMQ.

## Why async, and why routing slips

- Transcoding audio and resizing images is CPU/IO-intensive: it shouldn't block an API call.
- It's a **multi-step process with side effects** (download from S3 → transcode → upload results → update the DB). If a step fails, the previous ones need to be **undone**. That's what **routing slips** are for (MassTransit's Saga pattern): each step is an *activity* with its own **compensation**.
- **Transactional outbox** (EF + MassTransit): the event is published consistently with the DB change (never lost, never duplicated).

## Trigger

When a track or playlist is created or updated, the handler publishes an event (e.g. `CreateTrackResourcesEvent`). A **consumer** (`CreateTrackConsumer`, `UpdateTrackPictureConsumer`, …) builds a **routing slip** with the sequence of activities and executes it.

## Routing slip builders

- `CreateTrackRoutingSlipBuilder`: creating a track (consumes intents, moves files, kicks off processing).
- `PictureWorkflowRoutingSlipBuilder`: image pipeline (download → resize → upload → update).
- `AudioWorkflowRoutingSlipBuilder`: audio pipeline (download → transcode to `.m4a` → upload folder → update).
- `DeleteTrackRoutingSlipBuilder`, `DeletePlayListRoutingSlipBuilder`, `PlayListPictureSourceRoutingSlipBuilder`.

## Activities (steps)

Grouped by area:
- **Files**: `DownloadFileFromBucketActivity`, `UploadFileToBucketActivity`, `TransferFilesToBucketActivity`, `CopyFileInBucketActivity`, `RemoveFileFromBucketActivity`.
- **Audio**: `GenerateAudioWorkflowPathsActivity`, `TranscodeAudioActivity` (ffmpeg → `.m4a`), `UpdateTrackAudioActivity` (saves `AudioFolderName` and marks it `Completed`).
- **Pictures**: `GeneratePictureWorkflowPathsActivity`, `ResizePictureActivity` (ImageSharp), `UpdateTrackPictureActivity`, `UpdatePlayListPictureActivity`.
- **Tracks/Playlists**: `MarkTrackAsRemovingActivity`, `DeleteTrackFromDbActivity`, `PublishTrackProcessingEventsActivity`, and their playlist equivalents.
- **UploadIntents**: `ConsumeUploadIntentsActivity`.

`RoutingSlipCleanUpConsumer` reacts to `Completed`/`Faulted` to clean up (e.g. deleting the temporary working directory).

## Audio transcoding

`AudioTranscoderService` calls **ffmpeg**, reading the original from stdin and producing a single `audio.m4a` in a working directory: AAC with a configurable codec/bitrate/sample-rate/profile, and `-movflags +faststart` (moov at the start, so range-based seeking works). `TransferFilesToBucketActivity` then uploads that folder to `Tracks/ProcessedAudios/{folder}/`, and `UpdateTrackAudioActivity` saves the `AudioFolderName` (the **destination folder in S3**, not the local one) and sets the status to `Completed`.

## What the Worker needs configured

The Worker registers: storage (S3), transcoder (ffmpeg), pictures, the DB, the MassTransit consumers, and the upload-intent jobs. Its `appsettings` needs the right sections: `Bucket` (= `webapi-storage`, same as the API), `PlayList`, `Track`, `MassTransit`, `AudioTranscoder`, `UploadIntent`, `Workers` (temp directory), `InfrastructureStorage`, `Database`.

## Background jobs

- `UploadIntentExpirationJob`: marks expired intents as such (freeing quota) every `ExpirationJobIntervalSeconds`.
- `TemporalUploadsCleanUpJob`: cleans up old temporary uploads.

Both run on the Worker host (`AddUploadIntentJobs`). If they aren't registered anywhere, stale intents never get cleaned up on their own.
