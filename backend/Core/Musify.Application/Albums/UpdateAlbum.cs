using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Pictures;
using Musify.Application.Services;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums
{
    public record UpdateAlbumCommand(
        long UserId,
        Guid AlbumId,
        string Title,
        string? Description,
        int? ReleaseYear,
        Guid? NewPictureIntentId)
        : ICommand<ErrorOr<AlbumApplicationResponse>>;

    public class UpdateAlbumCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<UpdateAlbumCommandHandler> logger,
        AlbumConfiguration albumConfiguration)
        : ICommandHandler<UpdateAlbumCommand, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(UpdateAlbumCommand request, CancellationToken cancellationToken)
        {
            var found = await database.Albums.FindOwnedAsync(request.AlbumId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var album = found.Value;
            var title = request.Title.Trim();

            album.Title = title;
            album.NormalizedTitle = TextNormalizer.Normalize(title);
            album.Description = request.Description;
            album.ReleaseYear = request.ReleaseYear;

            if (request.NewPictureIntentId is { } intentId)
            {
                var prepared = await PictureSourceChange.PrepareAsync(
                    uploadIntentValidator, intentId, request.UserId, UploadIntentPurpose.AlbumPicture, albumConfiguration, cancellationToken);
                if (prepared.IsError)
                    return prepared.Errors;

                var picture = prepared.Value;
                album.Pictures = picture.Pictures;

                await eventBus.PublishAsync(
                    new UpdateAlbumPictureSourceEvent(
                        album.Id, picture.Intent.Id, picture.Intent.Bucket, picture.Intent.Key, picture.FinalKey, picture.Sizes),
                    cancellationToken);
            }

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
