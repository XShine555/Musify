using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks;

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
        var found = await database.Tracks.FindOwnedAsync(request.TrackId, request.UserId, cancellationToken);
        if (found.IsError)
            return found.Errors;

        var track = found.Value;
        if (track.Audio.IsInProgress)
            return AppErrors.Conflict("Track.Processing", "Track is currently being processed and cannot be deleted.");

        track.LifeCycleStatus = LifeCycleStatus.Removing;

        await eventBus.PublishAsync(new DeleteTrackEvent(track.Id, request.UserId), cancellationToken);
        await database.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Marked track {TrackId} as removing", request.TrackId);

        return Result.Success;
    }
}
