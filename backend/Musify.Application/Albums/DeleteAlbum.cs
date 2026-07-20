using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;

namespace Musify.Application.Albums
{
    public record DeleteAlbumCommand(long UserId, Guid AlbumId)
        : ICommand<ErrorOr<Success>>;

    public class DeleteAlbumCommandHandler(
        IDatabase database,
        ILogger<DeleteAlbumCommandHandler> logger)
        : ICommandHandler<DeleteAlbumCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(DeleteAlbumCommand request, CancellationToken cancellationToken)
        {
            var album = await database.UserAlbums
                .SingleOrDefaultAsync(a => a.Id == request.AlbumId, cancellationToken);
            if (album is null)
            {
                logger.LogDebug("Album {AlbumId} not found", request.AlbumId);
                return Error.NotFound();
            }

            if (album.OwnerUserId != request.UserId)
            {
                logger.LogWarning("Album {AlbumId} does not belong to user {UserId}", request.AlbumId, request.UserId);
                return Error.Unauthorized();
            }

            database.UserAlbums.Remove(album);

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to delete album {AlbumId}", request.AlbumId);
                return Error.Failure(description: $"Failed to delete album {request.AlbumId}");
            }

            logger.LogInformation("Deleted album {AlbumId} for user {UserId}", request.AlbumId, request.UserId);

            return new Success();
        }
    }
}
