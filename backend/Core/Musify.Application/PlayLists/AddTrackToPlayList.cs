using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists;

public record AddTrackToPlayListCommand(long UserId, Guid PlayListId, Guid TrackId)
    : ICommand<ErrorOr<Success>>;

public class AddTrackToPlayListCommandHandler(
    IDatabase database,
    ILogger<AddTrackToPlayListCommandHandler> logger)
    : ICommandHandler<AddTrackToPlayListCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(AddTrackToPlayListCommand request, CancellationToken cancellationToken)
    {
        var playList = await database.PlayLists
            .AsNoTracking()
            .SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);
        if (playList == null)
            return AppErrors.NotFound("PlayList", request.PlayListId);

        if (playList.OwnerUserId != request.UserId)
            return AppErrors.Forbidden("PlayList", request.PlayListId);

        var trackExists = await database.Tracks
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TrackId, cancellationToken);
        if (!trackExists)
            return AppErrors.NotFound("Track", request.TrackId);

        var alreadyAdded = await database.PlayListHasTracks
            .AsNoTracking()
            .AnyAsync(plt => plt.PlayListId == request.PlayListId && plt.TrackId == request.TrackId, cancellationToken);
        if (alreadyAdded)
            return AlreadyInPlayList;

        // Playlist positions are internal indexes, so they start at 0.
        var nextPosition = (await database.PlayListHasTracks
            .Where(plt => plt.PlayListId == request.PlayListId)
            .MaxAsync(plt => (int?)plt.Position, cancellationToken) ?? -1) + 1;

        await database.PlayListHasTracks.AddAsync(new PlayListHasTrack
        {
            PlayListId = request.PlayListId,
            TrackId = request.TrackId,
            Position = nextPosition
        }, cancellationToken);

        var saved = await database.TrySaveChangesAsync(AlreadyInPlayList, cancellationToken);
        if (saved.IsError)
            return saved;

        logger.LogInformation("Added track {TrackId} to playlist {PlayListId} at position {Position}",
            request.TrackId, request.PlayListId, nextPosition);

        return Result.Success;
    }

    private static Error AlreadyInPlayList => AppErrors.Conflict("PlayList.TrackAlreadyAdded", "Track is already in the playlist.");
}
