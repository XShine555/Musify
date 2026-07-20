using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;

namespace Musify.Application.Albums
{
    public record UpdateAlbumCommand(
        long UserId,
        Guid AlbumId,
        string Title,
        string? Description,
        int? ReleaseYear)
        : ICommand<ErrorOr<AlbumApplicationResponse>>;

    public class UpdateAlbumCommandHandler(
        IDatabase database,
        ILogger<UpdateAlbumCommandHandler> logger)
        : ICommandHandler<UpdateAlbumCommand, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(UpdateAlbumCommand request, CancellationToken cancellationToken)
        {
            var album = await database.UserAlbums
                .SingleOrDefaultAsync(a => a.Id == request.AlbumId, cancellationToken);
            if (album is null)
            {
                logger.LogInformation("Album {AlbumId} not found", request.AlbumId);
                return Error.NotFound();
            }

            if (album.OwnerUserId != request.UserId)
            {
                logger.LogWarning("Album {AlbumId} does not belong to user {UserId}", request.AlbumId, request.UserId);
                return Error.Unauthorized();
            }

            var title = request.Title.Trim();

            album.Title = title;
            album.NormalizedTitle = title.ToUpperInvariant();
            album.Description = request.Description;
            album.ReleaseYear = request.ReleaseYear;

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update album {AlbumId}", request.AlbumId);
                return Error.Failure(description: $"Failed to update album {request.AlbumId}");
            }

            var trackCount = await database.AlbumHasTracks
                .AsNoTracking()
                .CountAsync(albumTrack => albumTrack.AlbumId == album.Id, cancellationToken);

            return AlbumApplicationResponse.FromEntity(album, trackCount);
        }
    }
}
