using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;
using Musify.Application.Shared;

namespace Musify.Application.Tracks;


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
        if (request.UserId == null && !playbackConfiguration.AllowAnonymousListening)
            return Error.Unauthorized(description: "Sign in to stream music, or ask an administrator to enable anonymous listening.");

        var track = await database.Tracks.AsNoTracking()
            .Where(t => t.Id == request.TrackId && t.LifeCycleStatus == LifeCycleStatus.Active)
            .Select(t => new { t.Id, t.Audio })
            .SingleOrDefaultAsync(cancellationToken);

        if (track == null)
            return AppErrors.NotFound("Track", request.TrackId);

        if (!track.Audio.IsProcessed)
            return AppErrors.Conflict("Track.AudioNotReady", "Track audio is not available for streaming yet.");

        var response = await streamIssuer.IssueAsync(track.Id, track.Audio.FolderName, request.UserId, cancellationToken);

        logger.LogInformation("Issued stream ticket for track {TrackId} to user {UserId}", request.TrackId, request.UserId?.ToString() ?? "(anonymous)");

        return response;
    }
}
