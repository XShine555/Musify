using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums
{
    /// <summary>Appends your own tracks to an album in the given order, skipping the ones it already has.</summary>
    public record AddTracksToAlbumCommand(long UserId, Guid AlbumId, IReadOnlyList<Guid> TrackIds)
        : ICommand<ErrorOr<Success>>;

    public class AddTracksToAlbumCommandHandler(
        IDatabase database,
        ILogger<AddTracksToAlbumCommandHandler> logger)
        : ICommandHandler<AddTracksToAlbumCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(AddTracksToAlbumCommand request, CancellationToken cancellationToken)
        {
            var album = await database.Albums.FindOwnedAsync(request.AlbumId, request.UserId, cancellationToken);
            if (album.IsError)
                return album.Errors;

            var requestedIds = request.TrackIds.Distinct().ToList();

            var tracks = await database.Tracks
                .AsNoTracking()
                .Where(track => requestedIds.Contains(track.Id) && track.LifeCycleStatus == LifeCycleStatus.Active)
                .Select(track => new { track.Id, track.OwnerUserId })
                .ToListAsync(cancellationToken);

            var missing = requestedIds.FirstOrDefault(id => tracks.All(track => track.Id != id));
            if (missing != Guid.Empty)
                return AppErrors.NotFound("Track", missing);

            var foreign = tracks.FirstOrDefault(track => track.OwnerUserId != request.UserId);
            if (foreign != null)
                return AppErrors.Forbidden("Track", foreign.Id);

            var alreadyInAlbum = await database.AlbumHasTracks
                .AsNoTracking()
                .Where(link => link.AlbumId == request.AlbumId && requestedIds.Contains(link.TrackId))
                .Select(link => link.TrackId)
                .ToListAsync(cancellationToken);

            var toAdd = requestedIds.Where(id => !alreadyInAlbum.Contains(id)).ToList();
            if (toAdd.Count == 0)
                return Result.Success;

            // Album track numbers are user-visible, so they start at 1.
            var nextTrackNumber = (await database.AlbumHasTracks
                .Where(link => link.AlbumId == request.AlbumId)
                .MaxAsync(link => (int?)link.TrackNumber, cancellationToken) ?? 0) + 1;

            foreach (var trackId in toAdd)
            {
                await database.AlbumHasTracks.AddAsync(new AlbumHasTrack
                {
                    AlbumId = request.AlbumId,
                    TrackId = trackId,
                    TrackNumber = nextTrackNumber++
                }, cancellationToken);
            }

            var saved = await database.TrySaveChangesAsync(
                AppErrors.Conflict("Album.ConcurrentChange", "The album changed while adding tracks. Try again."), cancellationToken);
            if (saved.IsError)
                return saved;

            logger.LogInformation("Added {Count} tracks to album {AlbumId}", toAdd.Count, request.AlbumId);

            return Result.Success;
        }
    }
}
