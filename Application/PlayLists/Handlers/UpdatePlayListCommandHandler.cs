using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.Extensions;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class UpdatePlayListCommandHandler(IEventBus eventBus, IDatabase database, IStorageHandler storageHandler,
        ILogger<UpdatePlayListCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
        : IRequestHandler<UpdatePlayListCommand, Task<Result<PlayListResponse>> >
    {
        public async Task<Result<PlayListResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
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
                return updatePictureResult.ToErrorResult();

            try
            {
                database.PlayLists.Update(playListEntity);
                await database.SaveChangesAsync(cancellationToken);
                await PublishPlayListPictureEvent(playListEntity, updatePictureResult.Value, cancellationToken);
                await RemoveOldPictures(playListEntity, cancellationToken);
            }
            catch (Exception exception)
            {
                await Rollback(updatePictureResult.Value, cancellationToken);
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

        async Task<Result<string>> UpdatePicture(PlayList playList, IFileData? newPicture, CancellationToken cancellationToken)
        {
            if (newPicture is null)
                return Result.Success();

            var pictureName = Guid.NewGuid() + newPicture.FileType;
            var pictureKey = Path.Combine(playListConfiguration.Routes.OriginalPicturesPath, pictureName);
            var uploadResult = await storageHandler.UploadFileAsync(
                newPicture.FileStream,
                newPicture.ContentType,
                storageConfiguration.BucketName,
                pictureKey,
                cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for playlist {PlayListId} to storage: {ErrorMessage}",
                    playList.Id, string.Join("; ", uploadResult.Errors));
                return Result.Error($"Failed to upload playlist {playList.Id} picture");
            }
            playList.OriginalPictureName = pictureName;
            return Result.Success(pictureKey);
        }

        async Task<Result> PublishPlayListPictureEvent(PlayList playList, string pictureKey, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                    playList.Id,
                    storageConfiguration.BucketName,
                    pictureKey,
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