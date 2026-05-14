using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Application.UploadIntents;
using Musify.Application.Extensions;
using Musify.Application.Services;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class CreatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        IStorageService storageService,
        ILogger<CreatePlayListCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<CreatePlayListCommand, Result<PlayListApplicationResponse>>
    {
        public async ValueTask<Result<PlayListApplicationResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            string originalPictureName;
            UploadIntent? pictureIntent = null;

            if (request.PictureIntentId.HasValue)
            {
                var validation = await uploadIntentValidator.ValidateAndLoadAsync(
                    uploadIntentConfiguration,
                    request.PictureIntentId.Value, request.UserId, cancellationToken);
                if (!validation.IsSuccess)
                    return validation.As<UploadIntent, PlayListApplicationResponse>();

                pictureIntent = validation.Value;
                originalPictureName = pictureIntent.ObjectName;

                var finalKey = playListConfiguration.Routes.BuildOriginalPicturePath(request.UserId, originalPictureName);
                try
                {
                    await storageService.CopyFileAsync(pictureIntent.Bucket, pictureIntent.Key, pictureIntent.Bucket, finalKey, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to copy playlist picture from temp {TempKey} to final {FinalKey}", pictureIntent.Key, finalKey);
                    return Result.Error("Failed to move uploaded file to its final location.");
                }
            }
            else
            {
                originalPictureName = playListConfiguration.Routes.PresetOriginalPicture;
            }

            var playList = new PlayList
            {
                UserId = request.UserId,
                Name = request.Name,
                NormalizedName = request.Name.Trim().ToUpperInvariant(),
                Description = request.Description,
                OriginalPictureName = originalPictureName,
                SmallPictureName = playListConfiguration.Routes.PresetSmallPicture,
                MediumPictureName = playListConfiguration.Routes.PresetMediumPicture,
                LargePictureName = playListConfiguration.Routes.PresetLargePicture
            };

            await database.PlayLists.AddAsync(playList, cancellationToken);

            if (pictureIntent is not null)
            {
                pictureIntent.Status = UploadIntentStatus.Consumed;

                var publishResult = await PublishPictureUpdateEventAsync(playList, cancellationToken);
                if (!publishResult.IsSuccess)
                    return publishResult;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save playlist for user {UserId}", request.UserId);
                return Result.Error($"Failed to create playlist for user {request.UserId}");
            }

            logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playList.Id, request.UserId);
            return Result.Created(PlayListApplicationResponse.FromEntity(playList));
        }

        async Task<Result> PublishPictureUpdateEventAsync(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                var updateEvent = new UpdatePlayListPictureEvent(
                    playList.Id,
                    storageConfiguration.Bucket,
                    playListConfiguration.Routes.BuildOriginalPicturePath(playList.UserId, playList.OriginalPictureName),
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
                        playListConfiguration.PicturesSizes.LargePictureHeight));

                await eventBus.PublishAsync(updateEvent, cancellationToken);
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish picture update event for playlist {PlayListId}", playList.Id);
                return Result.Error($"Failed to publish picture update event for playlist {playList.Id}");
            }
        }
    }
}