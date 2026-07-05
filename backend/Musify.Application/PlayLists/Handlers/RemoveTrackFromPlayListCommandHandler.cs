using Ardalis.Result;
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
        : ICommandHandler<RemoveTrackFromPlayListCommand, Result>
    {
        public async ValueTask<Result> Handle(RemoveTrackFromPlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);
            if (playList is null)
            {
                logger.LogInformation("Playlist {PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of playlist {PlayListId}", request.UserId, request.PlayListId);
                return Result.Unauthorized();
            }

            var link = await database.PlayListHasTracks
                .SingleOrDefaultAsync(plt => plt.PlayListId == request.PlayListId && plt.TrackId == request.TrackId, cancellationToken);
            if (link is null)
            {
                logger.LogInformation("Track {TrackId} not in playlist {PlayListId}", request.TrackId, request.PlayListId);
                return Result.NotFound("Track is not in the playlist.");
            }

            database.PlayListHasTracks.Remove(link);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Removed track {TrackId} from playlist {PlayListId}", request.TrackId, request.PlayListId);

            return Result.Success();
        }
    }
}
