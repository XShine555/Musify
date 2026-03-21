using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;

namespace Musify.Application.PlayLists.Commands.DeletePlayList
{
    public class DeletePlayListCommandHandler(IDatabase database, IEventBus eventBus,
        ApplicationStorageConfiguration storageConfiguration, PlayListConfiguration playListConfiguration, ILogger<DeletePlayListCommandHandler> logger)
        : IRequestHandler<DeletePlayListCommand, Task<Result>>
    {
        public async Task<Result> Handle(DeletePlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.FindAsync(request.PlayListId, cancellationToken);

            if (playList is null)
            {
                logger.LogDebug("PlayList with id={PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogInformation("PlayList with id={PlayListId} does not belong to user with id={UserId}", request.PlayListId, request.UserId);
                return Result.Unauthorized();
            }

            var keysToRemove = new List<string>();

            if (playList.SmallPictureKeyName != playListConfiguration.Routes.PresetSmallPicture)
            {
                keysToRemove.Add(playList.SmallPictureKeyName);
            }
            if (playList.MediumPictureKeyName != playListConfiguration.Routes.PresetMediumPicture)
            {
                keysToRemove.Add(playList.MediumPictureKeyName);
            }
            if (playList.LargePictureKeyName != playListConfiguration.Routes.PresetLargePicture)
            {
                keysToRemove.Add(playList.LargePictureKeyName);
            }

            foreach (var key in keysToRemove)
            {
                var publishResult = await TryPublishRemoveFileEventAsync(playList.Id, key, cancellationToken);

                if (!publishResult.IsSuccess)
                {
                    return publishResult;
                }
            }

            database.PlayLists.Remove(playList);
            await database.SaveChangesAsync(cancellationToken);
            return Result.NoContent();
        }

        async Task<Result> TryPublishRemoveFileEventAsync(Guid playListId, string keyName, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new RemoveFileEvent(
                    storageConfiguration.BucketName,
                    keyName), cancellationToken);

                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish remove file event for PlayList id={PlayListId} and key={KeyName}.", playListId, keyName);
                return Result.Error($"Failed to publish remove file event for PlayList with id {playListId}.");
            }
        }
    }
}