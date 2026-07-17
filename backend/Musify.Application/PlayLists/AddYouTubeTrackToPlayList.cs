using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Services;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists
{
    public record AddYouTubeTrackToPlayListCommand(
        long UserId,
        Guid PlayListId,
        string VideoId)
        : ICommand<ErrorOr<TrackApplicationResponse>>;

    public class AddYouTubeTrackToPlayListCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        IYouTubeMusicService youTubeMusicService,
        YouTubeTrackProvisioner provisioner,
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

            var provisionResult = await provisioner.GetOrCreateAsync(request.VideoId, cancellationToken);
            if (provisionResult.IsError)
                return provisionResult.Errors;

            var track = provisionResult.Value.Track;

            var failed = track.Audio.TranscodeStatus == ProcessingStatus.Failed;
            var neverQueued = !track.Audio.DownloadRequested && !track.Audio.IsProcessed;
            if (failed || neverQueued)
            {
                if (failed)
                {
                    track.Audio.TranscodeStatus = ProcessingStatus.Pending;
                    track.Pictures.ProcessingStatus = ProcessingStatus.Pending;
                    track.LifeCycleStatus = LifeCycleStatus.Active;
                    track.Audio.RetryCount += 1;
                    track.Audio.LastRetryAt = DateTime.UtcNow;
                }
                track.Audio.DownloadRequested = true;

                var thumbnailUrl = await ResolveThumbnailUrlAsync(request.VideoId, provisionResult.Value.Song, cancellationToken);
                if (thumbnailUrl.IsError)
                    return thumbnailUrl.Errors;

                await PublishDownloadEventAsync(track.Id, request.VideoId, thumbnailUrl.Value, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Queued YouTube track {VideoId} for download", request.VideoId);
            }

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

        private async Task<ErrorOr<string>> ResolveThumbnailUrlAsync(string videoId, YouTubeSongResult? song, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(song?.ThumbnailUrl))
                return song.ThumbnailUrl;

            var songResult = await youTubeMusicService.GetSongAsync(videoId, cancellationToken);
            if (songResult.IsError)
                return songResult.Errors;

            return songResult.Value.ThumbnailUrl;
        }

        private async Task PublishDownloadEventAsync(Guid trackId, string videoId, string thumbnailUrl, CancellationToken cancellationToken)
        {
            var audioProcessedFolderKey = trackConfiguration.Routes.BuildProcessedAudioPath(Guid.NewGuid().ToString());

            await eventBus.PublishAsync(
                new DownloadYouTubeTrackEvent(
                    trackId,
                    videoId,
                    thumbnailUrl,
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
    }
}
