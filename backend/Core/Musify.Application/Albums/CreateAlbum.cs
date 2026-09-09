using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Albums
{
    public record CreateAlbumCommand(
        long UserId,
        string Title,
        string? Description,
        int? ReleaseYear)
        : ICommand<ErrorOr<AlbumApplicationResponse>>;

    public class CreateAlbumCommandHandler(
        IDatabase database,
        ILogger<CreateAlbumCommandHandler> logger)
        : ICommandHandler<CreateAlbumCommand, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(CreateAlbumCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Error.NotFound(description: $"User {request.UserId} not found");
            }

            var title = request.Title.Trim();

            var album = new UserAlbum
            {
                OwnerUserId = request.UserId,
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                Description = request.Description,
                ReleaseYear = request.ReleaseYear
            };

            await database.UserAlbums.AddAsync(album, cancellationToken);

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save album for user {UserId}", request.UserId);
                return Error.Failure(description: $"Failed to create album for user {request.UserId}");
            }

            logger.LogInformation("Created album {AlbumId} for user {UserId}", album.Id, request.UserId);

            return AlbumApplicationResponse.FromEntity(album, trackCount: 0);
        }
    }
}
