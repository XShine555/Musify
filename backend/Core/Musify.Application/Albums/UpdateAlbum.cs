using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;

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
            var found = await database.Albums.FindOwnedAsync(request.AlbumId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var album = found.Value;
            var title = request.Title.Trim();

            // This is a full replace of the plain fields; the picture has its own command.
            album.Title = title;
            album.NormalizedTitle = TextNormalizer.Normalize(title);
            album.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            album.ReleaseYear = request.ReleaseYear;

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated album {AlbumId}", album.Id);

            return await database.Albums
                .AsNoTracking()
                .Where(a => a.Id == album.Id)
                .SelectResponse()
                .SingleAsync(cancellationToken);
        }
    }
}
