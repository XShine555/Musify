using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.YouTube.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.YouTube
{
    public record ResolveYouTubeTrackStreamCommand(string VideoId, long UserId)
        : ICommand<ErrorOr<YouTubeStreamResponse>>;

    public class ResolveYouTubeTrackStreamCommandHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        YouTubeTrackProvisioner provisioner,
        TrackStreamIssuer streamIssuer,
        ILogger<ResolveYouTubeTrackStreamCommandHandler> logger)
        : ICommandHandler<ResolveYouTubeTrackStreamCommand, ErrorOr<YouTubeStreamResponse>>
    {
        public async ValueTask<ErrorOr<YouTubeStreamResponse>> Handle(ResolveYouTubeTrackStreamCommand request, CancellationToken cancellationToken)
        {
            var track = await provisioner.FindAsync(request.VideoId, cancellationToken);

            if (track is not null && track.Audio.IsProcessed)
            {
                var serverStream = await streamIssuer.IssueAsync(track.Id, track.Audio.FolderName, request.UserId, cancellationToken);
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

            if (track is null)
            {
                var provisionResult = await provisioner.GetOrCreateAsync(request.VideoId, cancellationToken);
                if (!provisionResult.IsError)
                    track = provisionResult.Value.Track;
                else
                    logger.LogWarning("Could not provision YouTube track {VideoId}: {Error}", request.VideoId, provisionResult.FirstError.Description);
            }

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
    }
}
