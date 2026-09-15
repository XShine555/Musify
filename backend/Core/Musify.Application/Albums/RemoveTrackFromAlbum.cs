using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;

namespace Musify.Application.Albums
{
    public record RemoveTrackFromAlbumCommand(long UserId, Guid AlbumId, Guid TrackId)
        : ICommand<ErrorOr<Success>>;

    public class RemoveTrackFromAlbumCommandHandler(
        IDatabase database,
        ILogger<RemoveTrackFromAlbumCommandHandler> logger)
        : ICommandHandler<RemoveTrackFromAlbumCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(RemoveTrackFromAlbumCommand request, CancellationToken cancellationToken)
        {
            var album = await database.Albums
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == request.AlbumId, cancellationToken);
            if (album is null)
            {
                logger.LogInformation("Album {AlbumId} not found", request.AlbumId);
                return Error.NotFound();
            }

            if (album.OwnerUserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of album {AlbumId}", request.UserId, request.AlbumId);
                return Error.Unauthorized();
            }

            var albumTracks = await database.AlbumHasTracks
                .Where(albumTrack => albumTrack.AlbumId == request.AlbumId)
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .ToListAsync(cancellationToken);

            var link = albumTracks.SingleOrDefault(albumTrack => albumTrack.TrackId == request.TrackId);
            if (link is null)
            {
                logger.LogInformation("Track {TrackId} not in album {AlbumId}", request.TrackId, request.AlbumId);
                return Error.NotFound(description: "Track is not in the album.");
            }

            database.AlbumHasTracks.Remove(link);

            var trackNumber = 1;
            foreach (var remaining in albumTracks.Where(albumTrack => albumTrack.Id != link.Id))
                remaining.TrackNumber = trackNumber++;

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Removed track {TrackId} from album {AlbumId}", request.TrackId, request.AlbumId);

            return new Success();
        }
    }
}
