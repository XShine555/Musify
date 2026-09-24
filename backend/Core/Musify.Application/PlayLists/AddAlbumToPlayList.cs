using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists
{
    /// <summary>Appends every track of an album to a playlist in album order, skipping the ones it already has.</summary>
    public record AddAlbumToPlayListCommand(long UserId, Guid PlayListId, Guid AlbumId)
        : ICommand<ErrorOr<AddAlbumToPlayListResponse>>;

    public class AddAlbumToPlayListCommandHandler(
        IDatabase database,
        ILogger<AddAlbumToPlayListCommandHandler> logger)
        : ICommandHandler<AddAlbumToPlayListCommand, ErrorOr<AddAlbumToPlayListResponse>>
    {
        public async ValueTask<ErrorOr<AddAlbumToPlayListResponse>> Handle(AddAlbumToPlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.FindOwnedAsync(request.PlayListId, request.UserId, cancellationToken);
            if (playList.IsError)
                return playList.Errors;

            var albumExists = await database.Albums
                .AsNoTracking()
                .Active()
                .AnyAsync(album => album.Id == request.AlbumId, cancellationToken);
            if (!albumExists)
                return AppErrors.NotFound("Album", request.AlbumId);

            var albumTrackIds = await database.AlbumHasTracks
                .AsNoTracking()
                .Where(albumTrack => albumTrack.AlbumId == request.AlbumId && albumTrack.Track.LifeCycleStatus == Domain.ValueObjects.LifeCycleStatus.Active)
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .Select(albumTrack => albumTrack.TrackId)
                .ToListAsync(cancellationToken);

            var playListTracksQuery = database.PlayListHasTracks
                .Where(link => link.PlayListId == request.PlayListId);

            var existingTrackIds = (await playListTracksQuery
                .Select(link => link.TrackId)
                .ToListAsync(cancellationToken)).ToHashSet();

            var newTrackIds = albumTrackIds
                .Where(trackId => !existingTrackIds.Contains(trackId))
                .ToList();

            if (newTrackIds.Count == 0)
                return new AddAlbumToPlayListResponse(0);

            // Playlist positions are internal indexes, so they start at 0.
            var nextPosition = (await playListTracksQuery
                .MaxAsync(link => (int?)link.Position, cancellationToken) ?? -1) + 1;

            foreach (var trackId in newTrackIds)
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
                return saved.Errors;

            logger.LogInformation("Added {Count} tracks from album {AlbumId} to playlist {PlayListId}",
                newTrackIds.Count, request.AlbumId, request.PlayListId);

            return new AddAlbumToPlayListResponse(newTrackIds.Count);
        }
    }
}
