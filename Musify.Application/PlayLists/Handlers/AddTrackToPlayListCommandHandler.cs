using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Commands;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class AddTrackToPlayListCommandHandler(
        IDatabase database,
        ILogger<AddTrackToPlayListCommandHandler> logger)
        : ICommandHandler<AddTrackToPlayListCommand, Result>
    {
        public async ValueTask<Result> Handle(AddTrackToPlayListCommand request, CancellationToken cancellationToken)
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

            var trackExists = await database.Tracks
                .AsNoTracking()
                .AnyAsync(t => t.Id == request.TrackId, cancellationToken);
            if (!trackExists)
            {
                logger.LogInformation("Track {TrackId} not found", request.TrackId);
                return Result.NotFound($"Track {request.TrackId} not found");
            }

            var alreadyAdded = await database.PlayListHasTracks
                .AsNoTracking()
                .AnyAsync(plt => plt.PlayListId == request.PlayListId && plt.TrackId == request.TrackId, cancellationToken);
            if (alreadyAdded)
            {
                logger.LogInformation("Track {TrackId} already in playlist {PlayListId}", request.TrackId, request.PlayListId);
                return Result.Conflict("Track is already in the playlist.");
            }

            var nextPosition = await database.PlayListHasTracks
                .Where(plt => plt.PlayListId == request.PlayListId)
                .Select(plt => (int?)plt.Position)
                .MaxAsync(cancellationToken) + 1 ?? 0;

            await database.PlayListHasTracks.AddAsync(new PlayListHasTrack
            {
                PlayListId = request.PlayListId,
                TrackId = request.TrackId,
                Position = nextPosition
            }, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Added track {TrackId} to playlist {PlayListId} at position {Position}",
                request.TrackId, request.PlayListId, nextPosition);

            return Result.Success();
        }
    }
}
