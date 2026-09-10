using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks
{
    /// <param name="UserId">Null for an anonymous request — allowed only when
    /// <see cref="PlaybackConfiguration.AllowAnonymousListening"/> is enabled.</param>
    public record GetTrackStreamQuery(Guid TrackId, long? UserId)
        : IQuery<ErrorOr<TrackStreamResponse>>;

    public class GetTrackStreamQueryHandler(
        IDatabase database,
        TrackStreamIssuer streamIssuer,
        PlaybackConfiguration playbackConfiguration,
        ILogger<GetTrackStreamQueryHandler> logger)
        : IQueryHandler<GetTrackStreamQuery, ErrorOr<TrackStreamResponse>>
    {
        public async ValueTask<ErrorOr<TrackStreamResponse>> Handle(GetTrackStreamQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId is null && !playbackConfiguration.AllowAnonymousListening)
                return Error.Unauthorized(description: "Sign in to stream music, or ask an administrator to enable anonymous listening.");

            var track = await database.Tracks.AsNoTracking()
                .Where(t => t.Id == request.TrackId)
                .Select(t => new { t.Id, t.Audio })
                .SingleOrDefaultAsync(cancellationToken);

            if (track == null)
                return Error.NotFound();

            if (!track.Audio.IsProcessed)
            {
                logger.LogInformation("Stream requested for track {TrackId} but audio is not ready", request.TrackId);
                return Error.Conflict(description: "Track audio is not available for streaming yet.");
            }

            var response = await streamIssuer.IssueAsync(track.Id, track.Audio.FolderName, request.UserId, cancellationToken);

            logger.LogInformation("Issued stream ticket for track {TrackId} to user {UserId}", request.TrackId, request.UserId?.ToString() ?? "(anonymous)");

            return response;
        }
    }
}
