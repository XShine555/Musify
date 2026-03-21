using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;

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
            {
                try
                {
                    await eventBus.PublishAsync(new RemoveFileEvent(
                        storageConfiguration.BucketName,
                        playList.SmallPictureKeyName), cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to publish remove file event for small picture of PlayList id={PlayListId}.", playList.Id);
                    return Result.Error($"Failed to publish remove file event for PlayList with id {playList.Id}.");
                }
            }
            if (playList.MediumPictureKeyName != playListConfiguration.Routes.PresetMediumPicture)
            {
                try
                {
                    await eventBus.PublishAsync(new RemoveFileEvent(
                        storageConfiguration.BucketName,
                        playList.MediumPictureKeyName), cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to publish remove file event for medium picture of PlayList id={PlayListId}.", playList.Id);
                    return Result.Error($"Failed to publish remove file event for PlayList with id {playList.Id}.");
                }
            }
            if (playList.LargePictureKeyName != playListConfiguration.Routes.PresetLargePicture)
            {
                try
                {
                    await eventBus.PublishAsync(new RemoveFileEvent(
                        storageConfiguration.BucketName,
                        playList.LargePictureKeyName), cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to publish remove file event for large picture of PlayList id={PlayListId}.", playList.Id);
                    return Result.Error($"Failed to publish remove file event for PlayList with id {playList.Id}.");
                }
            }

            var newImageId = Guid.NewGuid();
            var pictureName = $"{newImageId}{request.NewPicture.FileType}";
            var newImageStorageKey = Path.Join(playListConfiguration.Routes.OriginalPictures, pictureName);

            var uploadResult = await storageHandler.UploadFileAsync(
                request.NewPicture.FileStream,
                request.NewPicture.ContentType,
                storageConfiguration.BucketName,
                newImageStorageKey,
                cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for PlayList with Id={PlayListId} to storage. Storage handler returned error: {ErrorMessage}",
                    playList.Id, string.Join("; ", uploadResult.Errors));
                return Result.Error($"Failed to upload picture for PlayList with Id {playList.Id} to storage.");
            }

            var smallPictureKeyName = Path.Combine(
                playListConfiguration.Routes.SmallPictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");
            var mediumPictureKeyName = Path.Combine(
                playListConfiguration.Routes.MediumPictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");
            var largePictureKeyName = Path.Combine(
                playListConfiguration.Routes.LargePictures,
                $"{playList.Id}.{pictureHandler.FileExtension}");

            try
            {
                await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                    playList.Id,
                    storageConfiguration.BucketName,
                    newImageStorageKey,
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
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish resize picture event for PlayList id={PlayListId}.", playList.Id);
                return Result.Error($"Failed to publish resize picture event for PlayList with id {playList.Id}.");
            }

            database.PlayLists.Update(playList);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated PlayList with id={PlayListId} and picture key {PictureKeyName}.", playList.Id, newImageStorageKey);
            return Result.Success(PlayListResponse.FromEntity(playList));
        }
    }
}