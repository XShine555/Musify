using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Tracks.Commands;
using Musify.Domain.Entities;
using Musify.Application.Events;

namespace Musify.Application.Tracks.Handler
{
    public class DeleteTrackCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        ILogger<DeleteTrackCommandHandler> logger)
        : ICommandHandler<DeleteTrackCommand, Result>
    {
        public async ValueTask<Result> Handle(DeleteTrackCommand request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);
            if (track is null)
            {
                logger.LogInformation("Track {TrackId} not found", request.TrackId);
                return Result.NotFound();
            }

            var isOwner = await database.UserHasTracks
                .AsNoTracking()
                .AnyAsync(ut => ut.TrackId == request.TrackId && ut.UserId == request.UserId, cancellationToken);
            if (!isOwner)
            {
                logger.LogWarning("User {UserId} unauthorized to delete track {TrackId}", request.UserId, request.TrackId);
                return Result.Unauthorized();
            }

            if (track.AudioTranscodeProcessingStatus == ProcessingStatus.Processing
                || track.AudioTranscodeProcessingStatus == ProcessingStatus.Pending)
            {
                logger.LogWarning("Track {TrackId} is currently being processed and cannot be deleted", request.TrackId);
                return Result.Conflict("Track is currently being processed and cannot be deleted");
            }

            track.PicturesProcessingStatus = ProcessingStatus.Processing;
            track.AudioTranscodeProcessingStatus = ProcessingStatus.Processing;

            try
            {
                await eventBus.PublishAsync(
                    new DeleteTrackEvent(track.Id, request.UserId),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish delete track event for track {TrackId}", request.TrackId);
                return Result.Error($"Failed to delete track {request.TrackId}");
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to mark track {TrackId} as processing before deletion", request.TrackId);
                return Result.Error($"Failed to delete track {request.TrackId}");
            }

            return Result.NoContent();
        }
    }
}