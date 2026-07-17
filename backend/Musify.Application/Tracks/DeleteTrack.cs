using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Domain.ValueObjects;
using Musify.Application.Events;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks
{
    public record DeleteTrackCommand(
        long UserId,
        Guid TrackId)
        : ICommand<ErrorOr<Success>>;

    public class DeleteTrackCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        ILogger<DeleteTrackCommandHandler> logger)
        : ICommandHandler<DeleteTrackCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(DeleteTrackCommand request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);
            if (track is null)
            {
                logger.LogInformation("Track {TrackId} not found", request.TrackId);
                return Error.NotFound();
            }

            if (track.OwnerUserId != request.UserId)
            {
                logger.LogWarning("User {UserId} unauthorized to delete track {TrackId}", request.UserId, request.TrackId);
                return Error.Unauthorized();
            }

            if (track.Audio.TranscodeStatus == ProcessingStatus.Processing
                || track.Audio.TranscodeStatus == ProcessingStatus.Pending)
            {
                logger.LogWarning("Track {TrackId} is currently being processed and cannot be deleted", request.TrackId);
                return Error.Conflict(description: "Track is currently being processed and cannot be deleted");
            }

            track.LifeCycleStatus = LifeCycleStatus.Removing;

            try
            {
                await eventBus.PublishAsync(
                    new DeleteTrackEvent(track.Id, request.UserId),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish delete track event for track {TrackId}", request.TrackId);
                return Error.Failure(description: $"Failed to delete track {request.TrackId}");
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to mark track {TrackId} as processing before deletion", request.TrackId);
                return Error.Failure(description: $"Failed to delete track {request.TrackId}");
            }

            return new Success();
        }
    }
}
