using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.YouTube.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.YouTube
{
    public record ResolveYouTubeTrackStreamCommand(string VideoId, long? UserId)
        : ICommand<ErrorOr<YouTubeStreamResponse>>;

    public class ResolveYouTubeTrackStreamCommandHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        YouTubeTrackProvisioner provisioner,
        TrackStreamIssuer streamIssuer,
        PlaybackConfiguration playbackConfiguration,
        ILogger<ResolveYouTubeTrackStreamCommandHandler> logger)
        : ICommandHandler<ResolveYouTubeTrackStreamCommand, ErrorOr<YouTubeStreamResponse>>
    {
        public async ValueTask<ErrorOr<YouTubeStreamResponse>> Handle(ResolveYouTubeTrackStreamCommand request, CancellationToken cancellationToken)
        {
            var isAnonymous = !request.UserId.HasValue;
            if (isAnonymous && !playbackConfiguration.AllowAnonymousListening)
                return Error.Unauthorized(description: "Sign in to stream music, or ask an administrator to enable anonymous listening.");

            var track = await provisioner.FindAsync(request.VideoId, cancellationToken);

            if (track != null && track.Audio.IsProcessed)
            {
                var serverStream = await streamIssuer.IssueAsync(track.Id, track.Audio.FolderName, request.UserId, cancellationToken);
                return new YouTubeStreamResponse(
                    track.Id,
                    YouTubeStreamMode.Server,
                    serverStream.ManifestUrl,
                    serverStream.Ticket,
                    serverStream.ExpiresInSeconds);
            }

            if (isAnonymous && playbackConfiguration.AnonymousFragmentSeconds > 0)
                return Error.Conflict(description: "This track isn't available for anonymous preview yet. Try again in a moment.");

            var streamInfo = await youTubeMusicService.GetAudioStreamAsync(request.VideoId, cancellationToken);
            if (streamInfo.IsError)
                return streamInfo.Errors;

            if (track == null)
            {
                var provisionResult = await provisioner.GetOrCreateAsync(request.VideoId, cancellationToken);
                if (!provisionResult.IsError)
                    track = provisionResult.Value.Track;
                else
                    logger.LogWarning("Could not provision YouTube track {VideoId}: {Error}", request.VideoId, provisionResult.FirstError.Description);
            }

            if (track != null&& request.UserId != null)
            {
                await database.ListeningHistories.AddAsync(new ListeningHistory
                {
                    UserId = request.UserId.Value,
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
                track?.Id,
                YouTubeStreamMode.YouTube,
                streamInfo.Value.Url, null,
                streamInfo.Value.ExpiresInSeconds);
        }
    }
}
