using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;

namespace Musify.Application.PlayLists
{
    public record RemoveTrackFromPlayListCommand(long UserId, Guid PlayListId, Guid TrackId)
        : ICommand<ErrorOr<Success>>;

    public class RemoveTrackFromPlayListCommandHandler(
        IDatabase database,
        ILogger<RemoveTrackFromPlayListCommandHandler> logger)
        : ICommandHandler<RemoveTrackFromPlayListCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(RemoveTrackFromPlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.FindOwnedAsync(request.PlayListId, request.UserId, cancellationToken);
            if (playList.IsError)
                return playList.Errors;

            var playListTracks = await database.PlayListHasTracks
                .Where(plt => plt.PlayListId == request.PlayListId)
                .OrderBy(plt => plt.Position)
                .ToListAsync(cancellationToken);

            var link = playListTracks.SingleOrDefault(plt => plt.TrackId == request.TrackId);
            if (link == null)
                return Error.NotFound("PlayList.TrackNotFound", "Track is not in the playlist.");

            database.PlayListHasTracks.Remove(link);

            var position = 0;
            foreach (var remaining in playListTracks.Where(plt => plt.Id != link.Id))
                remaining.Position = position++;

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Removed track {TrackId} from playlist {PlayListId}", request.TrackId, request.PlayListId);

            return Result.Success;
        }
    }
}
