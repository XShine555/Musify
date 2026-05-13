using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
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

            if (track.Id != request.UserId)
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
            await database.SaveChangesAsync(cancellationToken);

            await eventBus.PublishAsync(
                new DeleteTrackEvent(track.Id, request.UserId),
                cancellationToken);

            return Result.NoContent();
        }
    }
}