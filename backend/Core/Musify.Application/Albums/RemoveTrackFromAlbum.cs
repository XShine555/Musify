using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;

namespace Musify.Application.Albums;

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
        if (album == null)
            return AppErrors.NotFound("Album", request.AlbumId);

        if (album.OwnerUserId != request.UserId)
            return AppErrors.Forbidden("Album", request.AlbumId);

        var albumTracks = await database.AlbumHasTracks
            .Where(albumTrack => albumTrack.AlbumId == request.AlbumId)
            .OrderBy(albumTrack => albumTrack.TrackNumber)
            .ToListAsync(cancellationToken);

        var link = albumTracks.SingleOrDefault(albumTrack => albumTrack.TrackId == request.TrackId);
        if (link == null)
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
