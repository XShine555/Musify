using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
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
    public class UpdatePlayListCommandHandler(IEventBus eventBus, IDatabase database, IStorageHandler storageHandler,
        ILogger<UpdatePlayListCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration, IPictureHandler pictureHandler)
        : IRequestHandler<UpdatePlayListCommand, Task<Result<PlayListResponse>> >
    {
        public async Task<Result<PlayListResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);

            if (playList is null)
            {
                logger.LogInformation("PlayList with id {PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("User with id {UserId} is not the owner of the PlayList with id {PlayListId}", request.UserId, request.PlayListId);
                return Result.Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(request.NewName))
            {
                playList.Name = request.NewName;
                playList.NormalizedName = request.NewName.Trim().ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(request.NewDescription))
                playList.Description = request.NewDescription;

            if (request.NewPicture is null)
            {
                database.PlayLists.Update(playList);
                await database.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Updated PlayList with id={PlayListId} without picture changes.", playList.Id);
                return Result.Success(PlayListResponse.FromEntity(playList));
            }

            if (playList.SmallPictureKeyName != playListConfiguration.Routes.PresetSmallPicture)
                await PublishRemoveFileEvent(Path.Combine(
                    playListConfiguration.Routes.SmallPictures,
                    playList.SmallPictureKeyName), cancellationToken);
            if (playList.MediumPictureKeyName != playListConfiguration.Routes.PresetMediumPicture)
                await PublishRemoveFileEvent(Path.Combine(
                    playListConfiguration.Routes.MediumPictures,
                    playList.MediumPictureKeyName), cancellationToken);
            if (playList.LargePictureKeyName != playListConfiguration.Routes.PresetLargePicture)
                await PublishRemoveFileEvent(Path.Combine(
                    playListConfiguration.Routes.LargePictures,
                    playList.LargePictureKeyName), cancellationToken);

            var newImageId = Guid.NewGuid();
            var pictureName = newImageId + request.NewPicture.FileType;
            var newImageStorageKey = Path.Join(playListConfiguration.Routes.OriginalPictures, pictureName);

            var uploadResult = await UploadFile(request.NewPicture, newImageStorageKey, cancellationToken);
            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for PlayList with Id={PlayListId} to storage. Storage handler returned error: {ErrorMessage}",
                    playList.Id, string.Join("; ", uploadResult.Errors));
                return Result.Error($"Failed to upload picture for PlayList with Id {playList.Id} to storage.");
            }

            try
            {
                await PublishUpdateEvent(newImageStorageKey, playList, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish update picture event for PlayList with id {PlayListId}.", playList.Id);
                return Result.Error($"Failed to publish update picture event for PlayList with id {playList.Id}.");
            }

            database.PlayLists.Update(playList);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated PlayList with id={PlayListId} and picture key {PictureKeyName}.", playList.Id, newImageStorageKey);
            return Result.Success(PlayListResponse.FromEntity(playList));
        }

        async Task<Result> PublishRemoveFileEvent(string keyName, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new RemoveFileEvent(
                    storageConfiguration.BucketName,
                    Path.Combine(
                        playListConfiguration.Routes.ParentFolders,
                        keyName)), cancellationToken);

                return Result.NoContent();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish remove file event for key {KeyName}.", keyName);
                return Result.Error($"Failed to publish remove file event for key {keyName}.");
            }
        }

        async Task<Result<string>> UploadFile(IFileData picture, string pictureKeyName, CancellationToken cancellationToken)
        {
            var uploadFileResult = await storageHandler.UploadFileAsync(
                picture.FileStream,
                picture.ContentType,
                storageConfiguration.BucketName,
                pictureKeyName,
                cancellationToken);

            return uploadFileResult;
        }

        async Task PublishUpdateEvent(string originalPictureKeyName, PlayList playList, CancellationToken cancellationToken)
        {
            var smallPictureKeyName = Path.Combine(
                playListConfiguration.Routes.SmallPictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");
            var mediumPictureKeyName = Path.Combine(
                playListConfiguration.Routes.MediumPictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");
            var largePictureKeyName = Path.Combine(
                playListConfiguration.Routes.LargePictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");

            await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                playList.Id,
                storageConfiguration.BucketName,
                originalPictureKeyName,
                smallPictureKeyName,
                playListConfiguration.PicturesSizes.SmallPictureWidth,
                playListConfiguration.PicturesSizes.SmallPictureHeight,
                mediumPictureKeyName,
                playListConfiguration.PicturesSizes.MediumPictureWidth,
                playListConfiguration.PicturesSizes.MediumPictureHeight,
                largePictureKeyName,
                playListConfiguration.PicturesSizes.LargePictureWidth,
                playListConfiguration.PicturesSizes.LargePictureHeight), cancellationToken);
        }
    }
}