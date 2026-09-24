using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists;

/// <summary>Appends tracks to a playlist in the given order, skipping the ones it already has.</summary>
public record AddTracksToPlayListCommand(long UserId, Guid PlayListId, IReadOnlyList<Guid> TrackIds)
    : ICommand<ErrorOr<Success>>;

public class AddTracksToPlayListCommandHandler(
    IDatabase database,
    ILogger<AddTracksToPlayListCommandHandler> logger)
    : ICommandHandler<AddTracksToPlayListCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(AddTracksToPlayListCommand request, CancellationToken cancellationToken)
    {
        var playList = await database.PlayLists.FindOwnedAsync(request.PlayListId, request.UserId, cancellationToken);
        if (playList.IsError)
            return playList.Errors;

        var requestedIds = request.TrackIds.Distinct().ToList();

        var existingIds = await database.Tracks
            .AsNoTracking()
            .Where(track => requestedIds.Contains(track.Id) && track.LifeCycleStatus == LifeCycleStatus.Active)
            .Select(track => track.Id)
            .ToListAsync(cancellationToken);

        var missing = requestedIds.FirstOrDefault(id => !existingIds.Contains(id));
        if (missing != Guid.Empty)
            return AppErrors.NotFound("Track", missing);

        var alreadyInPlayList = await database.PlayListHasTracks
            .AsNoTracking()
            .Where(link => link.PlayListId == request.PlayListId && requestedIds.Contains(link.TrackId))
            .Select(link => link.TrackId)
            .ToListAsync(cancellationToken);

        var toAdd = requestedIds.Where(id => !alreadyInPlayList.Contains(id)).ToList();
        if (toAdd.Count == 0)
            return Result.Success;

        // Playlist positions are internal indexes, so they start at 0.
        var nextPosition = (await database.PlayListHasTracks
            .Where(link => link.PlayListId == request.PlayListId)
            .MaxAsync(link => (int?)link.Position, cancellationToken) ?? -1) + 1;

        foreach (var trackId in toAdd)
        {
            await database.PlayListHasTracks.AddAsync(new PlayListHasTrack
            {
                PlayListId = request.PlayListId,
                TrackId = trackId,
                Position = nextPosition++
            }, cancellationToken);
        }

        var saved = await database.TrySaveChangesAsync(
            AppErrors.Conflict("PlayList.ConcurrentChange", "The playlist changed while adding tracks. Try again."), cancellationToken);
        if (saved.IsError)
            return saved;

        logger.LogInformation("Added {Count} tracks to playlist {PlayListId}", toAdd.Count, request.PlayListId);

        return Result.Success;
    }
}
