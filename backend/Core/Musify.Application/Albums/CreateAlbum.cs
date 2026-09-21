using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Shared;
using Musify.Application.Events;
using Musify.Application.Albums.Responses;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

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
        ApplicationStorageConfiguration storageConfiguration,
        AlbumConfiguration albumConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
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

            var pictureIntent = await uploadIntentValidator.ValidateAndLoadAsync(
                uploadIntentConfiguration, request.PictureIntentId, request.UserId, cancellationToken);
            if (pictureIntent.IsError)
                return pictureIntent.Errors;

            var intent = pictureIntent.Value;
            var title = request.Title.Trim();

            var album = new Album
            {
                OwnerUserId = request.UserId,
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                Description = request.Description,
                ReleaseYear = request.ReleaseYear,
                Pictures = new AlbumPictures
                {
                    OriginalName = intent.ObjectName,
                    SmallName = intent.ObjectName,
                    MediumName = intent.ObjectName,
                    LargeName = intent.ObjectName
                }
            };

            await database.Albums.AddAsync(album, cancellationToken);

            var finalPictureKey = albumConfiguration.Routes.BuildOriginalPicturePath(request.UserId, intent.ObjectName);
            var publishResult = await PublishCreateAlbumEventAsync(album.Id, intent, finalPictureKey, cancellationToken);
            if (publishResult.IsError)
                return publishResult.Errors;

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

        private async Task<ErrorOr<Success>> PublishCreateAlbumEventAsync(
            Guid albumId,
            UploadIntent pictureIntent,
            string finalPictureKey,
            CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(
                    new CreateAlbumResourcesEvent(
                        albumId,
                        pictureIntent.Id,
                        storageConfiguration.Bucket,
                        pictureIntent.Key,
                        finalPictureKey,
                        new ImageSize(
                            albumConfiguration.Routes.SmallPicturesPath,
                            albumConfiguration.PicturesSizes.SmallPictureWidth,
                            albumConfiguration.PicturesSizes.SmallPictureHeight),
                        new ImageSize(
                            albumConfiguration.Routes.MediumPicturesPath,
                            albumConfiguration.PicturesSizes.MediumPictureWidth,
                            albumConfiguration.PicturesSizes.MediumPictureHeight),
                        new ImageSize(
                            albumConfiguration.Routes.LargePicturesPath,
                            albumConfiguration.PicturesSizes.LargePictureWidth,
                            albumConfiguration.PicturesSizes.LargePictureHeight)),
                    cancellationToken);
                return new Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish create album event for album {AlbumId}", albumId);
                return Error.Failure(description: $"Failed to publish create album event for album {albumId}");
            }
        }
    }
}
