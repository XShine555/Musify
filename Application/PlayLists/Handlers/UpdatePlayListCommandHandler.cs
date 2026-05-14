using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Application.UploadIntents;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class UpdatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        IStorageService storageService,
        ILogger<UpdatePlayListCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<UpdatePlayListCommand, Result<PlayListApplicationResponse>>
    {
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

            if (request.NewName is not null)
            {
                playListEntity.Name = request.NewName;
                playListEntity.NormalizedName = request.NewName.Trim().ToUpperInvariant();
            }

            if (request.NewDescription is not null)
            {
                playListEntity.Description = request.NewDescription;
            }

            if (request.NewPictureIntentId.HasValue)
            {
                var validation = await UploadIntentHelpers.ValidateAndLoadAsync(
                    database, storageService, uploadIntentConfiguration,
                    request.NewPictureIntentId.Value, request.UserId, cancellationToken);
                if (!validation.IsSuccess)
                    return validation.Error!.Value;

                var intent = validation.Intent!;
                var finalKey = playListConfiguration.Routes.BuildOriginalPicturePath(request.UserId, intent.ObjectName);
                try
                {
                    await storageService.CopyFileAsync(intent.Bucket, intent.Key, intent.Bucket, finalKey, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to copy playlist picture from temp {TempKey} to final {FinalKey}", intent.Key, finalKey);
                    return Result.Error("Failed to move uploaded file to its final location.");
                }

                playListEntity.OriginalPictureName = intent.ObjectName;
                intent.Status = UploadIntentStatus.Consumed;

                var publishResult = await PublishPlayListPictureEvent(playListEntity.UserId, playListEntity.Id, playListEntity.OriginalPictureName, cancellationToken);
                if (!publishResult.IsSuccess)
                    return publishResult;
            }

            try
            {
                database.PlayLists.Update(playListEntity);
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update playlist {PlayListId}", playListEntity.Id);
                return Result.Error($"Failed to update playlist {playListEntity.Id}");
            }

            logger.LogInformation("Updated playlist {PlayListId}", playListEntity.Id);
            return Result.Success(PlayListApplicationResponse.FromEntity(playListEntity));
        }

        async Task<Result> PublishPlayListPictureEvent(Guid userId, Guid playListId, string originalPictureName, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                    playListId,
                    storageConfiguration.Bucket,
                    playListConfiguration.Routes.BuildOriginalPicturePath(userId, originalPictureName),
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
                logger.LogError(exception, "Failed to publish playlist picture update event for playlist {PlayListId}", playListId);
                return Result.Error($"Failed to publish playlist {playListId} picture update event");
            }
        }
    }
}
