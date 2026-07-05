using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Commands;

namespace Musify.Application.PlayLists.Handlers
{
    public class RemoveTrackFromPlayListCommandHandler(
        IDatabase database,
        ILogger<RemoveTrackFromPlayListCommandHandler> logger)
        : ICommandHandler<RemoveTrackFromPlayListCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(RemoveTrackFromPlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);
            if (playList is null)
            {
                logger.LogInformation("Playlist {PlayListId} not found", request.PlayListId);
                return Error.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of playlist {PlayListId}", request.UserId, request.PlayListId);
                return Error.Unauthorized();
            }

            var link = await database.PlayListHasTracks
                .SingleOrDefaultAsync(plt => plt.PlayListId == request.PlayListId && plt.TrackId == request.TrackId, cancellationToken);
            if (link is null)
            {
                logger.LogInformation("Track {TrackId} not in playlist {PlayListId}", request.TrackId, request.PlayListId);
                return Error.NotFound(description: "Track is not in the playlist.");
            }

            database.PlayListHasTracks.Remove(link);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Removed track {TrackId} from playlist {PlayListId}", request.TrackId, request.PlayListId);

            return new Success();
        }
    }
}
