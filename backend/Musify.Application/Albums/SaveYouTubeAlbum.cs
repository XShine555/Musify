using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Domain.Entities;

namespace Musify.Application.Albums
{
    public record SaveYouTubeAlbumCommand(long UserId, string AlbumId)
        : ICommand<ErrorOr<AlbumApplicationResponse>>;

    public class SaveYouTubeAlbumCommandHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        YouTubeTrackProvisioner provisioner,
        ILogger<SaveYouTubeAlbumCommandHandler> logger)
        : ICommandHandler<SaveYouTubeAlbumCommand, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(SaveYouTubeAlbumCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == request.UserId, cancellationToken);
            if (!userExists)
                return Error.NotFound(description: $"User {request.UserId} not found");

            var alreadySaved = await database.UserAlbums
                .AsNoTracking()
                .AnyAsync(album => album.OwnerUserId == request.UserId && album.ExternalId == request.AlbumId, cancellationToken);
            if (alreadySaved)
            {
                logger.LogInformation("Album {AlbumId} is already saved for user {UserId}", request.AlbumId, request.UserId);
                return Error.Conflict(description: "The album is already in your library.");
            }

            var detailResult = await youTubeMusicService.GetAlbumAsync(request.AlbumId, cancellationToken);
            if (detailResult.IsError)
                return detailResult.Errors;

            var detail = detailResult.Value;
            if (detail.Tracks.Count == 0)
                return Error.NotFound(description: "The album has no playable tracks.");

            var title = Truncate(detail.Album.Title, 200);
            var album = new UserAlbum
            {
                OwnerUserId = request.UserId,
                ExternalId = request.AlbumId,
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                Description = Truncate(detail.Description, 256),
                ReleaseYear = detail.Album.ReleaseYear
            };

            await database.UserAlbums.AddAsync(album, cancellationToken);

            var linkedTrackIds = new HashSet<Guid>();
            foreach (var track in detail.Tracks)
            {
                var provisionResult = await provisioner.GetOrCreateAsync(track.VideoId, cancellationToken);
                if (provisionResult.IsError)
                {
                    logger.LogWarning("Skipped track {VideoId} of album {AlbumId}: {Error}",
                        track.VideoId, request.AlbumId, provisionResult.FirstError.Description);
                    continue;
                }

                if (!linkedTrackIds.Add(provisionResult.Value.Track.Id))
                    continue;

                await database.AlbumHasTracks.AddAsync(new AlbumHasTrack
                {
                    AlbumId = album.Id,
                    TrackId = provisionResult.Value.Track.Id,
                    TrackNumber = track.TrackNumber
                }, cancellationToken);
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save YouTube album {AlbumId} for user {UserId}", request.AlbumId, request.UserId);
                return Error.Failure(description: $"Failed to save album {request.AlbumId}");
            }

            var trackCount = await database.AlbumHasTracks
                .AsNoTracking()
                .CountAsync(albumTrack => albumTrack.AlbumId == album.Id, cancellationToken);

            logger.LogInformation("Saved YouTube album {AlbumId} as {AlbumGuid} with {TrackCount} tracks",
                request.AlbumId, album.Id, trackCount);

            return AlbumApplicationResponse.FromEntity(album, trackCount);
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
