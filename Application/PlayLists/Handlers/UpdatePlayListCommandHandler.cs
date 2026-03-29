using Ardalis.Result;
using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class UpdatePlayListCommandHandler(IEventBus eventBus, IDatabase database, IStorageService storageHandler,
        ILogger<UpdatePlayListCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
        : ICommandHandler<UpdatePlayListCommand, Result<PlayListResponse>>
    {
        public async ValueTask<Result<PlayListResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
        {
            var playListEntity = await database.PlayLists.FindAsync(
                [request.PlayListId],
                cancellationToken);
            if (playListEntity is null)
            {
                logger.LogInformation("Playlist {PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playListEntity.UserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of playlist {PlayListId}", request.UserId, request.PlayListId);
                return Result.Unauthorized();
            }

            UpdateName(playListEntity, request.NewName);
            UpdateDescription(playListEntity, request.NewDescription);
            var updatePictureResult = await UpdatePicture(playListEntity, request.NewPicture, cancellationToken);
            if (!updatePictureResult.IsSuccess)
                return updatePictureResult;

            try
            {
                database.PlayLists.Update(playListEntity);
                await database.SaveChangesAsync(cancellationToken);
                await PublishPlayListPictureEvent(playListEntity, cancellationToken);
                await RemoveOldPictures(playListEntity, cancellationToken);
            }
            catch (Exception exception)
            {
                await Rollback(playListEntity.OriginalPictureName, cancellationToken);
                logger.LogError(exception, "Failed to update playlist {PlayListId}", playListEntity.Id);
                return Result.Error($"Failed to update playlist {playListEntity.Id}");
            }

            logger.LogInformation("Updated playlist {PlayListId}", playListEntity.Id);
            return Result.Success(PlayListResponse.FromEntity(playListEntity));
        }

        void UpdateName(PlayList playList, string? newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                playList.Name = newName;
                playList.NormalizedName = newName.Trim().ToUpperInvariant();
            }
        }

        void UpdateDescription(PlayList playList, string? newDescription)
        {
            if (!string.IsNullOrWhiteSpace(newDescription))
                playList.Description = newDescription;
        }

        async Task<Result> UpdatePicture(PlayList playList, IFileData? newPicture, CancellationToken cancellationToken)
        {
            if (newPicture is null)
                return Result.Success();

            var pictureName = Guid.NewGuid() + newPicture.FileType;
            var pictureKey = Path.Combine(playListConfiguration.Routes.OriginalPicturesPath, pictureName);
            try
            {
                await storageHandler.UploadFileAsync(
                    newPicture.FileStream,
                    newPicture.ContentType,
                    storageConfiguration.BucketName,
                    pictureKey,
                    cancellationToken);
            }
            catch
            {
                return Result.Error("Failed to upload picture to storage");
            }

            playList.OriginalPictureName = pictureName;
            return Result.Success();
        }

        async Task<Result> PublishPlayListPictureEvent(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                    playList.Id,
                    storageConfiguration.BucketName,
                    Path.Combine(playListConfiguration.Routes.OriginalPicturesPath, playList.OriginalPictureName),
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
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish playlist picture update event for playlist {PlayListId}", playList.Id);
                return Result.Error($"Failed to publish playlist {playList.Id} picture update event");
            }
        }

        async Task RemoveOldPictures(PlayList playList, CancellationToken cancellationToken)
        {
            if (playList.SmallPictureName != playListConfiguration.Routes.PresetSmallPicture)
                await PublishRemoveFileEvent(Path.Combine(
                    playListConfiguration.Routes.SmallPicturesPath,
                    playList.SmallPictureName),
                    cancellationToken);
            if (playList.MediumPictureName != playListConfiguration.Routes.PresetMediumPicture)
                await PublishRemoveFileEvent(Path.Combine(
                    playListConfiguration.Routes.MediumPicturesPath,
                    playList.MediumPictureName),
                    cancellationToken);
            if (playList.LargePictureName != playListConfiguration.Routes.PresetLargePicture)
                await PublishRemoveFileEvent(Path.Combine(
                    playListConfiguration.Routes.LargePicturesPath,
                    playList.LargePictureName),
                    cancellationToken);
        }

        async Task Rollback(string pictureKey, CancellationToken cancellationToken)
        {
            await storageHandler.RemoveFileAsync(
                storageConfiguration.BucketName,
                pictureKey,
                cancellationToken);
            logger.LogInformation("Rolled back uploaded picture {PictureKey} from storage", pictureKey);
        }

        async Task<Result> PublishRemoveFileEvent(string key, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new RemoveFileEvent(
                    storageConfiguration.BucketName,
                    key), cancellationToken);
                return Result.NoContent();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish remove file event for key {Key}", key);
                return Result.Error($"Failed to publish remove file event for {key}");
            }
        }
    }
}