using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;

namespace Musify.Application.Tracks
{
    public record SetTrackPublishedCommand(long UserId, Guid TrackId, bool IsPublished)
        : ICommand<ErrorOr<Success>>;

    public class SetTrackPublishedCommandHandler(
        IDatabase database,
        ILogger<SetTrackPublishedCommandHandler> logger)
        : ICommandHandler<SetTrackPublishedCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(SetTrackPublishedCommand request, CancellationToken cancellationToken)
        {
            var found = await database.Tracks.FindOwnedAsync(request.TrackId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var track = found.Value;
            if (request.IsPublished && !track.Audio.IsProcessed)
                return Error.Validation("Track.NotProcessed", "Only tracks whose audio finished processing can be published.");

            if (track.IsPublished == request.IsPublished)
                return Result.Success;

            track.IsPublished = request.IsPublished;
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("{Action} track {TrackId}", request.IsPublished ? "Published" : "Unpublished", track.Id);

            return Result.Success;
        }
    }
}
