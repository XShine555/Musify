using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Abstractions.Application;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class CreatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        IStorageService storageService,
        ILogger<CreatePlayListCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
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

            var playList = CreatePlayListCommand.ToEntity(
                request,
                playListConfiguration.Routes.PresetOriginalPicture,
                playListConfiguration.Routes.PresetSmallPicture,
                playListConfiguration.Routes.PresetMediumPicture,
                playListConfiguration.Routes.PresetLargePicture);

            if (request.Picture is not null)
            {
                var uploadResult = await UploadPictureAsync(playList, request.Picture, cancellationToken);
                if (!uploadResult.IsSuccess)
                    return uploadResult;
            }

            try
            {
                await database.PlayLists.AddAsync(playList, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save playlist for user {UserId}", request.UserId);
                await RollbackAllPicturesAsync(playList, cancellationToken);
                return Result.Error($"Failed to create playlist for user {request.UserId}");
            }

            if (request.Picture is not null)
            {
                var publishResult = await PublishPictureUpdateEventAsync(playList, cancellationToken);
                if (!publishResult.IsSuccess)
                {
                    await TryRollbackOnPublishFailureAsync(playList, cancellationToken);
                    return publishResult;
                }
            }

            logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playList.Id, request.UserId);
            return Result.Created(PlayListApplicationResponse.FromEntity(playList));
        }

        async Task TryRollbackOnPublishFailureAsync(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                database.PlayLists.Remove(playList);
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to rollback playlist creation after publish failure for playlist {PlayListId}", playList.Id);
            }

            await RollbackAllPicturesAsync(playList, cancellationToken);
        }

        async Task<Result> UploadPictureAsync(PlayList playList, IFileData picture, CancellationToken cancellationToken)
        {
            var pictureName = Guid.NewGuid() + picture.FileType;
            var pictureKey = playListConfiguration.Routes.BuildOriginalPicturePath(pictureName);

            try
            {
                await storageService.UploadFileAsync(picture.FileStream, picture.ContentType, storageConfiguration.Bucket, pictureKey, cancellationToken);
                playList.OriginalPictureName = pictureName;
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload picture for playlist {PlayListId}", playList.Id);
                return Result.Error($"Failed to upload picture for playlist {playList.Id}");
            }
        }

        async Task<Result> PublishPictureUpdateEventAsync(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                var updateEvent = new UpdatePlayListPictureEvent(
                    playList.Id,
                    storageConfiguration.Bucket,
                    playListConfiguration.Routes.BuildOriginalPicturePath(playList.OriginalPictureName),
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

        async Task RollbackAllPicturesAsync(PlayList playList, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(playList.OriginalPictureName))
                return;

            if (playList.OriginalPictureName == playListConfiguration.Routes.PresetOriginalPicture)
                return;

            var originalPicturePath = playListConfiguration.Routes.BuildOriginalPicturePath(playList.OriginalPictureName);

            try
            {
                await storageService.RemoveFileAsync(storageConfiguration.Bucket, originalPicturePath, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to rollback picture upload for playlist {PlayListId}", playList.Id);
            }
        }
    }
}