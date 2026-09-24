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
        var album = await database.Albums.FindOwnedAsync(request.AlbumId, request.UserId, cancellationToken);
        if (album.IsError)
            return album.Errors;

        var albumTracks = await database.AlbumHasTracks
            .Where(albumTrack => albumTrack.AlbumId == request.AlbumId)
            .OrderBy(albumTrack => albumTrack.TrackNumber)
            .ToListAsync(cancellationToken);

        var link = albumTracks.SingleOrDefault(albumTrack => albumTrack.TrackId == request.TrackId);
        if (link == null)
            return Error.NotFound("Album.TrackNotFound", "Track is not in the album.");

        database.AlbumHasTracks.Remove(link);

        var trackNumber = 1;
        foreach (var remaining in albumTracks.Where(albumTrack => albumTrack.Id != link.Id))
            remaining.TrackNumber = trackNumber++;

        await database.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Removed track {TrackId} from album {AlbumId}", request.TrackId, request.AlbumId);

        return Result.Success;
    }
}
