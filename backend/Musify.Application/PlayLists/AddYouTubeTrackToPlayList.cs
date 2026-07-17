using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists
{
    public record AddYouTubeTrackToPlayListCommand(
        long UserId,
        Guid PlayListId,
        string VideoId,
        string Title,
        string Artist,
        int DurationSeconds,
        string ThumbnailUrl)
        : ICommand<ErrorOr<TrackApplicationResponse>>;

    public class AddYouTubeTrackToPlayListCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration,
        ILogger<AddYouTubeTrackToPlayListCommandHandler> logger)
        : ICommandHandler<AddYouTubeTrackToPlayListCommand, ErrorOr<TrackApplicationResponse>>
    {
        public async ValueTask<ErrorOr<TrackApplicationResponse>> Handle(AddYouTubeTrackToPlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.AsNoTracking()
                .SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);
            if (playList is null)
            {
                logger.LogInformation("Playlist {PlayListId} not found", request.PlayListId);
                return Error.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of playlist {PlayListId}", request.UserId, request.PlayListId);
                return Error.Unauthorized();
            }

            var trackResult = await GetOrCreateTrackAsync(request, cancellationToken);
            if (trackResult.IsError)
                return trackResult.Errors;

            var track = trackResult.Value;

            var alreadyAdded = await database.PlayListHasTracks.AsNoTracking()
                .AnyAsync(plt => plt.PlayListId == request.PlayListId && plt.TrackId == track.Id, cancellationToken);
            if (alreadyAdded)
            {
                logger.LogInformation("Track {TrackId} already in playlist {PlayListId}", track.Id, request.PlayListId);
                return Error.Conflict(description: "Track is already in the playlist.");
            }

            var nextPosition = await database.PlayListHasTracks
                .Where(plt => plt.PlayListId == request.PlayListId)
                .Select(plt => (int?)plt.Position)
                .MaxAsync(cancellationToken) + 1 ?? 0;

            await database.PlayListHasTracks.AddAsync(new PlayListHasTrack
            {
                PlayListId = request.PlayListId,
                TrackId = track.Id,
                Position = nextPosition
            }, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Added YouTube track {VideoId} ({TrackId}) to playlist {PlayListId} at position {Position}",
                request.VideoId, track.Id, request.PlayListId, nextPosition);

            var listensCount = await database.ListeningHistories.AsNoTracking()
                .CountAsync(lh => lh.TrackId == track.Id, cancellationToken);

            return TrackApplicationResponse.FromEntity(track, listensCount);
        }

        private async Task<ErrorOr<Track>> GetOrCreateTrackAsync(AddYouTubeTrackToPlayListCommand request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks
                .SingleOrDefaultAsync(t => t.Source == TrackSource.YouTube && t.ExternalId == request.VideoId, cancellationToken);

            if (track is not null)
            {
                var failed = track.AudioTranscodeProcessingStatus == ProcessingStatus.Failed;
                var neverQueued = !track.DownloadRequested && !track.IsAudioProcessed;
                if (failed || neverQueued)
                {
                    if (failed)
                    {
                        track.AudioTranscodeProcessingStatus = ProcessingStatus.Pending;
                        track.PicturesProcessingStatus = ProcessingStatus.Pending;
                        track.LifeCycleStatus = LifeCycleStatus.Active;
                        track.RetryCount += 1;
                        track.LastRetryAt = DateTime.UtcNow;
                    }
                    track.DownloadRequested = true;
                    await PublishDownloadEventAsync(track.Id, request, cancellationToken);
                    await database.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Queued YouTube track {VideoId} for download", request.VideoId);
                }
                return track;
            }

            var title = Truncate(request.Title, 50);
            track = new Track
            {
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                Artist = Truncate(request.Artist, 200),
                Source = TrackSource.YouTube,
                ExternalId = request.VideoId,
                DownloadRequested = true,
                Duration = request.DurationSeconds,
                OriginalPictureName = null,
                OriginalAudioName = null,
                SmallPictureName = trackConfiguration.Routes.PresetSmallPicture,
                MediumPictureName = trackConfiguration.Routes.PresetMediumPicture,
                LargePictureName = trackConfiguration.Routes.PresetLargePicture,
                PicturesProcessingStatus = ProcessingStatus.Pending,
                AudioTranscodeProcessingStatus = ProcessingStatus.Pending
            };

            await database.Tracks.AddAsync(track, cancellationToken);
            await PublishDownloadEventAsync(track.Id, request, cancellationToken);

            try
            {
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Created YouTube track {VideoId} ({TrackId}) and queued its download", request.VideoId, track.Id);
                return track;
            }
            catch (DbUpdateException)
            {
                logger.LogInformation("YouTube track {VideoId} was created concurrently", request.VideoId);
                return Error.Conflict(description: "The track was just created by another request. Retry the operation.");
            }
        }

        private async Task PublishDownloadEventAsync(Guid trackId, AddYouTubeTrackToPlayListCommand request, CancellationToken cancellationToken)
        {
            var audioProcessedFolderKey = trackConfiguration.Routes.BuildProcessedAudioPath(Guid.NewGuid().ToString());

            await eventBus.PublishAsync(
                new DownloadYouTubeTrackEvent(
                    trackId,
                    request.VideoId,
                    request.ThumbnailUrl,
                    storageConfiguration.Bucket,
                    audioProcessedFolderKey,
                    new ImageSize(
                        trackConfiguration.Routes.SmallPicturesPath,
                        trackConfiguration.PicturesSizes.SmallPictureWidth,
                        trackConfiguration.PicturesSizes.SmallPictureHeight),
                    new ImageSize(
                        trackConfiguration.Routes.MediumPicturesPath,
                        trackConfiguration.PicturesSizes.MediumPictureWidth,
                        trackConfiguration.PicturesSizes.MediumPictureHeight),
                    new ImageSize(
                        trackConfiguration.Routes.LargePicturesPath,
                        trackConfiguration.PicturesSizes.LargePictureWidth,
                        trackConfiguration.PicturesSizes.LargePictureHeight)),
                cancellationToken);
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
