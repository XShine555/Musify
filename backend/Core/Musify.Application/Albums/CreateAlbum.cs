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
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums
{
    public record CreateAlbumCommand(
        long UserId,
        string Title,
        string? Description,
        int? ReleaseYear,
        Guid PictureIntentId)
        : ICommand<ErrorOr<AlbumApplicationResponse>>;

    public class CreateAlbumCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<CreateAlbumCommandHandler> logger,
        AlbumConfiguration albumConfiguration)
        : ICommandHandler<CreateAlbumCommand, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(CreateAlbumCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == request.UserId, cancellationToken);
            if (!userExists)
                return AppErrors.NotFound("User", request.UserId);

            var prepared = await PictureSourceChange.PrepareAsync(
                uploadIntentValidator, request.PictureIntentId, request.UserId, UploadIntentPurpose.AlbumPicture, albumConfiguration, cancellationToken);
            if (prepared.IsError)
                return prepared.Errors;

            var picture = prepared.Value;
            var title = request.Title.Trim();

            var album = new Album
            {
                OwnerUserId = request.UserId,
                Title = title,
                NormalizedTitle = TextNormalizer.Normalize(title),
                Description = request.Description,
                ReleaseYear = request.ReleaseYear,
                Pictures = picture.Pictures
            };

            await database.Albums.AddAsync(album, cancellationToken);

            await eventBus.PublishAsync(
                new CreateAlbumResourcesEvent(
                    album.Id, picture.Intent.Id, picture.Intent.Bucket, picture.Intent.Key, picture.FinalKey, picture.Sizes),
                cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created album {AlbumId} for user {UserId}", album.Id, request.UserId);

            return AlbumApplicationResponse.FromEntity(album, trackCount: 0);
        }
    }
}
