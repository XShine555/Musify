using System.Globalization;
using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks;

/// <summary>Starts a listen: records it for signed-in users and issues the stream ticket.</summary>
public record StartListeningCommand(Guid TrackId, long? UserId)
    : ICommand<ErrorOr<TrackStreamResponse>>;

public class StartListeningCommandHandler(
    IDatabase database,
    TrackStreamIssuer streamIssuer,
    PlaybackConfiguration playbackConfiguration,
    ILogger<StartListeningCommandHandler> logger)
    : ICommandHandler<StartListeningCommand, ErrorOr<TrackStreamResponse>>
{
    public async ValueTask<ErrorOr<TrackStreamResponse>> Handle(StartListeningCommand request, CancellationToken cancellationToken)
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

        Guid? listenId = null;
        if (request.UserId is { } userId)
        {
            var listen = new ListeningHistory { UserId = userId, TrackId = track.Id };
            await database.ListeningHistories.AddAsync(listen, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            listenId = listen.Id;
        }

        logger.LogInformation("Issued stream ticket for track {TrackId} to user {UserId}", request.TrackId, request.UserId?.ToString(CultureInfo.InvariantCulture) ?? "(anonymous)");

        return streamIssuer.Issue(track.Audio.FolderName, request.UserId, listenId);
    }
}
