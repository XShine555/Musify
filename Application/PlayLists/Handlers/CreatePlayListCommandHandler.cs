using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
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
    public class CreatePlayListCommandHandler(IEventBus eventBus, IDatabase database, IStorageHandler storageHandler,
        ILogger<CreatePlayListCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration, PlayListConfiguration playListConfiguration)
        : IRequestHandler<CreatePlayListCommand, Task<Result<PlayListResponse>> >
    {
        public async Task<Result<PlayListResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var playListEntity = CreatePlayListCommand.ToEntity(request,
                playListConfiguration.Routes.PresetOriginalPicture,
                playListConfiguration.Routes.PresetSmallPicture,
                playListConfiguration.Routes.PresetMediumPicture,
                playListConfiguration.Routes.PresetLargePicture);

            if (request.Picture is not null)
            {
                var uploadResult = await SetPicture(playListEntity, request.Picture, cancellationToken);
                if (!uploadResult.IsSuccess)
                    return uploadResult;

                var publishResult = await PublishUpdateEvent(playListEntity, cancellationToken);
                if (!publishResult.IsSuccess)
                {
                    await RollbackPicture(playListEntity, cancellationToken);
                    return publishResult;
                }
            }

            try
            {
                await database.PlayLists.AddAsync(playListEntity, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                await RollbackPicture(playListEntity, cancellationToken);
                logger.LogError(exception, "Failed to create playlist for user {UserId}", request.UserId);
                return Result.Error($"Failed to create playlist for user {request.UserId}");
            }

            logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playListEntity.Id, request.UserId);
            return Result.Created(PlayListResponse.FromEntity(playListEntity));
        }

        async Task RollbackPicture(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                await storageHandler.RemoveFileAsync(storageConfiguration.BucketName,
                    Path.Combine(playListConfiguration.Routes.OriginalPicturesPath, playList.OriginalPictureName), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to rollback picture upload for playlist {PlayListId}", playList.Id);
            }
        }

        async Task<Result> SetPicture(PlayList playList, IFileData picture, CancellationToken cancellationToken)
        {
            var pictureName = Guid.NewGuid() + picture.FileType;
            var pictureKey = Path.Combine(playListConfiguration.Routes.OriginalPicturesPath, pictureName);

            var uploadResult = await storageHandler.UploadFileAsync(picture.FileStream, picture.ContentType, storageConfiguration.BucketName, pictureKey, cancellationToken);
            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for playlist {PlayListId} to storage: {ErrorMessage}",
                    playList.Id, string.Join(", ", uploadResult.Errors));
                return uploadResult.ToErrorResult();
            }

            playList.OriginalPictureName = pictureName;

            return Result.Success();
        }

        async Task<Result> PublishUpdateEvent(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                    playList.Id,
                    storageConfiguration.BucketName,
                    playList.OriginalPictureName,
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
    }
}