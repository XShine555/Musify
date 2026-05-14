using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;

namespace Musify.Application.PlayLists.Handlers
{
    public class DeletePlayListCommandHandler(IDatabase database, IEventBus eventBus,
        ApplicationStorageConfiguration storageConfiguration, PlayListConfiguration playListConfiguration, ILogger<DeletePlayListCommandHandler> logger)
        : ICommandHandler<DeletePlayListCommand, Result>
    {
        public async ValueTask<Result> Handle(DeletePlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.SingleOrDefaultAsync(p => p.Id == request.PlayListId, cancellationToken);
            if (playList is null)
            {
                logger.LogDebug("Playlist {PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("Playlist {PlayListId} does not belong to user {UserId}", request.PlayListId, request.UserId);
                return Result.Unauthorized();
            }

            var originalPictureName = playList.OriginalPictureName;
            var smallPictureName = playList.SmallPictureName;
            var mediumPictureName = playList.MediumPictureName;
            var largePictureName = playList.LargePictureName;

            var originalPictureKey = playListConfiguration.Routes.BuildOriginalPicturePath(playList.UserId, playList.OriginalPictureName);
            var smallPictureKey = playListConfiguration.Routes.BuildSmallPicturePath(playList.SmallPictureName);
            var mediumPictureKey = playListConfiguration.Routes.BuildMediumPicturePath(playList.MediumPictureName);
            var largePictureKey = playListConfiguration.Routes.BuildLargePicturePath(playList.LargePictureName);

            var publishResult = await PublishRemoveFileEventsAsync(
                playList.Id,
                originalPictureName,
                smallPictureName,
                mediumPictureName,
                largePictureName,
                originalPictureKey,
                smallPictureKey,
                mediumPictureKey,
                largePictureKey,
                playListConfiguration.Routes,
                cancellationToken);

            if (!publishResult.IsSuccess)
                return publishResult;

            try
            {
                database.PlayLists.Remove(playList);
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to delete playlist {PlayListId}", request.PlayListId);
                return Result.Error($"Failed to delete playlist {request.PlayListId}");
            }

            return Result.NoContent();
        }

        async Task<Result> PublishRemoveFileEventsAsync(
            Guid playListId,
            string originalPictureName,
            string smallPictureName,
            string mediumPictureName,
            string largePictureName,
            string originalPictureKey,
            string smallPictureKey,
            string mediumPictureKey,
            string largePictureKey,
            PlayListRoutes playListRoutes,
            CancellationToken cancellationToken)
        {
            if (originalPictureName != playListRoutes.PresetOriginalPicture)
            {
                var result = await PublishRemoveFileEventAsync(playListId, originalPictureKey, cancellationToken);
                if (!result.IsSuccess)
                    return result;
            }
            if (smallPictureName != playListRoutes.PresetSmallPicture)
            {
                var result = await PublishRemoveFileEventAsync(playListId, smallPictureKey, cancellationToken);
                if (!result.IsSuccess)
                    return result;
            }
            if (mediumPictureName != playListRoutes.PresetMediumPicture)
            {
                var result = await PublishRemoveFileEventAsync(playListId, mediumPictureKey, cancellationToken);
                if (!result.IsSuccess)
                    return result;
            }
            if (largePictureName != playListRoutes.PresetLargePicture)
            {
                var result = await PublishRemoveFileEventAsync(playListId, largePictureKey, cancellationToken);
                if (!result.IsSuccess)
                    return result;
            }

            return Result.Success();
        }

        async Task<Result> PublishRemoveFileEventAsync(Guid playListId, string key, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new RemoveFileEvent(
                    storageConfiguration.Bucket,
                    key), cancellationToken);
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish remove file event for playlist {PlayListId} with key {Key}", playListId, key);
                return Result.Error($"Failed to publish remove file event for playlist {playListId}");
            }
        }
    }
}