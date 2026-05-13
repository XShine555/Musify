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
    public class UpdatePlayListCommandHandler(IEventBus eventBus, IDatabase database, IStorageService storageHandler,
        ILogger<UpdatePlayListCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
        : ICommandHandler<UpdatePlayListCommand, Result<PlayListApplicationResponse>>
    {
        record UpdatePictureOutcome(Result Result, bool PictureUpdated, string? UploadedPictureKey);
        record PreviousPlayListState(string Name, string NormalizedName, string Description, string OriginalPictureName);

        public async ValueTask<Result<PlayListApplicationResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
        {
            var playListEntity = await database.PlayLists.SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);
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

            var previousState = CapturePreviousState(playListEntity);

            UpdateName(playListEntity, request.NewName);
            UpdateDescription(playListEntity, request.NewDescription);

            var pictureOutcome = await UpdatePicture(playListEntity, request.NewPicture, cancellationToken);
            if (!pictureOutcome.Result.IsSuccess)
                return pictureOutcome.Result;

            try
            {
                database.PlayLists.Update(playListEntity);
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                if (pictureOutcome.PictureUpdated && pictureOutcome.UploadedPictureKey is not null)
                    await Rollback(pictureOutcome.UploadedPictureKey, cancellationToken);
                logger.LogError(exception, "Failed to update playlist {PlayListId}", playListEntity.Id);
                return Result.Error($"Failed to update playlist {playListEntity.Id}");
            }

            if (pictureOutcome.PictureUpdated)
            {
                var publishResult = await PublishPlayListPictureEvent(playListEntity, cancellationToken);
                if (!publishResult.IsSuccess)
                {
                    await TryRollbackAfterPublishFailureAsync(
                        playListEntity,
                        previousState,
                        pictureOutcome.UploadedPictureKey,
                        cancellationToken);
                    return Result.Error($"Failed to update playlist {playListEntity.Id}");
                }
            }

            logger.LogInformation("Updated playlist {PlayListId}", playListEntity.Id);
            return Result.Success(PlayListApplicationResponse.FromEntity(playListEntity));
        }

        void UpdateName(PlayList playList, string? newName)
        {
            if (newName is null)
                return;

            playList.Name = newName;
            playList.NormalizedName = newName.Trim().ToUpperInvariant();
        }

        void UpdateDescription(PlayList playList, string? newDescription)
        {
            if (newDescription is null)
                return;

            playList.Description = newDescription;
        }

        static PreviousPlayListState CapturePreviousState(PlayList playList)
            => new(playList.Name, playList.NormalizedName, playList.Description, playList.OriginalPictureName);

        async Task<UpdatePictureOutcome> UpdatePicture(
            PlayList playList,
            IFileData? newPicture,
            CancellationToken cancellationToken)
        {
            if (newPicture is null)
                return new UpdatePictureOutcome(Result.Success(), false, null);

            var pictureName = Guid.NewGuid() + newPicture.FileType;
            var pictureKey = playListConfiguration.Routes.BuildOriginalPicturePath(pictureName);
            try
            {
                await storageHandler.UploadFileAsync(
                    newPicture.FileStream,
                    newPicture.ContentType,
                    storageConfiguration.Bucket,
                    pictureKey,
                    cancellationToken);
            }
            catch
            {
                return new UpdatePictureOutcome(Result.Error("Failed to upload picture to storage"), false, null);
            }

            playList.OriginalPictureName = pictureName;
            return new UpdatePictureOutcome(Result.Success(), true, pictureKey);
        }

        async Task<Result> PublishPlayListPictureEvent(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
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

        async Task Rollback(string pictureKey, CancellationToken cancellationToken)
        {
            await storageHandler.RemoveFileAsync(
                storageConfiguration.Bucket,
                pictureKey,
                cancellationToken);
            logger.LogInformation("Rolled back uploaded picture {PictureKey} from storage", pictureKey);
        }

        async Task TryRollbackAfterPublishFailureAsync(
            PlayList playList,
            PreviousPlayListState previousState,
            string? uploadedPictureKey,
            CancellationToken cancellationToken)
        {
            playList.Name = previousState.Name;
            playList.NormalizedName = previousState.NormalizedName;
            playList.Description = previousState.Description;
            playList.OriginalPictureName = previousState.OriginalPictureName;

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to rollback playlist state after publish failure for playlist {PlayListId}", playList.Id);
            }

            if (uploadedPictureKey is null)
                return;

            try
            {
                await Rollback(uploadedPictureKey, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to rollback uploaded picture after publish failure for playlist {PlayListId}", playList.Id);
            }
        }
    }
}
