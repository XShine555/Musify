using Ardalis.Result;
using Mediator;
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
    public class CreatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        IStorageService storageService,
        ILogger<CreatePlayListCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
        : ICommandHandler<CreatePlayListCommand, Result<PlayListResponse>>
    {
        public async ValueTask<Result<PlayListResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
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
                    return publishResult;
            }

            logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playList.Id, request.UserId);
            return Result.Created(PlayListResponse.FromEntity(playList));
        }

        async Task<Result> UploadPictureAsync(PlayList playList, IFileData picture, CancellationToken cancellationToken)
        {
            var pictureName = Guid.NewGuid() + picture.FileType;
            var pictureKey = Path.Combine(playListConfiguration.Routes.OriginalPicturesPath, pictureName);

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

            var originalPicturePath = Path.Combine(playListConfiguration.Routes.OriginalPicturesPath, playList.OriginalPictureName);

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