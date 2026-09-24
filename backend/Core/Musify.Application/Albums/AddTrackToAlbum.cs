using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;

namespace Musify.Application.Albums;

public record AddTrackToAlbumCommand(long UserId, Guid AlbumId, Guid TrackId)
    : ICommand<ErrorOr<Success>>;

public class AddTrackToAlbumCommandHandler(
    IDatabase database,
    ILogger<AddTrackToAlbumCommandHandler> logger)
    : ICommandHandler<AddTrackToAlbumCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(AddTrackToAlbumCommand request, CancellationToken cancellationToken)
    {
        var album = await database.Albums.FindOwnedAsync(request.AlbumId, request.UserId, cancellationToken);
        if (album.IsError)
            return album.Errors;

        var track = await database.Tracks.FindOwnedAsync(request.TrackId, request.UserId, cancellationToken);
        if (track.IsError)
            return track.Errors;

        var alreadyAdded = await database.AlbumHasTracks
            .AsNoTracking()
            .AnyAsync(albumTrack => albumTrack.AlbumId == request.AlbumId && albumTrack.TrackId == request.TrackId, cancellationToken);
        if (alreadyAdded)
            return AlreadyInAlbum;

        // Album track numbers are user-visible, so they start at 1.
        var nextTrackNumber = (await database.AlbumHasTracks
            .Where(albumTrack => albumTrack.AlbumId == request.AlbumId)
            .MaxAsync(albumTrack => (int?)albumTrack.TrackNumber, cancellationToken) ?? 0) + 1;

        await database.AlbumHasTracks.AddAsync(new AlbumHasTrack
        {
            AlbumId = request.AlbumId,
            TrackId = request.TrackId,
            TrackNumber = nextTrackNumber
        }, cancellationToken);

        var saved = await database.TrySaveChangesAsync(AlreadyInAlbum, cancellationToken);
        if (saved.IsError)
            return saved;

        logger.LogInformation("Added track {TrackId} to album {AlbumId} as number {TrackNumber}",
            request.TrackId, request.AlbumId, nextTrackNumber);

        return Result.Success;
    }

    private static Error AlreadyInAlbum => AppErrors.Conflict("Album.TrackAlreadyAdded", "Track is already in the album.");
}
