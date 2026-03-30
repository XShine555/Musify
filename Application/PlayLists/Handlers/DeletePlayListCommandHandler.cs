using Ardalis.Result;
using Mediator;
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
            var playList = await database.PlayLists.FindAsync(
                [request.PlayListId],
                cancellationToken);
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

            if (playList.SmallPictureName != playListConfiguration.Routes.PresetSmallPicture)
                await PublishRemoveFileEventAsync(playList.Id, Path.Combine(playListConfiguration.Routes.SmallPicturesPath,
                    playList.SmallPictureName), cancellationToken);
            if (playList.MediumPictureName != playListConfiguration.Routes.PresetMediumPicture)
                await PublishRemoveFileEventAsync(playList.Id, Path.Combine(playListConfiguration.Routes.MediumPicturesPath,
                    playList.MediumPictureName), cancellationToken);
            if (playList.LargePictureName != playListConfiguration.Routes.PresetLargePicture)
                await PublishRemoveFileEventAsync(playList.Id, Path.Combine(playListConfiguration.Routes.LargePicturesPath,
                    playList.LargePictureName), cancellationToken);

            database.PlayLists.Remove(playList);
            await database.SaveChangesAsync(cancellationToken);

            return Result.NoContent();
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