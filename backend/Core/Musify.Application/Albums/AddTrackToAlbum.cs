using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Albums
{
    public record AddTrackToAlbumCommand(long UserId, Guid AlbumId, Guid TrackId)
        : ICommand<ErrorOr<Success>>;

    public class AddTrackToAlbumCommandHandler(
        IDatabase database,
        ILogger<AddTrackToAlbumCommandHandler> logger)
        : ICommandHandler<AddTrackToAlbumCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(AddTrackToAlbumCommand request, CancellationToken cancellationToken)
        {
            var album = await database.Albums
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == request.AlbumId, cancellationToken);
            if (album == null)
            {
                logger.LogInformation("Album {AlbumId} not found", request.AlbumId);
                return Error.NotFound();
            }

            if (album.OwnerUserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of album {AlbumId}", request.UserId, request.AlbumId);
                return Error.Unauthorized();
            }

            var track = await database.Tracks
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);
            if (track == null)
            {
                logger.LogInformation("Track {TrackId} not found", request.TrackId);
                return Error.NotFound(description: $"Track {request.TrackId} not found");
            }

            if (track.OwnerUserId != request.UserId)
            {
                logger.LogWarning("User {UserId} does not own track {TrackId}", request.UserId, request.TrackId);
                return Error.Unauthorized();
            }

            var alreadyAdded = await database.AlbumHasTracks
                .AsNoTracking()
                .AnyAsync(albumTrack => albumTrack.AlbumId == request.AlbumId && albumTrack.TrackId == request.TrackId, cancellationToken);
            if (alreadyAdded)
            {
                logger.LogInformation("Track {TrackId} already in album {AlbumId}", request.TrackId, request.AlbumId);
                return Error.Conflict(description: "Track is already in the album.");
            }

            var albumTracksQuery = database.AlbumHasTracks
                .Where(albumTrack => albumTrack.AlbumId == request.AlbumId);

            var hasTracks = await albumTracksQuery.AnyAsync(cancellationToken);
            var nextTrackNumber = hasTracks
                ? await albumTracksQuery.MaxAsync(albumTrack => albumTrack.TrackNumber, cancellationToken) + 1
                : 1;

            await database.AlbumHasTracks.AddAsync(new AlbumHasTrack
            {
                AlbumId = request.AlbumId,
                TrackId = request.TrackId,
                TrackNumber = nextTrackNumber
            }, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Added track {TrackId} to album {AlbumId} as number {TrackNumber}",
                request.TrackId, request.AlbumId, nextTrackNumber);

            return new Success();
        }
    }
}
