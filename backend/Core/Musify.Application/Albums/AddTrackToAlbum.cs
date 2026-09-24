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
        var album = await database.Albums
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == request.AlbumId, cancellationToken);
        if (album == null)
            return AppErrors.NotFound("Album", request.AlbumId);

        if (album.OwnerUserId != request.UserId)
            return AppErrors.Forbidden("Album", request.AlbumId);

        var track = await database.Tracks
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);
        if (track == null)
            return AppErrors.NotFound("Track", request.TrackId);

        if (track.OwnerUserId != request.UserId)
            return AppErrors.Forbidden("Track", request.TrackId);

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
