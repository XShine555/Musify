using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.YouTube.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.YouTube
{
    public record GetYouTubeTrackStreamQuery(string VideoId, long UserId)
        : IQuery<ErrorOr<YouTubeStreamResponse>>;

    public class GetYouTubeTrackStreamQueryHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        TrackStreamIssuer streamIssuer,
        TrackConfiguration trackConfiguration,
        ILogger<GetYouTubeTrackStreamQueryHandler> logger)
        : IQueryHandler<GetYouTubeTrackStreamQuery, ErrorOr<YouTubeStreamResponse>>
    {
        public async ValueTask<ErrorOr<YouTubeStreamResponse>> Handle(GetYouTubeTrackStreamQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks
                .SingleOrDefaultAsync(t => t.Source == TrackSource.YouTube && t.ExternalId == request.VideoId, cancellationToken);

            if (track is not null && track.IsAudioProcessed)
            {
                var serverStream = await streamIssuer.IssueAsync(track.Id, track.AudioFolderName, request.UserId, cancellationToken);
                return new YouTubeStreamResponse(
                    YouTubeStreamMode.Server,
                    serverStream.ManifestUrl,
                    serverStream.Ticket,
                    serverStream.ExpiresInSeconds,
                    track.Id);
            }

            var streamInfo = await youTubeMusicService.GetAudioStreamAsync(request.VideoId, cancellationToken);
            if (streamInfo.IsError)
                return streamInfo.Errors;

            track ??= await CreateTrackFromYouTubeAsync(request.VideoId, cancellationToken);

            if (track is not null)
            {
                await database.ListeningHistories.AddAsync(new ListeningHistory
                {
                    UserId = request.UserId,
                    TrackId = track.Id,
                }, cancellationToken);

                try
                {
                    await database.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException exception)
                {
                    logger.LogWarning(exception, "Failed to record listening history for YouTube video {VideoId}", request.VideoId);
                }
            }

            return new YouTubeStreamResponse(
                YouTubeStreamMode.YouTube,
                streamInfo.Value.Url,
                string.Empty,
                streamInfo.Value.ExpiresInSeconds,
                track?.Id ?? Guid.Empty);
        }

        private async Task<Track?> CreateTrackFromYouTubeAsync(string videoId, CancellationToken cancellationToken)
        {
            var songResult = await youTubeMusicService.GetSongAsync(videoId, cancellationToken);
            if (songResult.IsError)
            {
                logger.LogWarning("Could not load YouTube metadata for {VideoId}: {Error}", videoId, songResult.FirstError.Description);
                return null;
            }

            var song = songResult.Value;
            var title = Truncate(song.Title, 50);

            var track = new Track
            {
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                Artist = Truncate(song.Artist, 200),
                Source = TrackSource.YouTube,
                ExternalId = videoId,
                DownloadRequested = false,
                Duration = song.DurationSeconds,
                OriginalPictureName = null,
                OriginalAudioName = null,
                SmallPictureName = trackConfiguration.Routes.PresetSmallPicture,
                MediumPictureName = trackConfiguration.Routes.PresetMediumPicture,
                LargePictureName = trackConfiguration.Routes.PresetLargePicture,
                PicturesProcessingStatus = ProcessingStatus.Pending,
                AudioTranscodeProcessingStatus = ProcessingStatus.Pending
            };

            await database.Tracks.AddAsync(track, cancellationToken);

            try
            {
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Created YouTube track {VideoId} ({TrackId}) from playback", videoId, track.Id);
                return track;
            }
            catch (DbUpdateException)
            {
                database.Tracks.Remove(track);
                return await database.Tracks
                    .SingleOrDefaultAsync(t => t.Source == TrackSource.YouTube && t.ExternalId == videoId, cancellationToken);
            }
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
