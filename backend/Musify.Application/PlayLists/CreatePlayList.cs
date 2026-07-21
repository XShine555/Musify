using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Shared;
using Musify.Application.Events;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists
{
    public record CreatePlayListCommand(
        long UserId,
        string Name,
        string? Description,
        Guid? PictureIntentId)
        : ICommand<ErrorOr<PlayListApplicationResponse>>;

    public class CreatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<CreatePlayListCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<CreatePlayListCommand, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Error.NotFound(description: $"User {request.UserId} not found");
            }

            UploadIntent? pictureIntent = null;
            if (request.PictureIntentId.HasValue)
            {
                var validation = await uploadIntentValidator.ValidateAndLoadAsync(
                    uploadIntentConfiguration,
                    request.PictureIntentId.Value, request.UserId, cancellationToken);
                if (validation.IsError)
                    return validation.Errors;

                pictureIntent = validation.Value;
            }

            var playList = new PlayList
            {
                UserId = request.UserId,
                Name = request.Name,
                NormalizedName = request.Name.Trim().ToUpperInvariant(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Pictures = pictureIntent == null ? null
                    : new PlayListPictures { OriginalName = pictureIntent.ObjectName }
            };

            await database.PlayLists.AddAsync(playList, cancellationToken);

            if (pictureIntent != null)
            {
                var finalPictureKey = playListConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureIntent.ObjectName);
                var publishResult = await PublishCreatePlayListEventAsync(
                    playList.Id, pictureIntent, finalPictureKey, cancellationToken);
                if (publishResult.IsError)
                    return publishResult.Errors;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save playlist for user {UserId}", request.UserId);
                return Error.Failure(description: $"Failed to create playlist for user {request.UserId}");
            }

            logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playList.Id, request.UserId);
            return PlayListApplicationResponse.FromEntity(playList);
        }

        async Task<ErrorOr<Success>> PublishCreatePlayListEventAsync(
            Guid playListId,
            UploadIntent pictureIntent,
            string finalPictureKey,
            CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(
                    new CreatePlayListResourcesEvent(
                        playListId,
                        pictureIntent.Id,
                        storageConfiguration.Bucket,
                        pictureIntent.Key,
                        finalPictureKey,
                        new ImageSize(
                            playListConfiguration.Routes.SmallPicturesPath,
                            playListConfiguration.PicturesSizes.SmallPictureWidth,
                            playListConfiguration.PicturesSizes.SmallPictureHeight),
                        new ImageSize(
                            playListConfiguration.Routes.MediumPicturesPath,
                            playListConfiguration.PicturesSizes.MediumPictureWidth,
                            playListConfiguration.PicturesSizes.MediumPictureHeight),
                        new ImageSize(
                            playListConfiguration.Routes.LargePicturesPath,
                            playListConfiguration.PicturesSizes.LargePictureWidth,
                            playListConfiguration.PicturesSizes.LargePictureHeight)),
                    cancellationToken);
                return new Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish create playlist event for playlist {PlayListId}", playListId);
                return Error.Failure(description: $"Failed to publish create playlist event for playlist {playListId}");
            }
        }
    }
}
