using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
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

            var originalPictureKey = playListConfiguration.Routes.BuildOriginalPicturePath(playList.OriginalPictureName);
            var smallPictureKey = playListConfiguration.Routes.BuildSmallPicturePath(playList.SmallPictureName);
            var mediumPictureKey = playListConfiguration.Routes.BuildMediumPicturePath(playList.MediumPictureName);
            var largePictureKey = playListConfiguration.Routes.BuildLargePicturePath(playList.LargePictureName);

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

            await PublishRemoveFileEventsBestEffortAsync(
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

            return Result.NoContent();
        }

        async Task PublishRemoveFileEventsBestEffortAsync(
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
                await PublishRemoveFileEventIgnoringErrorsAsync(playListId, originalPictureKey, cancellationToken);
            if (smallPictureName != playListRoutes.PresetSmallPicture)
                await PublishRemoveFileEventIgnoringErrorsAsync(playListId, smallPictureKey, cancellationToken);
            if (mediumPictureName != playListRoutes.PresetMediumPicture)
                await PublishRemoveFileEventIgnoringErrorsAsync(playListId, mediumPictureKey, cancellationToken);
            if (largePictureName != playListRoutes.PresetLargePicture)
                await PublishRemoveFileEventIgnoringErrorsAsync(playListId, largePictureKey, cancellationToken);
        }

        async Task PublishRemoveFileEventIgnoringErrorsAsync(Guid playListId, string key, CancellationToken cancellationToken)
        {
            var result = await PublishRemoveFileEventAsync(playListId, key, cancellationToken);
            if (!result.IsSuccess)
                logger.LogWarning("Remove file event publish failed for playlist {PlayListId} with key {Key}", playListId, key);
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