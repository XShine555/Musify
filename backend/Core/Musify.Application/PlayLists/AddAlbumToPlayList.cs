using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists
{
    public record AddAlbumToPlayListCommand(long UserId, Guid PlayListId, Guid AlbumId)
        : ICommand<ErrorOr<AddAlbumToPlayListResponse>>;

    public class AddAlbumToPlayListCommandHandler(
        IDatabase database,
        ILogger<AddAlbumToPlayListCommandHandler> logger)
        : ICommandHandler<AddAlbumToPlayListCommand, ErrorOr<AddAlbumToPlayListResponse>>
    {
        public async ValueTask<ErrorOr<AddAlbumToPlayListResponse>> Handle(AddAlbumToPlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);
            if (playList == null)
            {
                logger.LogInformation("Playlist {PlayListId} not found", request.PlayListId);
                return Error.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of playlist {PlayListId}", request.UserId, request.PlayListId);
                return Error.Unauthorized();
            }

            var albumExists = await database.Albums
                .AsNoTracking()
                .AnyAsync(album => album.Id == request.AlbumId, cancellationToken);
            if (!albumExists)
            {
                logger.LogInformation("Album {AlbumId} not found", request.AlbumId);
                return Error.NotFound(description: $"Album {request.AlbumId} not found");
            }

            var albumTrackIds = await database.AlbumHasTracks
                .AsNoTracking()
                .Where(albumTrack => albumTrack.AlbumId == request.AlbumId)
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .Select(albumTrack => albumTrack.TrackId)
                .ToListAsync(cancellationToken);

            var playListTracksQuery = database.PlayListHasTracks
                .Where(plt => plt.PlayListId == request.PlayListId);

            var existingTrackIds = (await playListTracksQuery
                .Select(plt => plt.TrackId)
                .ToListAsync(cancellationToken)).ToHashSet();

            var newTrackIds = albumTrackIds
                .Where(trackId => !existingTrackIds.Contains(trackId))
                .ToList();

            if (newTrackIds.Count == 0)
                return new AddAlbumToPlayListResponse(0);

            var nextPosition = existingTrackIds.Count > 0
                ? await playListTracksQuery.MaxAsync(plt => plt.Position, cancellationToken) + 1
                : 0;

            await database.PlayListHasTracks.AddRangeAsync(
                newTrackIds.Select((trackId, index) => new PlayListHasTrack
                {
                    PlayListId = request.PlayListId,
                    TrackId = trackId,
                    Position = nextPosition + index
                }),
                cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Added {Count} tracks from album {AlbumId} to playlist {PlayListId}",
                newTrackIds.Count, request.AlbumId, request.PlayListId);

            return new AddAlbumToPlayListResponse(newTrackIds.Count);
        }
    }
}
